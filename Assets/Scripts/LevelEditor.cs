using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelEditor : MonoBehaviour
{
    class SpawnedObjectContainer
    {
        public GameObject obj;
        public Vector3 StartPosition;
        public float StartTime;
        public float Life;
        public Animation anim = null;
        public EnemyPath path = null;
        public int CommandIndex;
        public SpawnedObjectContainer(GameObject gameObject, int commandIndex, Vector3 startPosition, float time)
        {
            obj = gameObject;
            CommandIndex = commandIndex;
            StartPosition = startPosition;
            StartTime = time;
            Life = 10;
            CommandIndex = commandIndex;
        }
    }

    enum States {NoLevel, Loading, Editing, Testing}
    public static float EditorTime = 0;
    public static float EditorDeltaTime = 0;

    bool ScrollModeTime = false;
    float ScrollSpeed = 15;

    public TextAsset Level;
    public Scrollbar levelScroll;
    float LevelLength;
    float LevelPosition = 0;
    int ActiveCommand;
    int[] SelectedCommands;
    readonly float ScreenHeight = 48;
    readonly float LevelWidth = 96;
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
                        Debug.Log(assetPath);
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

    private void LateUpdate()
    {
        EditorDeltaTime = 0;
    }

    void PopulateLevel()
    {
        //TODO: use CurrentPosition with spawn commands to populate a level preview strip (just spawn the objects along the track, render into a very super tall texture?)
        float CurrentPosition = 0;
        float CurrentTime = 0;
        for(int CurrentCommandIndex = 0; CurrentCommandIndex < LevelParsed.Count; CurrentCommandIndex++)
        {
            LevelParser.LevelLine line = LevelParsed[CurrentCommandIndex];
            //TODO: handle "time" mode

            float newPosition = line.Position;
            if (ScrollModeTime)
            {
                if (!line.RelativePosition)
                {
                    newPosition -= CurrentTime;
                }
                CurrentTime += newPosition;
                CurrentPosition += newPosition * ScrollSpeed;
                //TODO: lerp speed
                //TODO: currently doesn't work if absolute position is backwards from current position
            }
            else
            {
                if (!line.RelativePosition)
                {
                    newPosition -= CurrentPosition;
                }
                CurrentPosition += newPosition;
            }

            if (CurrentPosition > LevelLength)
            {
                LevelLength = CurrentPosition;
            }

            Debug.Log($"{CurrentPosition}, {line.Command}");

            string arg = "";
            string val = "";
            int CurrentArg = 0;

            switch (line.Command)
            {
                case LevelParser.AvailableCommands.LevelMode:
                    GetNextArgument();
                    if (val == "Distance") ScrollModeTime = false;
                    else if (val == "Time") ScrollModeTime = true;
                    break;
                case LevelParser.AvailableCommands.ScrollSpeed:
                    GetNextArgument();
                    ScrollSpeed = float.Parse(val);
                    //TODO: handle lerp
                    break;
                case LevelParser.AvailableCommands.Spawn:
                    GetNextArgument();
                    ResourceRequest prefabResource = LoadAssets[val];
                    GetNextArgument();
                    float offset = 0; //TODO: get offset from multiples command (will require rearranging this stuff a bit)
                    Vector3 PositionOffset = new Vector3(0, offset, 0);
                    Vector3 SpawnPosition = LevelParser.ParseVector3(val)+PositionOffset;
                    GameObject newOb = Instantiate(prefabResource.asset as GameObject, SpawnPosition, Quaternion.identity);

                    Debug.Log($"spawned {newOb.name} \n");

                    SpawnedObjects.Add(new SpawnedObjectContainer(newOb, CurrentCommandIndex, SpawnPosition, 100));

                    //TODO: bool in SpawnedObjectContainer to keep track of if object has parallax
                    //TODO: set path or anim
                    //TODO: determine life
                    //  if it has parallax, figure out how long it'll take to scroll offscreen (account for changes in move speed?)
                    //  if it has an animation, just base it on the duration of the animation
                    //  if it has a path, same deal
                    //TODO: see UpdateLevelScroll for udpating level position todo list
                    ParallaxScroll parallax = newOb.GetComponent<ParallaxScroll>();
                    if (parallax)
                    {
                        parallax.enabled = false;
                        //TODO: determine life 
                    }

                    //TODO: for all these helper components, have a base HelperComponent class that checks if we're in game or editor so we don't have to disable everything here
                    //  although stuff like spawners should probably be recorded somehow on instantiation, so we can see them happen on scrolling up and down
                    Spawner spawner = newOb.GetComponent<Spawner>();
                    if (spawner)
                    {
                        spawner.enabled = false;
                    }
                    EnemyGeneric ai = newOb.GetComponent<EnemyGeneric>();
                    if (ai)
                    {
                        ai.enabled = false;
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
    }

    void SelectLevel()
    {
        //TODO: popup to select existing file or create new
    }

    void UpdateLevelScroll(float f)
    {
        EditorDeltaTime = f*LevelLength - LevelPosition;
        LevelPosition = f * LevelLength;
        transform.position = new Vector3(0, LevelPosition, -10);
        //TODO: update enemy positions
        //  figure out which enemies should currently be alive (if we're after start time but before start time + life)
        //  figure out where on the screen enemy should be
        //      if it has parallax, perform a parallax scroll from its spawn point
        //      if it has animation, just set the frame
        //      if it has a path, determine the position along the path
    }


}
