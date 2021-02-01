using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LevelEditor : MonoBehaviour
{
    public Text scrollTimeReadout;
    public Text activeSelectionName;

    class ScrollSpeedChange
    {
        public float Time;
        public float NewSpeed;
        public float LerpTime;
        public ScrollSpeedChange(float time, float speed, float lerpTime)
        {
            Time = time;
            NewSpeed = speed;
            LerpTime = lerpTime;
        }
    }

    public InputActionAsset actionMapUI;
    InputAction mousePos;
    InputAction leftClick;
    static readonly List<ScrollSpeedChange> ScrollSpeedCache = new List<ScrollSpeedChange>();

    enum States {NoLevel, Loading, Editing, Testing}
    public static float EditorTime = 0;

    public RectTransform UILinesLayer;
    public TextAsset Level;
    public Scrollbar levelScroll;
    public RectTransform commandsTrack;
    public static float LevelLength;
    public static float LevelDurationEditor = 0;
    public static float LevelDuration = 0;

    public static LevelEditorSpawnedCommand ActiveCommand = null;
    public static readonly List<LevelEditorSpawnedCommand> SelectedCommands = new List<LevelEditorSpawnedCommand>();

    public static readonly float ScreenHeight = 48;
    readonly float LevelWidth = 96;
    public float ScrollPosition;
    public List<LevelParser.LevelLine> LevelParsed = new List<LevelParser.LevelLine>();
    public Toggle UIShowSpawnLines;
    public RectTransform ShowLevelFrame;

    public Color CommandLineColor = Color.white;
    public Color SpawnerChildLineColor = Color.green/2;
    public Color MultipleChildLineColor = Color.red / 2;
    public Color CommandLineSelectedColor = Color.white;
    public float TailLength = 40;

    int WindowResizeStage = 0;
    Vector2 OldWindowSize = Vector2.zero;
    public static LevelEditor current;

    Camera cam;

    public float UILineWidth = 2;
    public float UILineHoveredWidth = 3;

    readonly Dictionary<string, ResourceRequest> LoadAssets = new Dictionary<string, ResourceRequest>();

    States state = States.NoLevel;

    public static readonly float EditorHoldDelay = 5;

    public static readonly List<LevelEditorSpawnedCommand.SpawnedObjectContainer> SpawnedObjects = new List<LevelEditorSpawnedCommand.SpawnedObjectContainer>();
    public static readonly List<LevelEditorSpawnedCommand.LevelHoldContainer> LevelHolds = new List<LevelEditorSpawnedCommand.LevelHoldContainer>();
    public static readonly List<LevelEditorSpawnedCommand.ScrollSpeedContainer> ScrollSpeeds = new List<LevelEditorSpawnedCommand.ScrollSpeedContainer>();
    void Awake()
    {
        current = this;
        if (Level == null)
        {
            SelectLevel();
        }
        cam = GetComponent<Camera>();
        mousePos = actionMapUI.FindAction("Point");
        leftClick = actionMapUI.FindAction("Click");
    }

    private void OnEnable()
    {
        GlobalTools.Mode = GlobalTools.GameModes.Editor;
        levelScroll.onValueChanged.AddListener(UpdateLevelScroll);
        UIShowSpawnLines.onValueChanged.AddListener(UpdateUILines);
    }
    private void OnDisable()
    {
        levelScroll.onValueChanged.RemoveListener(UpdateLevelScroll);
        UIShowSpawnLines.onValueChanged.RemoveListener(UpdateUILines);
    }

    private void Update()
    {
        switch (state)
        {
            case States.NoLevel:
                if (Level != null)
                {
                    LevelParsed = LevelParser.ParseFile(Level, out List<string> PathsToLoad);
                    foreach (string assetPath in PathsToLoad)
                    {
                        LoadAssets.Add(assetPath, Resources.LoadAsync(assetPath));
                    }
                    state = States.Loading;
                    goto StartLoading; //no need to wait a frame for the switch to come back around
                }
                break;
            case States.Loading:
                StartLoading:
                bool DoneLoading = true;
                foreach (ResourceRequest loadAsset in LoadAssets.Values)
                {
                    if (!loadAsset.isDone)
                    {
                        DoneLoading = false;
                    }
                }
                if (DoneLoading)
                {
                    PopulateLevel();
                    state = States.Editing;
                    actionMapUI.Enable();
                }

                break;
            case States.Editing:
                if (Screen.width != OldWindowSize.x || Screen.height != OldWindowSize.y)
                {
                    WindowResizeStage = 0;
                    OldWindowSize = new Vector2(Screen.width, Screen.height);
                }
                UpdateWindowShape();
                break;
            case States.Testing:
                break;
        }

    }

    void PopulateLevel()
    {
        //TODO: use CurrentPosition with spawn commands to populate a level preview strip (just spawn the objects along the track, render into a very super tall texture?)
        float TempPosition=0;
        float TempTime = 0;
        float TempHoldTime = 0;
        float deltaTimestamp;

        BuildScrollSpeedCacheFirstPass();

        for (int CurrentCommandIndex = 0; CurrentCommandIndex < LevelParsed.Count; CurrentCommandIndex++)
        {
            LevelParser.LevelLine line = LevelParsed[CurrentCommandIndex];

            deltaTimestamp = line.Time;

            if (!line.RelativePosition)
            {
                //convert absolute to delta
                deltaTimestamp -= TempTime-TempHoldTime;
            }
            if (deltaTimestamp != 0) //no need to update position if we haven't changed time since the last command
            {
                TempPosition = GetDistanceTraveledAtTime(TempTime+deltaTimestamp);
            }
            TempTime += deltaTimestamp;

            if (TempPosition > LevelLength)
            {
                LevelLength = TempPosition;
            }

            string arg = "";
            string val = "";
            int CurrentArg = 0;

            switch (line.Command)
            {
                case LevelParser.AvailableCommands.HoldForDeath:
                    bool add = true;
                    while (GetNextArgument())
                    {
                        if (arg == "Count" && int.Parse(val) == 0)
                        {
                            add = false;
                            break;
                        }
                    }
                    if (add)
                    {
                        GameObject newHoldOb = new GameObject();
                        newHoldOb.transform.SetParent(commandsTrack, false);
                        RectTransform t = newHoldOb.AddComponent<RectTransform>();
                        newHoldOb.AddComponent<RawImage>();

                        t.offsetMin = t.offsetMax = Vector2.zero;
                        t.anchorMin = new Vector2(0, TempTime / LevelDurationEditor);
                        t.anchorMax = new Vector2(1, (TempTime+EditorHoldDelay)/LevelDurationEditor);

                        LevelEditorSpawnedCommand.LevelHoldContainer newCommand = new LevelEditorSpawnedCommand.LevelHoldContainer(newHoldOb, CurrentCommandIndex, TempTime-TempHoldTime, TempTime);
                        LevelHolds.Add(newCommand);
                        newCommand.AddTimeCollider();
                        TempHoldTime += EditorHoldDelay;
                        TempTime += EditorHoldDelay;
                    }
                    break;
                case LevelParser.AvailableCommands.Spawn:
                    GetNextArgument();
                    ResourceRequest prefabResource = LoadAssets[val];
                    GetNextArgument();

                    Vector3 SpawnPosition = LevelParser.ParseVector3(val);
                    GameObject newSpawnedOb = Instantiate(prefabResource.asset as GameObject, SpawnPosition, Quaternion.identity);


                    LevelEditorSpawnedCommand.SpawnedObjectContainer newContainer = new LevelEditorSpawnedCommand.SpawnedObjectContainer(newSpawnedOb, CurrentCommandIndex, SpawnPosition, TempTime - TempHoldTime, TempTime, TempPosition);
                    SpawnedObjects.Add(newContainer);
                    newSpawnedOb.gameObject.AddComponent<LevelEditorSpawnedCommand>().command = newContainer;
                    newContainer.line = UILine.NewLine(UILineWidth, UILinesLayer);
                    newContainer.line.SetTail(TailLength);
                    newContainer.AddTimeCollider(newContainer.line.tail.gameObject);
                    newContainer.SliderSpawnTime.SetActive(false);

                    newContainer.LineBaseColor = CommandLineColor;

                    int multiples = 0;
                    float multipleDelay = 0;
                    float multipleOffset = 0;

                    while (GetNextArgument())
                    {
                        switch (arg)
                        {
                            case "Animation":
                                GameObject spawnedObParent = new GameObject(newSpawnedOb.name);
                                spawnedObParent.transform.position = newSpawnedOb.transform.position;
                                newSpawnedOb.transform.SetParent(spawnedObParent.transform, true);
                                newContainer.anim = newSpawnedOb.GetComponent<Animation>();
                                AnimationClip animClip = LoadAssets[val].asset as AnimationClip;
                                newContainer.anim.clip = animClip;
                                newContainer.anim.AddClip(animClip, animClip.name);
                                newContainer.anim.Stop();

                                break;
                            case "Path":
                                //TODO: implement
                                break;
                            case "Repeat":
                            case "Several":
                            case "Multiple":
                                Vector3 vec = LevelParser.ParseVector3(val);
                                multiples = (int)vec.x;
                                multipleDelay = vec.y;
                                multipleOffset = vec.z;
                                break;
                        }
                    }

                    DetermineCommandLifespan(newContainer);

                    Spawner spawner = newSpawnedOb.GetComponent<Spawner>();
                    if (spawner)
                    {
                        List<LevelEditorSpawnedCommand.SpawnedObjectContainer> tempSpawnerChildren = new List<LevelEditorSpawnedCommand.SpawnedObjectContainer>();
                        float SpawnerTimer;
                        float SpawnerDistance;
                        int SpawnerCount = spawner.spawnCount;
                        if (spawner.IntervalIsDistance)
                        {
                            SpawnerDistance = spawner.spawnDelay;
                            //TODO: need to fix GetTimeFromDistanceTraveled mid-lerp return
                            SpawnerTimer = GetTimeFromDistanceTraveled(SpawnerDistance, TempTime);
                        }
                        else
                        {
                            SpawnerTimer = spawner.spawnDelay;
                            SpawnerDistance = GetDistanceTraveledAtTime(SpawnerTimer);
                        }
                        //Debug.Log($"{newSpawn.Life}");
                        while(SpawnerTimer < newContainer.Life)
                        {
                            //spawn object
                            GameObject spawnedOb;
                            Vector3 spawnPos = spawner.transform.position + new Vector3(
                                UnityEngine.Random.Range(-spawner.randomX, spawner.randomX),
                                UnityEngine.Random.Range(-spawner.randomY, spawner.randomY));
                            if (spawner.LocalCoordinates)
                                spawnedOb = Instantiate(spawner.spawn, spawnPos, Quaternion.identity, spawner.transform);
                            else
                                spawnedOb = Instantiate(spawner.spawn, spawnPos, Quaternion.identity);
                            spawnedOb.gameObject.AddComponent<LevelEditorSpawnedCommand>().command = newContainer;
                            //TODO: handle animation (or just strip that option from the spawner class, that might be better)

                            LevelEditorSpawnedCommand.SpawnedObjectContainer newChildContainer = new LevelEditorSpawnedCommand.SpawnedObjectContainer(spawnedOb, spawnedOb.transform.position, -1, SpawnerTimer + TempTime, SpawnerDistance + TempPosition);

                            tempSpawnerChildren.Add(newChildContainer);

                            newChildContainer.TriggerOffsetTime = SpawnerTimer;
                            DetermineCommandLifespan(newChildContainer);
                            newChildContainer.line = UILine.NewLine(UILineWidth, UILinesLayer);
                            newChildContainer.LineBaseColor =SpawnerChildLineColor;
                            newChildContainer.SetVisualsIdle();


                            SpawnerCount--;
                            if (SpawnerCount == 0)
                            {
                                break;
                            }
                            if (spawner.IntervalIsDistance)
                            {
                                SpawnerDistance += spawner.spawnInterval + UnityEngine.Random.Range(0, spawner.randomInterval);
                                //TODO: need to fix GetTimeFromDistanceTraveled mid-lerp return
                                SpawnerTimer = GetTimeFromDistanceTraveled(SpawnerDistance, TempTime);
                            }
                            else
                            {
                                SpawnerTimer += spawner.spawnInterval
                                    + UnityEngine.Random.Range(0, spawner.randomInterval);
                                SpawnerDistance = GetDistanceTraveledAtTime(SpawnerTimer);
                            }
                        }

                        newContainer.SpawnerChildren = tempSpawnerChildren.ToArray();
                        //spawners may not use the multiples command
                    }
                    else if (multiples > 0)
                    {
                        newContainer.SpawnerChildren = new LevelEditorSpawnedCommand.SpawnedObjectContainer[multiples-1];
                        for(int i = 1; i < multiples; i++)
                        {
                            GameObject child;
                            if (newContainer.anim)
                            {
                                child = Instantiate(newSpawnedOb.transform.parent.gameObject).transform.GetChild(0).gameObject;
                                child.transform.parent.position += new Vector3(0, multipleOffset * i, 0);
                            }
                            else
                            {
                                child = Instantiate(newSpawnedOb);
                                child.transform.position += new Vector3(0, multipleOffset * i, 0);
                            }

                            child.GetComponent<LevelEditorSpawnedCommand>().command = newContainer; //the object we cloned already has this component, but in the cloning it seems like the component loses its pointer so point it back at the same thing again

                            float childTime = (multipleDelay * i);
                            LevelEditorSpawnedCommand.SpawnedObjectContainer newChildContainer = new LevelEditorSpawnedCommand.SpawnedObjectContainer(child, child.transform.position, -1, TempTime + childTime, GetDistanceTraveledAtTime(childTime));
                            
                            newContainer.SpawnerChildren[i - 1] = newChildContainer;

                            newChildContainer.TriggerOffsetTime = childTime;
                            newChildContainer.line = UILine.NewLine(UILineWidth, UILinesLayer);
                            newChildContainer.LineBaseColor = MultipleChildLineColor;
                            newChildContainer.SetVisualsIdle();
                            DetermineCommandLifespan(newChildContainer);
                        }
                    }

                    newContainer.Deselect();
                    newContainer.SetVisualsIdle();

                    break;



                    bool GetNextArgument()
                    {
                        if (CurrentArg < line.Arguments.Length)
                        {
                            arg = line.Arguments[CurrentArg].Argument;
                            val = line.Arguments[CurrentArg].Value;
                            CurrentArg++;
                            return true;
                        }
                        return false;
                    }

            }
        }
        UpdateLevelLength();

        UpdateLevelScroll(0);
    }

    public static void SpawnLevelBreakCommand()
    {
        //SpawnedObjects.Add()
    }

    public static void DetermineCommandLifespan(LevelEditorSpawnedCommand.LevelPositionalContainer container)
    {
        ParallaxScroll parallax = container.obj.GetComponent<ParallaxScroll>();
        Animation anim = container.obj.GetComponent<Animation>();
        if (parallax)
        {
            container.parallaxScroll = true;

            //TODO: there's gotta be a more elegant way to do this than to reset the position, calculate the bounds, then re-un-de-reset the position
            Vector3 TempPosition = container.obj.transform.position;
            container.obj.transform.position = container.StartPosition;
            float TopEdge = 0;
            foreach (Renderer renderer in container.obj.GetComponentsInChildren<Renderer>())
            {
                if (renderer.bounds.max.y > TopEdge) TopEdge = renderer.bounds.max.y;
            }
            container.obj.transform.position = TempPosition;
            float TravelDistance = ParallaxScroll.DetermineLife(TopEdge, container.StartPosition.z);
            container.Life = GetTimeFromDistanceTraveled(TravelDistance, container.EditorTriggerTime);
        }
        else if (anim)
        {
            container.Life = anim.clip.length;
            container.anim = anim;
        }
        //TODO: calculate lifespan for path

        else
        {
            container.Life = LevelDurationEditor - container.EditorTriggerTime;
        }
    }

    void SelectLevel()
    {
        //TODO: popup to select existing file or create new
    }

    public static void SelectCommand(LevelEditorSpawnedCommand commandOb)
    {
        if (commandOb == null)
        {
            ClearCommandSelection();
            return;
        }
        if (ActiveCommand && ActiveCommand.command != commandOb.command)
        {
            ActiveCommand.Deselect();
        }
        ActiveCommand = commandOb;
        ActiveCommand.Select();
        //current.activeSelectionName.text = $"Ob: {commandOb.command.obj.name} \n Line {commandOb.command.CommandIndex}";
        //TODO: f we're holding shift or ctrl and there's currently an active selected command, add commandOb to selectedCommands
    }
    public static void ClearCommandSelection()
    {
        if (ActiveCommand != null)
        {
            ActiveCommand.Deselect();
            ActiveCommand = null;
        }
        foreach (LevelEditorSpawnedCommand command in SelectedCommands)
        {
            command.Deselect();
        }
        SelectedCommands.Clear();
        current.activeSelectionName.text = "";
    }

    public static void UpdateCommand(LevelEditorSpawnedCommand.LevelPositionalContainer container)
    {
        current.UpdateLevelScroll(EditorTime / LevelDurationEditor, container);
    }

    void UpdateLevelScroll(float f)
    {
        UpdateLevelScroll(f, null);
    }
    void UpdateLevelScroll(float f, LevelEditorSpawnedCommand.LevelPositionalContainer individualContainer)
    {
        EditorTime = Mathf.Clamp(f, .001f, .999f) * LevelDurationEditor; //tiny offset to make sure commands at time 0 are present

        //when scrolling forward, we can just simulate the camera position from the current time to the new time
        //when scrolling backwards, we have to simulate the camera position from the start of the level to the new position
        //maybe we can cache this somehow and just rebuild the cache when scroll speed commands are entered/edited?
        ScrollPosition = GetDistanceTraveledAtTime(EditorTime);
        if (scrollTimeReadout) scrollTimeReadout.text = $"{EditorTime} \n {ScrollPosition}";
        float DistanceSinceTrigger;
        float TimeSinceTrigger;

        if (individualContainer != null)
        {
            UpdateContainer(individualContainer);
            UpdateUILine(individualContainer);
        }
        else
        {
            foreach (LevelEditorSpawnedCommand.CommandContainer spawnedContainer in SpawnedObjects)
            {
                if (spawnedContainer.CommandType == LevelParser.AvailableCommands.Spawn)
                {
                    UpdateContainer((LevelEditorSpawnedCommand.SpawnedObjectContainer)spawnedContainer);
                }
            }
            UpdateUILines();
        }


        void UpdateContainer(LevelEditorSpawnedCommand.LevelPositionalContainer container)
        {

            UpdateSubcontainer(container);
            if (container.CommandType == LevelParser.AvailableCommands.Spawn)
            {
                LevelEditorSpawnedCommand.SpawnedObjectContainer spawnContainer = (LevelEditorSpawnedCommand.SpawnedObjectContainer)container;
                if (spawnContainer.SpawnerChildren != null)
                {
                    foreach (LevelEditorSpawnedCommand.SpawnedObjectContainer spawnedObChild in spawnContainer.SpawnerChildren)
                    {
                        UpdateSubcontainer(spawnedObChild);
                    }
                }
            }
        }

        void UpdateSubcontainer(LevelEditorSpawnedCommand.LevelPositionalContainer container)
        {

            DistanceSinceTrigger = ScrollPosition - container.TriggerPosition;
            TimeSinceTrigger = EditorTime - container.EditorTriggerTime;
            float LifeEnd = LevelLength;
            if (container.Life > 0)
            {
                LifeEnd = container.EditorTriggerTime + container.Life;
            }
            if (container.EditorTriggerTime < EditorTime && LifeEnd > EditorTime)
            {
                container.obj.SetActive(true);
                if (container.parallaxScroll)
                {
                    container.obj.transform.position = ParallaxScroll.ScrollAbsolute(container.StartPosition, DistanceSinceTrigger);
                }
                else if (container.anim)
                {
                    container.anim.Play();
                    container.anim[container.anim.clip.name].time = TimeSinceTrigger;
                    container.anim.Sample();
                    container.anim.Stop();
                }
                else if (container.path)
                {
                    //TODO: implement
                }
                container.obj.transform.position = GlobalTools.PixelSnap(container.obj.transform.position);
            }
            else
            {
                container.obj.SetActive(false);
            }
        }
    }

    public static float GetDistanceTraveledAtTime(float TargetTime)
    {
        float currentTime = 0;
        float distance = 0;
        float speed = 0;

        for (int i = 0; i < ScrollSpeedCache.Count; i++)
        {
            ScrollSpeedChange currentStep = ScrollSpeedCache[i];

            if (currentStep.Time >= TargetTime)
            {
                return distance + (TargetTime - currentTime) * speed;
            }
            distance += (currentStep.Time - currentTime) * speed;

            currentTime = currentStep.Time;
            if (TargetTime < currentTime + currentStep.LerpTime)
            {
                float NewLerpTime = TargetTime - currentTime;
                distance += AreaUnderLerp(speed, Mathf.Lerp(speed, currentStep.NewSpeed, NewLerpTime/currentStep.LerpTime), NewLerpTime);
                return distance;
            }
            distance += AreaUnderLerp(speed, currentStep.NewSpeed, currentStep.LerpTime);
            speed = currentStep.NewSpeed;

            currentTime += currentStep.LerpTime;
        }

        return distance + (TargetTime - currentTime) * speed;
    }

    static float GetTimeFromDistanceTraveled(float TargetDistance, float StartTime)
    {
        float CurrentTime = StartTime;
        float CurrentDistance = 0;
        float DistanceOffset;
        float CurrentSpeed = 0;
        bool NotStartedYet = true;
        for (int i = 0; i < ScrollSpeedCache.Count; i++)
        {
            ScrollSpeedChange current = ScrollSpeedCache[i];
            if (current.Time < StartTime)
            {
                CurrentTime = current.Time;
                if (current.Time+current.LerpTime > StartTime)
                {
                    CurrentSpeed = Mathf.Lerp(CurrentSpeed, current.NewSpeed, (StartTime-CurrentTime)/current.LerpTime);
                    float NewLerp = current.LerpTime + CurrentTime - StartTime;
                    CurrentDistance = NewLerp * Mathf.Min(CurrentSpeed, current.NewSpeed); //area of the square under the line segment
                    CurrentDistance += NewLerp * Mathf.Abs(CurrentSpeed - current.NewSpeed) / 2; //area of the triangle described by the line segment
                }
                CurrentSpeed = current.NewSpeed;
                CurrentTime += current.LerpTime;
            }
            else
            {
                if (NotStartedYet)
                {
                    DistanceOffset = (current.Time - StartTime) * CurrentSpeed;
                    NotStartedYet = false;
                    //TODO: a return case for if the item despawns before we even reach the next lerp
                    if (DistanceOffset > TargetDistance)
                    {
                        return TargetDistance / CurrentSpeed;
                    }
                }
                else
                {
                    DistanceOffset = (current.Time - CurrentTime) * CurrentSpeed;
                }
                if (CurrentDistance + DistanceOffset > TargetDistance) //the target distance falls between two lerps, simple
                {
                    return CurrentTime + (TargetDistance - CurrentDistance) / CurrentSpeed - StartTime;
                }
                CurrentDistance += DistanceOffset;
                DistanceOffset = current.LerpTime * Mathf.Min(CurrentSpeed, current.NewSpeed); //area of the square under the line segment
                DistanceOffset += current.LerpTime * Mathf.Abs(CurrentSpeed - current.NewSpeed) / 2; //area of the triangle described by the line segment
                if (CurrentDistance + DistanceOffset > TargetDistance) //target distance falls within the lerp
                {
                    //I'm dumb, so I'll just return the end of the lerp instead of trying to figure out where within the lerp it lands
                    //some object will be enabled for a bit longer than they should, but eh I'm over it
                    return current.Time + current.LerpTime - StartTime;
                }
                CurrentDistance += DistanceOffset;
                CurrentSpeed = current.NewSpeed;
                CurrentTime = current.Time + current.LerpTime;
            }
        }
        //if we got here, we're past the last lerp, it's just steady smooth sailing from here on out
        return CurrentTime + (TargetDistance - CurrentDistance) / CurrentSpeed  - StartTime;
    }

    public static void RebuildScrollSpeedCache()
    {
        //TODO: BuildScrollSpeedCache, but using ScrollSpeeds and Levelholds rather than the parsed level
        ScrollSpeedCache.Clear();
        float HoldTime = 0;
        int HoldIndex = 0;
        int ScrollSpeedIndex = 0;

        int SortByTriggerTime(LevelEditorSpawnedCommand.CommandContainer x, LevelEditorSpawnedCommand.CommandContainer y)
        {
            if (x.TriggerTime > y.TriggerTime) return 1;
            if (x.TriggerTime < y.TriggerTime) return -1;
            return 0;
        };

        LevelHolds.Sort(delegate (LevelEditorSpawnedCommand.LevelHoldContainer x, LevelEditorSpawnedCommand.LevelHoldContainer y)
        {
            return SortByTriggerTime(x, y);
        });


        ScrollSpeeds.Sort(delegate (LevelEditorSpawnedCommand.ScrollSpeedContainer x, LevelEditorSpawnedCommand.ScrollSpeedContainer y)
        {
            return SortByTriggerTime(x, y);
        });

        for (float TempTime = 0; TempTime < LevelDuration;)
        {

            if ((HoldIndex < LevelHolds.Count && ScrollSpeedIndex >= ScrollSpeeds.Count)||
                (HoldIndex < LevelHolds.Count  && ScrollSpeedIndex < ScrollSpeeds.Count && LevelHolds[HoldIndex].TriggerTime <= ScrollSpeeds[ScrollSpeedIndex].TriggerTime))
            {
                //TODO: determine if I should do scrollspeed first instead (does it matter?)
                //TODO: add the level hold
                HoldTime += EditorHoldDelay;
                TempTime = LevelHolds[HoldIndex].TriggerTime;
                HoldIndex++;
            }
            else if (ScrollSpeedIndex < ScrollSpeeds.Count)
            {
                //TODO: add the scroll speed
                //don't forget to add current HoldTime to TempTime in the cache
                TempTime = ScrollSpeeds[ScrollSpeedIndex].TriggerTime;
                ScrollSpeedIndex++;
            }
            else
            {
                break;
            }
        }


    }

    void BuildScrollSpeedCacheFirstPass()
    {
        //TODO: in the editor, ensure no scroll speed changes overlap one another
        float TempTime = 0;
        float HoldTime = 0;
        ScrollSpeedCache.Clear();
        for (int CurrentCommandIndex = 0; CurrentCommandIndex < LevelParsed.Count; CurrentCommandIndex++)
        {
            LevelParser.LevelLine line = LevelParsed[CurrentCommandIndex];
            if (line.RelativePosition)
            {
                TempTime += line.Time;
            }
            else
            {
                TempTime = line.Time+HoldTime;
            }
            if (line.Command == LevelParser.AvailableCommands.HoldForDeath)
            {
                bool add = true;
                for(int i = 0; i < line.Arguments.Length; i++)
                {
                    if (line.Arguments[i].Argument == "Count" && int.Parse(line.Arguments[i].Value) == 0)
                    {
                        add = false;
                        break;
                    }
                }
                if (add)
                {
                    HoldTime += EditorHoldDelay;
                    TempTime += EditorHoldDelay;
                }
            }
            else if (line.Command == LevelParser.AvailableCommands.ScrollSpeed)
            {
                float LerpTime = 0;
                for (int i = 1; i < line.Arguments.Length; i++)
                {
                    if (line.Arguments[i].Argument == "Lerp")
                    {
                        LerpTime = float.Parse(line.Arguments[i].Value);
                        break;
                    }
                }
                ScrollSpeedCache.Add(new ScrollSpeedChange(TempTime, float.Parse(line.Arguments[0].Value), LerpTime));
            }
            else if (line.Command == LevelParser.AvailableCommands.LevelEnd)
            {
                LevelDurationEditor = TempTime;
                LevelDuration = TempTime - HoldTime;
                Debug.Log($"Level duration: {LevelDurationEditor}");
            }
        }
    }
    bool UpdateWindowShape()
    {
        if (WindowResizeStage == 0)
        {
            //bottom left, top left, top right, bottom right
            Vector3[] CurrentCorners = new Vector3[4];
            ShowLevelFrame.GetWorldCorners(CurrentCorners);
            float ratio = LevelWidth / ScreenHeight;
            Vector2 FrameOffsets = new Vector2((CurrentCorners[2].x - CurrentCorners[0].x), (CurrentCorners[2].y - CurrentCorners[0].y));

            if (FrameOffsets.x / FrameOffsets.y > ratio)
            {
                cam.orthographicSize *= ScreenHeight / FrameOffsets.y;
            }
            else
            {
                cam.orthographicSize *= LevelWidth / FrameOffsets.x;
            }
            WindowResizeStage++;
            UpdateUILines();
            return true;
        }

        if (WindowResizeStage == 1)
        {
            Vector3[] CurrentCorners = new Vector3[4];
            ShowLevelFrame.GetWorldCorners(CurrentCorners);
            transform.position -= new Vector3(
                (CurrentCorners[2].x + CurrentCorners[0].x) / 2,
                (CurrentCorners[2].y + CurrentCorners[0].y) / 2);
            WindowResizeStage++;
            UpdateUILines();
            return true;
        }
        if (WindowResizeStage == 2)
        {
            UpdateUILines();
            WindowResizeStage++;
        }
        return false;
    }
    void UpdateLevelLength()
    {
        LevelLength = GetDistanceTraveledAtTime(LevelDurationEditor);

        //TODO: should I just make the scroll bar a constant size?
        levelScroll.size = Mathf.Max(ScreenHeight / LevelLength, .05f);
        commandsTrack.offsetMax = new Vector2(commandsTrack.offsetMax.x, -levelScroll.handleRect.rect.height/2);
        commandsTrack.offsetMin = new Vector2(commandsTrack.offsetMin.x, levelScroll.handleRect.rect.height / 2);
    }

    static float AreaUnderLerp(float StartSpeed, float EndSpeed, float LerpTime)
    {
        float output = LerpTime * Mathf.Min(StartSpeed, EndSpeed); //area of the square up to the lowest part of the current chunk of the line
        output += LerpTime * Mathf.Abs(StartSpeed - EndSpeed) / 2; //area of the triangle of the change in speed
        return output;
    }

    void UpdateUILines()
    {
        foreach (LevelEditorSpawnedCommand.SpawnedObjectContainer container in SpawnedObjects)
        {
            UpdateUILine(container);
        }
        
    }

    void UpdateUILine(LevelEditorSpawnedCommand.LevelPositionalContainer container)
    {
        float VertPos = (container.EditorTriggerTime / LevelDurationEditor * (1 - levelScroll.size) + levelScroll.size / 2) * ScreenHeight - ScreenHeight / 2;
        UpdateUISubLine(container, new Vector3(LevelWidth / 2, VertPos));

        if (container.CommandType == LevelParser.AvailableCommands.Spawn)
        {
            LevelEditorSpawnedCommand.SpawnedObjectContainer spawnContainer = (LevelEditorSpawnedCommand.SpawnedObjectContainer)container;
            if (spawnContainer.SpawnerChildren != null)
            {
                foreach (LevelEditorSpawnedCommand.SpawnedObjectContainer childContainer in spawnContainer.SpawnerChildren)
                {
                    UpdateUISubLine(childContainer, container.obj.transform.position);
                }
            }
        }

        void UpdateUISubLine(LevelEditorSpawnedCommand.LevelPositionalContainer container, Vector3 target)
        {
            if (container.line)
            {
                if (current.UIShowSpawnLines.isOn)
                {
                    if (container.obj.activeInHierarchy)
                    {
                        container.line.SetActive(true);
                        container.line.UpdateLine(container.obj.transform.position, target);
                    }
                    else
                    {
                        if (container.Selected)
                        {
                            container.line.UpdateLine(container.obj.transform.position, target);
                            container.line.SetActive(false, true);
                        }
                        else
                        {
                            container.line.SetActive(false);
                        }
                    }
                }
                else
                {
                    container.line.SetActive(false);
                }
            }
        }
    }

    void UpdateUILines(bool b)
    {
        UpdateUILines();
    }
}
