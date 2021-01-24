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
        public SpawnedObjectContainer[] SpawnerChildren; //TODO: set this up

        public SpawnedObjectContainer(GameObject gameObject, int commandIndex, Vector3 startPosition, float triggerTime, float triggerPos)
        {
            obj = gameObject;
            CommandIndex = commandIndex;
            StartPosition = startPosition;
            TriggerTime = triggerTime;
            TriggerPosition = triggerPos;
            Life = -1;
            CommandIndex = commandIndex;
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

    float ScrollSpeed = 15;

    public TextAsset Level;
    public Scrollbar levelScroll;
    float LevelLength;
    float LevelDuration = 0;
    float LevelPosition = 0;
    int ActiveCommand;
    int[] SelectedCommands;
    readonly float ScreenHeight = 48;
    readonly float LevelWidth = 96;
    public float ScrollPosition;
    public List<LevelParser.LevelLine> LevelParsed = new List<LevelParser.LevelLine>();
    
    readonly Dictionary<string, ResourceRequest> LoadAssets = new Dictionary<string, ResourceRequest>();

    States state = States.NoLevel;

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
                    List<string> PathsToLoad;
                    LevelParsed = LevelParser.ParseFile(Level, out PathsToLoad);
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
        float TempPosition = 0;
        float TempTime = 0;

        BuildScrollSpeedCache();

        for (int CurrentCommandIndex = 0; CurrentCommandIndex < LevelParsed.Count; CurrentCommandIndex++)
        {
            LevelParser.LevelLine line = LevelParsed[CurrentCommandIndex];
            //TODO: handle "time" mode
            float newTime = line.Time;
            if (!line.RelativePosition)
            {
                newTime -= TempTime;
            }
            TempTime += newTime;

            //TODO: this is wasteful
            //optimizations:
            //  if current command is at same time as last, don't update TempPosition
            //  some way to get the time of the last speed change, and just calculate forward from there?
            TempPosition = GetDistanceTraveledAtTime(TempTime);

            if (TempPosition > LevelLength)
            {
                LevelLength = TempPosition;
            }

            if (CurrentCommandIndex == LevelParsed.Count - 1)
            {
                LevelDuration = TempTime;
                Debug.Log($"Level duration: {LevelDuration}");
            }

            string arg = "";
            string val = "";
            int CurrentArg = 0;

            switch (line.Command)
            {
                case LevelParser.AvailableCommands.Spawn:
                    GetNextArgument();
                    ResourceRequest prefabResource = LoadAssets[val];
                    GetNextArgument();
                    float offset = 0; //TODO: get offset from multiples command (will require rearranging this stuff a bit)
                    Vector3 PositionOffset = new Vector3(0, offset, 0);
                    Vector3 SpawnPosition = LevelParser.ParseVector3(val)+PositionOffset;
                    GameObject newOb = Instantiate(prefabResource.asset as GameObject, SpawnPosition, Quaternion.identity);

                    SpawnedObjectContainer newSpawn = new SpawnedObjectContainer(newOb, CurrentCommandIndex, SpawnPosition, TempTime, TempPosition);
                    SpawnedObjects.Add(newSpawn);

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
                                newSpawn.Life = newSpawn.anim.clip.length;

                                break;
                            case "Path":
                                //TODO: implement
                                //TODO: determine life
                                break;
                            case "Repeat":
                                //TODO: implement (requires breaking spawn code out into a function, forgot to start with that)
                                break;
                        }
                    }

                    //TODO: move the following stuff to a function (since it'll need to call itself for adding things added by a spawner)
                    ParallaxScroll parallax = newOb.GetComponent<ParallaxScroll>();
                    if (parallax)
                    {
                        newSpawn.parallaxScroll = true;
                        //TODO: get the actual object bound (check against all the renders in the object)
                        float TopEdge = 0;
                        foreach(Renderer renderer in newOb.GetComponentsInChildren<Renderer>())
                        {
                            if (renderer.bounds.max.y > TopEdge) TopEdge = renderer.bounds.max.y;
                        }
                        float TravelDistance = ParallaxScroll.DetermineLife(TopEdge, newOb.transform.position.z);

                        //TODO: convert TravelDistance into a time, using ScrollSpeedCache and GetDistanceTraveledAtTime somehow
                        newSpawn.Life = 100; //placeholder value
                    }

                    Spawner spawner = newOb.GetComponent<Spawner>();
                    if (spawner)
                    {
                        //TODO: simulate spawner over the course of its life, populate those items into a list, store in newSpawn.spawnerChildren
                        // also properly add children to SpawnedObjects
                    }

                    Debug.Log($"{newOb}, {newSpawn.TriggerTime}, {newSpawn.TriggerPosition}");

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
        //TODO: this scroll bar has to be converted to represent time, not distance traveled
        if (f == 0)
        {
            f = .0001f;
            //tiny offset to make sure commands at time 0 are present
        }
        EditorTime = f * LevelDuration;
        if (scrollTimeReadout) scrollTimeReadout.text = EditorTime.ToString();

        //when scrolling forward, we can just simulate the camera position from the current time to the new time
        //when scrolling backwards, we have to simulate the camera position from the start of the level to the new position
        //maybe we can cache this somehow and just rebuild the cache when scroll speed commands are entered/edited?
        ScrollPosition = GetDistanceTraveledAtTime(EditorTime);
        float DistanceSinceTrigger;
        float TimeSinceTrigger;

        foreach(SpawnedObjectContainer spawnedOb in SpawnedObjects)
        {
            DistanceSinceTrigger = ScrollPosition - spawnedOb.TriggerPosition;
            TimeSinceTrigger = EditorTime - spawnedOb.TriggerTime;
            float LifeEnd = LevelLength;
            if (spawnedOb.Life > 0)
            {
                LifeEnd = spawnedOb.TriggerTime + spawnedOb.Life;
            }
            if (spawnedOb.TriggerTime < EditorTime && LifeEnd > EditorTime)
            {
                spawnedOb.obj.SetActive(true);
                if (spawnedOb.parallaxScroll)
                {
                    spawnedOb.obj.transform.position = ParallaxScroll.ScrollAbsolute(spawnedOb.StartPosition, DistanceSinceTrigger);
                }
                else if (spawnedOb.anim)
                {
                    spawnedOb.anim.Play();
                    spawnedOb.anim[spawnedOb.anim.clip.name].time = TimeSinceTrigger;
                    spawnedOb.anim.Sample();
                    spawnedOb.anim.Stop();
                }
                else if (spawnedOb.path)
                {

                }
            }
            else
            {
                spawnedOb.obj.SetActive(false);
            }
        }
    }

    float GetDistanceTraveledAtTime(float TargetTime)
    {
        //TODO: the distance this returns is SUPER wrong
        float currentTime = 0;
        float distance = 0;
        float speed = 0;

        for (int cacheIndex = 0; cacheIndex < ScrollSpeedCache.Count; cacheIndex++)
        {
            ScrollSpeedChange current = ScrollSpeedCache[cacheIndex];

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

    void BuildScrollSpeedCache()
    {
        //TODO: in the editor, ensure no scroll speed changes overlap one another
        //TODO: rework with actual area-under-the-curve stuff
        float TempTime = 0;
        //float TempDistance = 0;
        //float TempSpeed = 0;
        ScrollSpeedCache.Clear();
        //first, once around to populate ScrollSpeedRef
        for (int CurrentCommandIndex = 0; CurrentCommandIndex < LevelParsed.Count; CurrentCommandIndex++)
        {
            LevelParser.LevelLine line = LevelParsed[CurrentCommandIndex];
            if (line.RelativePosition)
            {
                //TempDistance += line.Time * TempSpeed;
                TempTime += line.Time;
            }
            else
            {
                //TempDistance += line.Time-TempTime * TempSpeed;
                TempTime = line.Time;
            }
            if (line.Command == LevelParser.AvailableCommands.ScrollSpeed)
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
        }
    }

    float AreaUnderLerp(float StartSpeed, float EndSpeed, float LerpTime)
    {
        float output = LerpTime * Mathf.Min(StartSpeed, EndSpeed); //area of the square up to the lowest part of the current chunk of the line
        output += LerpTime * Mathf.Abs(StartSpeed - EndSpeed) / 2; //area of the triangle of the change in speed
        return output;
    }
}
