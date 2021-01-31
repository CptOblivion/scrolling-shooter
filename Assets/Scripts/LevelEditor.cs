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
    public static float LevelLength;
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

    static readonly float LevelHoldDelay = 5;

    readonly List<LevelEditorSpawnedCommand.SpawnedObjectContainer> SpawnedObjects = new List<LevelEditorSpawnedCommand.SpawnedObjectContainer>();
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

        BuildScrollSpeedCache();

        for (int CurrentCommandIndex = 0; CurrentCommandIndex < LevelParsed.Count; CurrentCommandIndex++)
        {
            LevelParser.LevelLine line = LevelParsed[CurrentCommandIndex];

            float newTime = line.Time;
            if (!line.RelativePosition)
            {
                newTime -= TempTime-TempHoldTime;
            }
            if (newTime != 0) //no need to update position if we haven't changed time since the last command
            {
                TempPosition = GetDistanceTraveledAtTime(TempTime+newTime);
            }
            TempTime += newTime;

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
                        TempHoldTime += LevelHoldDelay;
                        TempTime += LevelHoldDelay;
                    }
                    break;
                case LevelParser.AvailableCommands.Spawn:
                    GetNextArgument();
                    ResourceRequest prefabResource = LoadAssets[val];
                    GetNextArgument();

                    Vector3 SpawnPosition = LevelParser.ParseVector3(val);
                    GameObject newOb = Instantiate(prefabResource.asset as GameObject, SpawnPosition, Quaternion.identity);


                    LevelEditorSpawnedCommand.SpawnedObjectContainer newSpawn = new LevelEditorSpawnedCommand.SpawnedObjectContainer(newOb, CurrentCommandIndex, SpawnPosition, TempTime, TempPosition);
                    SpawnedObjects.Add(newSpawn);
                    newOb.gameObject.AddComponent<LevelEditorSpawnedCommand>().command = newSpawn;
                    newSpawn.line = UILine.NewLine(UILineWidth, UILinesLayer);
                    newSpawn.line.SetTail(TailLength);
                    newSpawn.ControlSpawnTime = newSpawn.line.tail.gameObject.AddComponent<LevelEditorCommandDragControl>();
                    newSpawn.ControlSpawnTime.DragAxis = LevelEditorCommandDragControl.Axes.Y;
                    newSpawn.ControlSpawnTime.ControlInput = LevelEditorCommandDragControl.ControlInputs.SpawnTime;
                    newSpawn.ControlSpawnTime.command = newSpawn;
                    newSpawn.ControlSpawnTime.SetActive(false);

                    //TODO: add a component to tail that gets activated when the command is selected, allowing us to move the tail in the timeline (adjusting the spawn time/position of the command)
                    newSpawn.baseColor = CommandLineColor;

                    int multiples = 0;
                    float multipleDelay = 0;
                    float multipleOffset = 0;

                    while (GetNextArgument())
                    {
                        switch (arg)
                        {
                            case "Animation":
                                GameObject spawnedObParent = new GameObject(newOb.name);
                                spawnedObParent.transform.position = newOb.transform.position;
                                newOb.transform.SetParent(spawnedObParent.transform, true);
                                newSpawn.anim = newOb.GetComponent<Animation>();
                                AnimationClip animClip = LoadAssets[val].asset as AnimationClip;
                                newSpawn.anim.clip = animClip;
                                newSpawn.anim.AddClip(animClip, animClip.name);
                                newSpawn.anim.Stop();

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

                    DetermineCommandLifespan(newSpawn);

                    Spawner spawner = newOb.GetComponent<Spawner>();
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
                        while(SpawnerTimer < newSpawn.Life)
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
                            spawnedOb.gameObject.AddComponent<LevelEditorSpawnedCommand>().command = newSpawn;
                            //TODO: handle animation (or just strip that option from the spawner class, that might be better)

                            LevelEditorSpawnedCommand.SpawnedObjectContainer newChild = new LevelEditorSpawnedCommand.SpawnedObjectContainer(spawnedOb, spawnedOb.transform.position, SpawnerTimer + TempTime, SpawnerDistance + TempPosition);

                            tempSpawnerChildren.Add(newChild);

                            newChild.TriggerOffsetTime = SpawnerTimer;
                            DetermineCommandLifespan(newChild);
                            newChild.line = UILine.NewLine(UILineWidth, UILinesLayer);
                            newChild.baseColor =SpawnerChildLineColor;

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

                        newSpawn.SpawnerChildren = tempSpawnerChildren.ToArray();
                        //spawners may not use the multiples command
                    }
                    else if (multiples > 0)
                    {
                        newSpawn.SpawnerChildren = new LevelEditorSpawnedCommand.SpawnedObjectContainer[multiples-1];
                        for(int i = 1; i < multiples; i++)
                        {
                            GameObject child;
                            if (newSpawn.anim)
                            {
                                child = Instantiate(newOb.transform.parent.gameObject).transform.GetChild(0).gameObject;
                                child.transform.parent.position += new Vector3(0, multipleOffset * i, 0);
                            }
                            else
                            {
                                child = Instantiate(newOb);
                                child.transform.position += new Vector3(0, multipleOffset * i, 0);
                            }

                            child.GetComponent<LevelEditorSpawnedCommand>().command = newSpawn; //the object we cloned already has this component, but in the cloning it seems like the component loses its pointer so point it back at the same thing again

                            float childTime = (multipleDelay * i);
                            LevelEditorSpawnedCommand.SpawnedObjectContainer childContainer = new LevelEditorSpawnedCommand.SpawnedObjectContainer(child, child.transform.position, TempTime + childTime, GetDistanceTraveledAtTime(childTime));
                            
                            newSpawn.SpawnerChildren[i - 1] = childContainer;

                            childContainer.TriggerOffsetTime = childTime;
                            childContainer.line = UILine.NewLine(UILineWidth, UILinesLayer);
                            childContainer.baseColor = MultipleChildLineColor;
                            DetermineCommandLifespan(childContainer);
                        }
                    }

                    newSpawn.Deselect();
                    newSpawn.HoverExit();

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

    public static void DetermineCommandLifespan(LevelEditorSpawnedCommand.SpawnedObjectContainer container)
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
            container.Life = GetTimeFromDistanceTraveled(TravelDistance, container.TriggerTime);
        }
        else if (anim)
        {
            container.Life = anim.clip.length;
            container.anim = anim;
        }
        //TODO: calculate lifespan for path

        else
        {
            container.Life = LevelDuration - container.TriggerTime;
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
        current.activeSelectionName.text = $"Ob: {commandOb.command.obj.name} \n Line {commandOb.command.CommandIndex}";
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

    public static void UpdateCommand(LevelEditorSpawnedCommand.SpawnedObjectContainer container)
    {
        current.UpdateLevelScroll(EditorTime / LevelDuration, container);
    }

    void UpdateLevelScroll(float f)
    {
        UpdateLevelScroll(f, null);
    }
    void UpdateLevelScroll(float f, LevelEditorSpawnedCommand.SpawnedObjectContainer individualContainer)
    {
        EditorTime = Mathf.Clamp(f, .001f, .999f) * LevelDuration; //tiny offset to make sure commands at time 0 are present

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
            foreach (LevelEditorSpawnedCommand.SpawnedObjectContainer spawnedOb in SpawnedObjects)
            {
                UpdateContainer(spawnedOb);
            }
            UpdateUILines();
        }


        void UpdateContainer(LevelEditorSpawnedCommand.SpawnedObjectContainer container)
        {

            UpdateSubcontainer(container);
            if (container.SpawnerChildren != null)
            {
                foreach (LevelEditorSpawnedCommand.SpawnedObjectContainer spawnedObChild in container.SpawnerChildren)
                {
                    UpdateSubcontainer(spawnedObChild);
                }
            }
        }

        void UpdateSubcontainer(LevelEditorSpawnedCommand.SpawnedObjectContainer container)
        {

            DistanceSinceTrigger = ScrollPosition - container.TriggerPosition;
            TimeSinceTrigger = EditorTime - container.TriggerTime;
            float LifeEnd = LevelLength;
            if (container.Life > 0)
            {
                LifeEnd = container.TriggerTime + container.Life;
            }
            if (container.TriggerTime < EditorTime && LifeEnd > EditorTime)
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

    void BuildScrollSpeedCache()
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
                    HoldTime += LevelHoldDelay;
                    TempTime += LevelHoldDelay;
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
                LevelDuration = TempTime;
                Debug.Log($"Level duration: {LevelDuration}");
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
        LevelLength = GetDistanceTraveledAtTime(LevelDuration);
        levelScroll.size = Mathf.Max(ScreenHeight / LevelLength, .05f);
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

    void UpdateUILine(LevelEditorSpawnedCommand.SpawnedObjectContainer container)
    {
        float VertPos = (container.TriggerTime / LevelDuration * (1 - levelScroll.size) + levelScroll.size / 2) * ScreenHeight - ScreenHeight / 2;
        UpdateUISubLine(container, new Vector3(LevelWidth / 2, VertPos));

        if (container.SpawnerChildren != null)
        {
            foreach (LevelEditorSpawnedCommand.SpawnedObjectContainer childContainer in container.SpawnerChildren)
            {
                UpdateUISubLine(childContainer, container.obj.transform.position);
            }
        }

        void UpdateUISubLine(LevelEditorSpawnedCommand.SpawnedObjectContainer container, Vector3 target)
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
