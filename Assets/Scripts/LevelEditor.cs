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
        float CurrentPosition = 0;
        for(int CurrentCommandIndex = 0; CurrentCommandIndex < LevelParsed.Count; CurrentCommandIndex++)
        {
            LevelParser.LevelLine line = LevelParsed[CurrentCommandIndex];
            //TODO: handle "time" mode

            float newPosition = line.Position;
            if (!line.RelativePosition)
            {
                newPosition -= CurrentPosition;
            }
            if (ScrollModeTime)
            {
                CurrentPosition += newPosition * ScrollSpeed;
                //TODO: lerp speed
                //TODO: currently doesn't work if absolute position is backwards from current position
            }
            else
            {
                CurrentPosition += newPosition;
            }

            if (CurrentPosition > LevelLength)
            {
                LevelLength = CurrentPosition;
            }

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
                    Vector3 LevelOffset = new Vector3(0, CurrentPosition * ScrollSpeed) * -.1f; //TODO: why is this value so wrong, and wrong differently for each object
                    Vector3 SpawnPosition = LevelParser.ParseVector3(val)+PositionOffset+LevelOffset;
                    GameObject newOb = Instantiate(prefabResource.asset as GameObject, SpawnPosition, Quaternion.identity);

                    SpawnedObjects.Add(new SpawnedObjectContainer(newOb, CurrentCommandIndex, SpawnPosition, 100));
                    EnemyGeneric ai = newOb.GetComponent<EnemyGeneric>();
                    if (ai)
                    {
                        ai.enabled = false;
                    }

                    //TODO: store parallax in SpawnedObjectContainer, so we can use it while scrolling the view
                    ParallaxScroll parallax = newOb.GetComponent<ParallaxScroll>();
                    if (parallax)
                    {
                        parallax.enabled = false;
                    }

                    Spawner spawner = newOb.GetComponent<Spawner>();
                    if (spawner)
                    {
                        spawner.enabled = false;
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
        transform.position = new Vector3(0, LevelPosition, 0);
        //TODO: update enemy positions
    }


}
