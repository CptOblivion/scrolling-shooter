using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelEditor : MonoBehaviour
{
    public Text scrollTimeReadout;
    class SpawnedObjectContainer
    {
        public GameObject obj;
        public Vector3 StartPosition;
        public float TriggerTime;
        public float TriggerPosition;
        public float Life;
        public Animation anim = null;
        public EnemyPath path = null;
        public bool parallaxScroll = false;
        public int CommandIndex;
        public int ParentSpawnedObject;
        public SpawnedObjectContainer[] SpawnerChildren;

        public SpawnedObjectContainer(GameObject gameObject, int commandIndex, Vector3 startPosition, float triggerTime, float triggerPos)
        {
            CommandIndex = commandIndex;
            InstantiateBase(gameObject, startPosition, triggerTime, triggerPos);
        }

        public SpawnedObjectContainer(GameObject gameObject, Vector3 startPosition, float triggerTime, float triggerPos)
        {
            InstantiateBase(gameObject, startPosition, triggerTime, triggerPos);
        }

        private void InstantiateBase(GameObject gameObject, Vector3 startPosition, float triggerTime, float triggerPos)
        {
            obj = gameObject;
            StartPosition = startPosition;
            TriggerTime = triggerTime;
            TriggerPosition = triggerPos;
            Life = -1;
            ParentSpawnedObject = -1;
        }
    }

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

    readonly List<ScrollSpeedChange> ScrollSpeedCache = new List<ScrollSpeedChange>();

    enum States {NoLevel, Loading, Editing, Testing}
    public static float EditorTime = 0;

    public TextAsset Level;
    public Scrollbar levelScroll;
    float LevelLength;
    float LevelDuration = 0;
    int ActiveCommand;
    int[] SelectedCommands;
    public static readonly float ScreenHeight = 48;
    readonly float LevelWidth = 96;
    public float ScrollPosition;
    public List<LevelParser.LevelLine> LevelParsed = new List<LevelParser.LevelLine>();
    
    readonly Dictionary<string, ResourceRequest> LoadAssets = new Dictionary<string, ResourceRequest>();

    States state = States.NoLevel;

    static readonly float LevelHoldDelay = 5;

    readonly List<SpawnedObjectContainer> SpawnedObjects = new List<SpawnedObjectContainer>();
    void Awake()
    {
        if (Level == null)
        {
            SelectLevel();
        }
    }

    private void OnEnable()
    {
        GlobalTools.Mode = GlobalTools.GameModes.Editor;
        levelScroll.onValueChanged.AddListener(UpdateLevelScroll);
    }
    private void OnDisable()
    {
        levelScroll.onValueChanged.RemoveListener(UpdateLevelScroll);
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
                }

                break;
            case States.Editing:
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

                    SpawnedObjectContainer newSpawn = new SpawnedObjectContainer(newOb, CurrentCommandIndex, SpawnPosition, TempTime, TempPosition);
                    SpawnedObjects.Add(newSpawn);

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

                    DetermineLifespan(newSpawn, TempTime);

                    Spawner spawner = newOb.GetComponent<Spawner>();
                    if (spawner)
                    {
                        List<SpawnedObjectContainer> tempSpawnerChildren = new List<SpawnedObjectContainer>();
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
                            //TODO: handle animation (or just strip that option from the spawner class, that might be better)

                            SpawnedObjectContainer newChild = new SpawnedObjectContainer(spawnedOb, spawnedOb.transform.position, SpawnerTimer + TempTime, SpawnerDistance + TempPosition);

                            tempSpawnerChildren.Add(newChild);
                            DetermineLifespan(newChild, SpawnerTimer + TempTime);

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
                        Debug.Log(newSpawn.SpawnerChildren.Length);

                        //TODO: simulate spawner over the course of its life, populate those items into a list, store in newSpawn.spawnerChildren
                        // also properly add children to SpawnedObjects
                        //spawners may not use the multiples command
                    }
                    else if (multiples > 0)
                    {
                        newSpawn.SpawnerChildren = new SpawnedObjectContainer[multiples];
                        for(int i = 0; i < multiples; i++)
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
                            float childTime = TempTime + (multipleDelay * i);
                            SpawnedObjectContainer childContainer = new SpawnedObjectContainer(child, child.transform.position, childTime, GetDistanceTraveledAtTime(childTime));
                            newSpawn.SpawnerChildren[i] = childContainer;
                            DetermineLifespan(childContainer, childTime);
                        }
                    }

                    void DetermineLifespan(SpawnedObjectContainer container, float time)
                    {
                        ParallaxScroll parallax = container.obj.GetComponent<ParallaxScroll>();
                        Animation anim = container.obj.GetComponent<Animation>();
                        if (parallax)
                        {
                            container.parallaxScroll = true;
                            float TopEdge = 0;
                            foreach (Renderer renderer in container.obj.GetComponentsInChildren<Renderer>())
                            {
                                if (renderer.bounds.max.y > TopEdge) TopEdge = renderer.bounds.max.y;
                            }
                            float TravelDistance = ParallaxScroll.DetermineLife(TopEdge, container.obj.transform.position.z);
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
                            container.Life = LevelDuration - container.TriggerTime + .001f;
                        }
                    }

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
        //load prefabs
        //run through each line, place in scene
        //update levelLength as we go

        UpdateLevelScroll(0);
    }

    void SelectLevel()
    {
        //TODO: popup to select existing file or create new
    }

    void UpdateLevelScroll(float f)
    {
        if (f == 0)
        {
            f = .0001f;
            //tiny offset to make sure commands at time 0 are present
        }
        EditorTime = f * LevelDuration;

        //when scrolling forward, we can just simulate the camera position from the current time to the new time
        //when scrolling backwards, we have to simulate the camera position from the start of the level to the new position
        //maybe we can cache this somehow and just rebuild the cache when scroll speed commands are entered/edited?
        ScrollPosition = GetDistanceTraveledAtTime(EditorTime);
        if (scrollTimeReadout) scrollTimeReadout.text = $"{EditorTime} \n {ScrollPosition}";
        float DistanceSinceTrigger;
        float TimeSinceTrigger;

        foreach(SpawnedObjectContainer spawnedOb in SpawnedObjects)
        {
            UpdateObject(spawnedOb);
            if (spawnedOb.SpawnerChildren != null)
            {
                foreach(SpawnedObjectContainer spawnedObChild in spawnedOb.SpawnerChildren)
                {
                    UpdateObject(spawnedObChild);
                }
            }
        }

        void UpdateObject(SpawnedObjectContainer ob)
        {

            DistanceSinceTrigger = ScrollPosition - ob.TriggerPosition;
            TimeSinceTrigger = EditorTime - ob.TriggerTime;
            float LifeEnd = LevelLength;
            if (ob.Life > 0)
            {
                LifeEnd = ob.TriggerTime + ob.Life;
            }
            if (ob.TriggerTime < EditorTime && LifeEnd > EditorTime)
            {
                ob.obj.SetActive(true);
                if (ob.parallaxScroll)
                {
                    ob.obj.transform.position = ParallaxScroll.ScrollAbsolute(ob.StartPosition, DistanceSinceTrigger);
                }
                else if (ob.anim)
                {
                    ob.anim.Play();
                    ob.anim[ob.anim.clip.name].time = TimeSinceTrigger;
                    ob.anim.Sample();
                    ob.anim.Stop();
                }
                else if (ob.path)
                {

                }
            }
            else
            {
                ob.obj.SetActive(false);
            }
        }
    }

    float GetDistanceTraveledAtTime(float TargetTime)
    {
        float currentTime = 0;
        float distance = 0;
        float speed = 0;

        for (int i = 0; i < ScrollSpeedCache.Count; i++)
        {
            ScrollSpeedChange current = ScrollSpeedCache[i];

            if (current.Time >= TargetTime)
            {
                return distance + (TargetTime - currentTime) * speed;
            }
            distance += (current.Time - currentTime) * speed;

            currentTime = current.Time;
            if (TargetTime < currentTime + current.LerpTime)
            {
                float NewLerpTime = TargetTime - currentTime;
                distance += AreaUnderLerp(speed, Mathf.Lerp(speed, current.NewSpeed, NewLerpTime/current.LerpTime), NewLerpTime);
                return distance;
            }
            distance += AreaUnderLerp(speed, current.NewSpeed, current.LerpTime);
            speed = current.NewSpeed;

            currentTime += current.LerpTime;
        }

        return distance + (TargetTime - currentTime) * speed;
    }

    float GetTimeFromDistanceTraveled(float TargetDistance, float StartTime)
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

    float AreaUnderLerp(float StartSpeed, float EndSpeed, float LerpTime)
    {
        float output = LerpTime * Mathf.Min(StartSpeed, EndSpeed); //area of the square up to the lowest part of the current chunk of the line
        output += LerpTime * Mathf.Abs(StartSpeed - EndSpeed) / 2; //area of the triangle of the change in speed
        return output;
    }
}
