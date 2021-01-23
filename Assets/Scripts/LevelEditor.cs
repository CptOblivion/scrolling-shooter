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
        public float PositionAtTime = -1;
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
                                break;
                            case "Repeat":
                                //TODO: implement (requires breaking spawn code out into a function, forgot to start with that)
                                break;
                        }
                    }
                    //TODO: determine life
                    //  if it has an animation, just base it on the duration of the animation
                    //  if it has a path, same deal
                    //TODO: see UpdateLevelScroll for udpating level position todo list
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
        ScrollPosition = GetDistanceTraveledAtTime(f);
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
        return GetDistanceTraveledAtTime(TargetTime, 0);
    }

    float GetDistanceTraveledAtTime(float TargetTime, float StartTime)
    {
        //TODO: the distance this is returning is SUPER wrong
        float currentTime = StartTime;
        float distance = 0;
        float speed = 24;
        float TimeStep = 1f / 60; //we can probably bump this down to 30, maybe even 15, without losing too much precision
        //TODO: test precision with different timesteps
        float LerpStartSpeed;

        bool Seeking = true;


        for (int cacheIndex = 0; cacheIndex < ScrollSpeedCache.Count; cacheIndex++)
        {
            ScrollSpeedChange current = ScrollSpeedCache[cacheIndex];

            if (Seeking)
            {
                if (current.Time <= StartTime) //this is the first entry that's later than the start time
                {
                    //get the details of the last entry before the start time
                    current = ScrollSpeedCache[cacheIndex-1];

                    distance = current.PositionAtTime;
                    currentTime = current.Time;
                    Seeking = false;
                    cacheIndex-=2;
                }
            }
            else
            {
                if (current.Time >= TargetTime)
                {
                    return distance + (TargetTime - currentTime) * speed;
                }
                distance += (current.Time - currentTime) * speed;
                LerpStartSpeed = speed;
                for (float t = 0; t < current.LerpTime; t += TimeStep)
                {
                    speed = Mathf.Lerp(LerpStartSpeed, current.NewSpeed, t / current.LerpTime);
                    distance += speed;
                    if (currentTime + t > TargetTime) return distance;
                }
            }
        }

        return distance + (TargetTime - currentTime) * speed;
    }

    void BuildScrollSpeedCache()
    {
        //TODO: in the editor, ensure no scroll speed changes overlap one another
        //TODO: rework with actual area-under-the-curve stuff
        float TempTime = 0;
        ScrollSpeedCache.Clear();
        //first, once around to populate ScrollSpeedRef
        for (int CurrentCommandIndex = 0; CurrentCommandIndex < LevelParsed.Count; CurrentCommandIndex++)
        {
            LevelParser.LevelLine line = LevelParsed[CurrentCommandIndex];
            if (line.RelativePosition)
            {
                TempTime += line.Time;
            }
            else
            {
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
                ScrollSpeedCache[ScrollSpeedCache.Count - 1].PositionAtTime = GetDistanceTraveledAtTime(TempTime);
            }
        }
    }
}
