using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Audio;

[RequireComponent(typeof(Camera))]
public class LevelController : MonoBehaviour
{

    class RepeatUnit
    {
        public LevelParser.LevelLine Line;
        public int UnitsRemaining;
        public float Delay;
        public float DelayTimer;
        public float Offset;
        public float CurrentOffset;
        public RepeatUnit (int count, float delay, float offset, LevelParser.LevelLine line)
        {
            Line = line;
            UnitsRemaining = count;
            Delay = DelayTimer = delay;
            Offset = offset;
            CurrentOffset = 0;
        }
    }

    //
    //dumb debug stuff, remove before proper build
    public float simulateLoading = 0;
    //
    //


    [HideInInspector]
    public InputActionMap actionMapGameplay;
    [HideInInspector]
    public InputActionMap actionMapMenus;

    public static LevelController current;

    public PlayerInput playerInput;

    Dictionary<string,ResourceRequest> LoadAssets;

    [Tooltip("the Pause Canvas should be dropped in here, so we can enable it at runtime")]
    public Canvas pauseCanvas;
    [Tooltip("the Display Canvas should be dropped in here, so we can enable it at runtime")]
    public Canvas displayCanvas;
    [Tooltip("the HUD Canvas should be dropped in here, so we can enable it at runtime")]
    public Canvas hudCanvas;
    public Canvas settingsCanvas;
    public Canvas loadingCanvas;
    [Tooltip("For now, dragon drop the player object into this box")]
    public PlayerControl playerControl;
    Transform Player;

    public AudioMixer audioMixer;
    AudioSource[] musicSources;

    //we'll be using currentMusicSource as an int but it's convenient to be able to do !currentMusicSource to get the other one
    //read from musicSources with musicSources[currentMusicSource?1:0] (or 0:1 for the not-current music source)
    bool currentMusicSource = true;
    float musicLerpSpeed = 0;
    float musicLerpProgress = 0;

    [HideInInspector]
    public bool Paused = false;

    [Tooltip("This is the level file we'll be parsing")]
    public TextAsset Level;

    //TODO: replace levelParsed string array entries with a class, which holds level position/time, command, and details (using enums for commands, etc)

    [Tooltip("The speed the level is scrolling at, used for parsing the level as well as moving background elements to make it appear like the camera is scrolling")]
    public float ScrollSpeed = 15;
    //these are all for tracking when the camera is changing speed over a period of time
    float ScrollSpeedLerpStart, ScrollSpeedLerpTarget, ScrollSpeedLerpSpeed, ScrollSpeedLerpProgress;
    [HideInInspector]
    public float LevelPosition = 0;
    [HideInInspector]
    public float LevelPositionRelative = 0;
    [HideInInspector]
    public float LevelTime = 0;
    [HideInInspector]
    public float LevelTimeRelative = 0;
    [HideInInspector]
    public int LevelHolding = 0; //when greater than 0, level progress holds until something in the scene resumes it
    [HideInInspector]
    public bool PlayerDead = false; //if the player dies, halt the level file (no winnin' during the death animation for you, bucko)
    [HideInInspector]
    public bool loading = true;


    [Tooltip("width of the level (camera and player horizontal scrolling limits)")]
    public float LevelWidth = 96;
    CompositeCollider2D playerCollider;

    public Text debugText;

    [HideInInspector]
    public float PlayerWidth;

    Camera cam;

    List<LevelParser.LevelLine> levelParsed;
    int LevelCommandIndex = 0;

    readonly List<RepeatUnit> RepeatUnits = new List<RepeatUnit>();

    private void Awake()
    {
        current = this;
        cam = GetComponent<Camera>();
        //initialize basic links
        GlobalTools.canvas_Display = displayCanvas;

        //initialize input stuff
        playerInput = GetComponent<PlayerInput>();
        actionMapGameplay = playerInput.actions.FindActionMap("Gameplay");
        actionMapMenus = playerInput.actions.FindActionMap("Menus");

        //initialize audio stuff
        GlobalTools.audioMixer = audioMixer;
        AudioSource sourceA = this.gameObject.AddComponent<AudioSource>();
        AudioSource sourceB = this.gameObject.AddComponent<AudioSource>();
        musicSources = new AudioSource[] {sourceA,sourceB};
        foreach (AudioSource audioSource in musicSources)
        {
            audioSource.loop = true;
            if (audioMixer) audioSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Music")[0];
        }

        //initialize player character stuff
        playerControl = Instantiate(playerControl.gameObject).GetComponent<PlayerControl>(); //probably a mistake to replace the prefab with the reference but I'm curious what'll happen

        Player = playerControl.gameObject.transform;
        playerCollider = Player.gameObject.GetComponent<CompositeCollider2D>();

    }
    void Start()
    {
        //CloseSettings();
        //Unpause();
        GlobalTools.Mode = GlobalTools.GameModes.Play;
        Cursor.lockState = CursorLockMode.None;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        displayCanvas.gameObject.SetActive(true);
        loadingCanvas.gameObject.SetActive(true);

        hudCanvas.gameObject.SetActive(false);
        pauseCanvas.gameObject.SetActive(false);
        settingsCanvas.gameObject.SetActive(false);
        Player.gameObject.SetActive(false);

        PlayerWidth = playerCollider.bounds.extents.x;

        ScrollSpeedLerpSpeed = 0;

        //TODO: check what this is about
        if (GlobalTools.level) Level = GlobalTools.level;

        LoadAssets = new Dictionary<string, ResourceRequest>();

        if (Level != null)
        {
            levelParsed = LevelParser.ParseFile(Level, out List<string> PathsToLoad);
            foreach (string assetPath in PathsToLoad)
            {
                LoadAssets.Add(assetPath, Resources.LoadAsync(assetPath));
            }
        }
    }


    void Update()
    {
        //debugText.text = playerInput.controlScheme;
        if (loading)
        {
            bool DoneLoading = true;
            foreach (ResourceRequest loadAsset in LoadAssets.Values)
            {
                if (!loadAsset.isDone)
                {
                    DoneLoading = false;
                }
            }

            if (simulateLoading > 0) simulateLoading -= Time.deltaTime; //force the loading screen to stick around for a bit for debugging
            if (DoneLoading && simulateLoading <= 0)
            {
                loading = false;

                string debugString = "Loaded Assets: \n";
                foreach(string name in LoadAssets.Keys)
                {
                    debugString += name + "\n";
                }
                hudCanvas.gameObject.SetActive(true);
                Player.gameObject.SetActive(true);
                loadingCanvas.gameObject.SetActive(false);
                //pauseCanvas.gameObject.SetActive(true);
                //settingsCanvas.gameObject.SetActive(true);
                playerInput.SwitchCurrentActionMap("Menus");
                playerInput.SwitchCurrentActionMap("Gameplay");
                Debug.Log(debugString);
            }
        }

        if(!loading) //mot using "else" because we don't want to wait for the next frame if loading just finished
        {
            playerCollider = Player.gameObject.GetComponent<CompositeCollider2D>();
            PlayerWidth = playerCollider.bounds.extents.x;
            //
            //building the level
            //

            //for some reason low-level errors start to happen if there are events triggered by the Gameplay action map on the Player Input component
            //this seems to be related to "Pause" sometimes being unable to leave Waiting
            //if (inputActionMapGameplay.GetAction("Pause").phase == InputActionPhase.Started) Pause();

            if (levelParsed != null && LevelHolding <= 0 && !PlayerDead && !Paused)
            {

                //time to loop through the parsed level until we hit a command we haven't reached yet
                bool ExitLoop = false;
                while (!ExitLoop) //risky!
                {
                    float NextCommandDistance;
                    float LevelProgress;
                    NextCommandDistance = levelParsed[LevelCommandIndex].Time;

                    //TODO: look over this, figure out what it's doing and why it's this way
                    if (levelParsed[LevelCommandIndex].RelativePosition)
                    {
                        LevelProgress = LevelTimeRelative;
                    }
                    else //absolute offset mode
                    {
                        LevelProgress = LevelTime;
                    }

                    if (LevelProgress < NextCommandDistance) break; //we haven't reached the next command yet so let's just leave the loop until the next frame, alright?

                    //if we passed that break we've hit a command, so reset the relative position counters
                    //TODO: these should probably be allowed to go negative, in case we passed several very close together but not 0 apart commands between frames (or is that already handled?)
                    LevelPositionRelative = 0;
                    LevelTimeRelative = 0;
                    LevelParser.LevelLine line = levelParsed[LevelCommandIndex]; //the current line we're working on
                    Debug.Log($"Command index {LevelCommandIndex}, pos: {LevelPosition}, Time: {LevelTime}");

                    int CurrentArg = 0;
                    string arg = "";
                    string val = "";

                    switch (line.Command)
                    {
                        case LevelParser.AvailableCommands.ScrollSpeed:
                            GetNextArgument();
                            float OldScrollSpeed = ScrollSpeed; //store in case we want to lerp from this value
                            ScrollSpeed = float.Parse(val);
                            while (GetNextArgument())
                            {
                                switch (arg)
                                {
                                    case "Lerp":
                                        ScrollSpeedLerpStart = OldScrollSpeed;
                                        ScrollSpeedLerpSpeed = 1 / float.Parse(val);
                                        ScrollSpeedLerpTarget = ScrollSpeed;
                                        ScrollSpeed = OldScrollSpeed;
                                        break;
                                }
                            }
                            break;
                        case LevelParser.AvailableCommands.Spawn:
                            foreach(LevelParser.LevelCommandArgument argument in line.Arguments)
                            {
                                if (argument.Argument == "Multiple" || argument.Argument == "Several" || argument.Argument == "Repeat")
                                {
                                    Vector3 vec = LevelParser.ParseVector3(argument.Value);
                                    RepeatUnits.Add(new RepeatUnit((int)vec[0], vec[1], vec[2], line));
                                    break;
                                }
                            }
                            SpawnObject(line);
                            break;
                        case LevelParser.AvailableCommands.DisplayText:
                            GetNextArgument();
                            GameObject textOb = Instantiate(Resources.Load("GenericText", typeof(GameObject)) as GameObject); //TODO: store a Text object in a public variable so Unity loads it automatically
                            textOb.transform.SetParent(hudCanvas.transform, false);
                            Text textComponent = textOb.GetComponent<Text>();
                            textComponent.text = arg; //storing text value in arg instead of val just for inspector readability, probably a bad idea
                            GetNextArgument();
                            float TextLife = float.Parse(val);
                            textOb.GetComponent<EffectKill>().life = TextLife; //do we need this component? Can't we call Destroy with a time? (check if the component does more than just destroy)
                            while (GetNextArgument())
                            {
                                switch (arg)
                                {
                                    case "Position":
                                        textOb.transform.localPosition = LevelParser.ParseVector3(val);
                                        break;
                                    case "Size":
                                        textComponent.fontSize = int.Parse(val);
                                        break;
                                    case "Color":
                                        textComponent.color = LevelParser.ParseVector4(val);
                                        break;
                                }
                            }
                            break;
                        case LevelParser.AvailableCommands.PlaySound:
                            float pitch = 1;
                            float volume = 1;
                            float spatialBlend = 0;
                            Vector3 Position = Vector3.zero;
                            GetNextArgument();
                            //AudioClip sound = LoadAssets[$"Sounds/{val}"].asset as AudioClip;
                            AudioClip sound = LoadAssets[val].asset as AudioClip;
                            while (GetNextArgument())
                            {
                                switch (arg)
                                {
                                    case "Volume":
                                        volume = float.Parse(val);
                                        break;
                                    case "Position":
                                        Position = LevelParser.ParseVector3(val);
                                        break;
                                    case "Pitch":
                                        pitch = float.Parse(val);
                                        break;
                                }
                            }
                            AudioSource audioSource = GlobalTools.PlaySound(sound, pitch);
                            audioSource.volume = volume;
                            audioSource.spatialBlend = spatialBlend;
                            audioSource.gameObject.transform.position = Position;
                            break;
                        case LevelParser.AvailableCommands.PlayMusic:
                            GetNextArgument();
                            //AudioClip music = Resources.Load($"Sounds/Music/{val}") as AudioClip;
                            AudioClip music = Resources.Load(val) as AudioClip;
                            currentMusicSource = !currentMusicSource;

                            //this whole chunk should probably go into a function
                            musicSources[currentMusicSource ? 0 : 1].clip = music;
                            musicSources[currentMusicSource ? 0 : 1].Play();
                            musicSources[currentMusicSource ? 0 : 1].volume = 1;
                            musicSources[currentMusicSource ? 1 : 0].volume = 0;
                            while (GetNextArgument())
                            {
                                switch (arg)
                                {
                                    case "Crossfade":
                                    case "Lerp":
                                        musicLerpProgress = 1;
                                        musicLerpSpeed = 1 / float.Parse(val);
                                        break;
                                    case "Intro":
                                        //AudioClip introMusic = Resources.Load($"Sounds/Music/{val}") as AudioClip;
                                        AudioClip introMusic = Resources.Load(val) as AudioClip;

                                        //the whole following chunk should probably go into a function
                                        musicSources[currentMusicSource ? 1 : 0].clip = introMusic; 
                                        musicSources[currentMusicSource ? 1 : 0].Play();
                                        musicSources[currentMusicSource ? 1 : 0].volume = 1;
                                        musicSources[currentMusicSource ? 1 : 0].loop = false;
                                        musicSources[currentMusicSource ? 0 : 1].Stop();
                                        musicSources[currentMusicSource ? 0 : 1].PlayScheduled(AudioSettings.dspTime + introMusic.length);
                                        break;
                                }
                            }

                            break;
                        case LevelParser.AvailableCommands.HoldForDeath:
                            bool additive = false;
                            int count = 1;
                            while (GetNextArgument())
                            {
                                switch (arg)
                                {
                                    case "Count":
                                        count = int.Parse(val);
                                        break;
                                    case "Additive":
                                        additive = true;
                                        break;
                                }
                            }
                            if (additive) LevelHolding += count;
                            else LevelHolding = count;
                            if (LevelHolding > 0) //if we're actually holding
                            {
                                ExitLoop = true;
                            }
                            break;
                        case LevelParser.AvailableCommands.VictoryAnim:
                            playerControl.PlayVictoryAnimation();
                            break;
                        case LevelParser.AvailableCommands.LevelEnd:
                            Debug.Log("Level finished");
                            GlobalTools.EndLevel(false);
                            ExitLoop = true;
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
                    LevelCommandIndex++;

                    //cleanup just in case a level is left open at the end
                    //this means the player will never see whatever the last command was, of course, since the level ends on the next frame
                    if (!ExitLoop && LevelCommandIndex == levelParsed.Count)
                    {
                        Debug.Log("no LevelEnd in level file! Ending...");
                        levelParsed.Add(new LevelParser.LevelLine {RelativePosition=true,Time=0,  Command=LevelParser.AvailableCommands.LevelEnd });
                        ExitLoop = true;
                    }
                }

                //update all the position and time variables
                LevelPosition += ScrollSpeed * Time.deltaTime;
                LevelPositionRelative += ScrollSpeed * Time.deltaTime;
                LevelTime += Time.deltaTime;
                LevelTimeRelative += Time.deltaTime;

                if (musicLerpProgress > 0)
                {
                    musicLerpProgress -= musicLerpSpeed * Time.deltaTime;
                    musicSources[currentMusicSource ? 1 : 0].volume = musicLerpProgress; //old music, ramping down
                    musicSources[currentMusicSource ? 0 : 1].volume = 1 - musicLerpProgress; //new music, ramping up
                }

                for(int i = 0; i < RepeatUnits.Count; i++)
                {
                    RepeatUnits[i].DelayTimer -= Time.deltaTime;
                    if (RepeatUnits[i].DelayTimer <= 0)
                    {
                        RepeatUnits[i].CurrentOffset += RepeatUnits[i].Offset;
                        SpawnObject(RepeatUnits[i].Line, RepeatUnits[i].CurrentOffset);
                        RepeatUnits[i].UnitsRemaining--;
                        RepeatUnits[i].DelayTimer = RepeatUnits[i].Delay;
                        if (RepeatUnits[i].UnitsRemaining == 1)
                        {
                            RepeatUnits.RemoveAt(i);
                            i--;
                        }
                    }
                }

            }


            //scroll speed lerp handling
            if (ScrollSpeedLerpSpeed > 0)
            {
                ScrollSpeedLerpProgress += ScrollSpeedLerpSpeed * Time.deltaTime;
                ScrollSpeed = Mathf.Lerp(ScrollSpeedLerpStart, ScrollSpeedLerpTarget, ScrollSpeedLerpProgress);
                if (ScrollSpeedLerpProgress >= 1) ScrollSpeedLerpSpeed = 0;
            }
        }
    }

    void LateUpdate()
    { 
        //now we do horizontal scrolling (if the level is wider than the camera)
        //lateupdate to make sure we're doing this after the player position has been updated
        float camWidth = cam.orthographicSize * cam.aspect;
        float levelEdge = LevelWidth / 2;
        float playerLimit = levelEdge - PlayerWidth; //this is how far horizontally the player can go in either direction
        float playerLimitNormalized = Player.position.x / playerLimit; //the player's x position in the level, where the edges are -1 and 1
        float scrollX = playerLimitNormalized * (levelEdge - camWidth); //the camera's horizontal position is based on the player's horizontal position in the level
        
        if (scrollX > levelEdge - camWidth) scrollX = levelEdge - camWidth;//clamp that camera horizontal position:
        else if (scrollX < -(levelEdge - camWidth)) scrollX = -(levelEdge - camWidth);

        transform.position = GlobalTools.PixelSnap(new Vector3(scrollX, 0, transform.position.z)); //set the position, snapped to pixel

    }
    void SpawnObject(LevelParser.LevelLine line, float offset = 0)
    {
        int CurrentArg = 0;
        string arg = "";
        string val = "";
        GetNextArgument();

        //string prefabPath = $"Prefabs/{val}";
        //ResourceRequest prefabResource = LoadAssets[prefabPath];
        ResourceRequest prefabResource = LoadAssets[val];
        GetNextArgument();
        Vector3 PositionOffset = new Vector3(0, offset, 0);
        Vector3 SpawnPosition = LevelParser.ParseVector3(val)+PositionOffset;
        //TODO: allow for horizontal offset if the path enters from top or bottom of screen
        GameObject spawnedObject = Instantiate(prefabResource.asset as GameObject, SpawnPosition, Quaternion.identity);
        while (GetNextArgument()) //optional arguments
        {
            switch (arg)
            {
                case "Animation":
                    GameObject animParent = new GameObject(spawnedObject.name);
                    animParent.transform.position = spawnedObject.transform.position;
                    spawnedObject.transform.SetParent(animParent.transform);

                    Animation anim = spawnedObject.GetComponent<Animation>();
                    //string animPath = $"Animations/{val}";
                    AnimationClip animClip = LoadAssets[val].asset as AnimationClip;
                    anim.clip = animClip;
                    anim.AddClip(animClip, animClip.name);
                    anim.Play(animClip.name);
                    break;
                case "DeathEvent":
                    spawnedObject.AddComponent<DecrementHoldOnDeath>();
                    break;
                case "HealthDelay":
                    spawnedObject.SendMessage("SetHealthDelay", float.Parse(val)); //TODO: directly call this by referencing the base class for anything that has health
                    break;
                case "SendMessage": //TODO: there's gotta be a better way to call functions without using SendMessage
                    string[] subArguments = val.Split(LevelParser.charComma);
                    switch (subArguments[1])
                    {
                        case "bool":
                            spawnedObject.SendMessage(subArguments[0], bool.Parse(subArguments[2]));
                            break;
                        case "int":
                            spawnedObject.SendMessage(subArguments[0], int.Parse(subArguments[2]));
                            break;
                        case "float":
                            spawnedObject.SendMessage(subArguments[0], float.Parse(subArguments[2]));
                            break;
                        case "string":
                            spawnedObject.SendMessage(subArguments[0], subArguments[2]);
                            break;
                    }
                    break;
                case "Multiple":
                case "Several":
                    break;
            }
        }
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
    public void HoldDecrement()
    {
        LevelHolding -= 1;
    }

    public void PlayerDied()
    {
        PlayerDead = true;
    }
    private void OnApplicationFocus(bool focus)
    {
        if (!focus && Time.timeScale>0) Pause();
    }

    public void Pause()
    {
        Paused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0;

        pauseCanvas.gameObject.SetActive(true);
        playerInput.SwitchCurrentActionMap("Menus");

        foreach(AudioSource musicSource in musicSources)
        {
            musicSource.Pause();
        }
    }

    public void Unpause()
    {
        Paused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1;

        pauseCanvas.gameObject.SetActive(false);
        playerInput.SwitchCurrentActionMap("Gameplay");

        foreach (AudioSource musicSource in musicSources)
        {
            musicSource.Play();
        }
    }
    public void ExitToSystem()
    {
        GlobalTools.ExitToSystem();
    }

    public void OpenSettings()
    {
        settingsCanvas.gameObject.SetActive(true);
        pauseCanvas.gameObject.SetActive(false);
    }

    public void CloseSettings()
    {
        settingsCanvas.gameObject.GetComponent<Settings>().CloseSettings(); 
        pauseCanvas.gameObject.SetActive(true);
    }

    public void OnPause(InputAction.CallbackContext context)
    {
            if (context.started) Pause();
    }

    public void OnMenuCancel(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (pauseCanvas.gameObject.activeInHierarchy) Unpause();
            else if (settingsCanvas.gameObject.activeInHierarchy) CloseSettings();
        }
    }
}