using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Audio;

[RequireComponent(typeof(Camera))]
public class OLD_LevelController : MonoBehaviour
{
    //
    //dumb debug stuff, remove before proper build
    //

    public float simulateLoading = 0;

    //
    //actual variables:
    //
    Transform tf;
    Camera cam;

    [HideInInspector]
    public PlayerInput playerInput;
    [HideInInspector]
    public InputActionMap actionMapGameplay;
    [HideInInspector]
    public InputActionMap actionMapMenus;

    Dictionary<string, ResourceRequest> LoadAssets;

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
    string[] levelLines; //the level file broken into an array of strings
    List<string[]> levelParsed; //the level file parsed out into a list of lines, where each line is an array of strings (representing things like the file line number, the position or time along the level for the command, and the command itself)
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
    public bool LevelScrollModeTime = false;
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

    readonly char[] charSpace = new char[] { ' ', '\t' };
    readonly char[] charEquals = new char[] { '=' };
    readonly char[] charComma = new char[] { ',' };


    private void Awake()
    {
        //initialize basic links
        GlobalTools.canvas_Display = displayCanvas;
        tf = GetComponent<Transform>();
        cam = GetComponent<Camera>();

        //initialize input stuff
        playerInput = GetComponent<PlayerInput>();
        actionMapGameplay = playerInput.actions.FindActionMap("Gameplay");
        actionMapMenus = playerInput.actions.FindActionMap("Menus");

        //initialize audio stuff
        GlobalTools.audioMixer = audioMixer;
        AudioSource sourceA = this.gameObject.AddComponent<AudioSource>();
        AudioSource sourceB = this.gameObject.AddComponent<AudioSource>();
        musicSources = new AudioSource[] { sourceA, sourceB };
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

        displayCanvas.gameObject.SetActive(true);
        loadingCanvas.gameObject.SetActive(true);

        hudCanvas.gameObject.SetActive(false);
        pauseCanvas.gameObject.SetActive(false);
        settingsCanvas.gameObject.SetActive(false);
        Player.gameObject.SetActive(false);

        PlayerWidth = playerCollider.bounds.extents.x;

        ScrollSpeedLerpSpeed = 0;

        if (GlobalTools.level) Level = GlobalTools.level;
        LoadAssets = new Dictionary<string, ResourceRequest>();
        if (Level != null) levelLines = Level.text.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.None);
        levelParsed = new List<string[]>();
        for (int i = 0; i < levelLines.Length; i++)
        {
            //add the line to the parsed list, if it's not a comment or blank
            if (levelLines[i] != "" && (levelLines[i].Substring(0, 2) != "//"))
            {
                string line = (i + 1).ToString() + " " + levelLines[i]; //add the file line to the front of the line (since the resulting array might be shorter than the file due to comments and blank lines, we can't just track it with an int)


                if (line.Contains("//")) //trim comments
                {
                    int commentIndex = line.IndexOf("//");
                    line = line.Substring(0, commentIndex);
                }
                line = line.TrimEnd(charSpace);
                string[] lineParsed = line.Split(charSpace, System.StringSplitOptions.RemoveEmptyEntries);
                levelParsed.Add(lineParsed);

                //check for objects to load
                if (lineParsed[2] == "Spawn")
                {
                    string prefabPath = "Prefabs/" + lineParsed[3];
                    if (!LoadAssets.ContainsKey(prefabPath)) LoadAssets.Add(prefabPath, Resources.LoadAsync(prefabPath));

                    foreach (string argument in lineParsed)
                    {
                        if (argument.StartsWith("Animation="))
                        {
                            string animPath = "Animations/" + argument.Substring(10);
                            if (!LoadAssets.ContainsKey(animPath)) LoadAssets.Add(animPath, Resources.LoadAsync(animPath));
                        }
                    }
                }
                else if (lineParsed[2] == "PlaySound")
                {
                    string prefabPath = "Sounds/" + lineParsed[3];
                    if (!LoadAssets.ContainsKey(prefabPath)) LoadAssets.Add(prefabPath, Resources.LoadAsync(prefabPath));
                }


                //resulting format is {"LevelFileLine", "Position", "command", "arguments..."}
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

                string debugString = "Loaded\n";
                foreach (string name in LoadAssets.Keys)
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

        if (!loading)
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
                while (true) //risky!
                {
                    float NextCommandDistance;
                    float LevelProgress;
                    if (levelParsed[0][1].Substring(0, 1) == "+") //if there's a "+" before the position, it's a relative offset from the last command
                    {
                        NextCommandDistance = float.Parse(levelParsed[0][1].Substring(1)); //in local offset mode, we have to trim the "+" before the number
                        if (LevelScrollModeTime) LevelProgress = LevelTimeRelative;
                        else LevelProgress = LevelPositionRelative;
                    }
                    else //absolute offset mode
                    {
                        NextCommandDistance = float.Parse(levelParsed[0][1]); //in absolute offset mode, we can just use the absolute number
                        if (LevelScrollModeTime) LevelProgress = LevelTime;
                        else LevelProgress = LevelPosition;
                    }

                    if (LevelProgress < NextCommandDistance) break; //we haven't reached the next command yet so let's just leave the loop until the next frame, alright?

                    //if we passed that break we've hit a command, so reset the relative position counters
                    LevelPositionRelative = 0;
                    LevelTimeRelative = 0;
                    string[] line = levelParsed[0]; //the current line we're working on

                    //I'm using an integer and counting it up because I don't doubt that I'll want to insert another dumb entry in the level file format for every line and this'll make the inevitable refactor a lot easier
                    int LineEntryIndex = 0; //track the current entry of line that we're working on
                    int LevelFileLine = int.Parse(line[LineEntryIndex]); LineEntryIndex++; //the line number of the file
                    LineEntryIndex++;//this entry is the position/time code, but we don't need that now that we're past the loop test so there's no reason to assign a variable
                    string command = line[LineEntryIndex]; LineEntryIndex++; //the command we'll be executing
                    string[] arguments = new string[line.Length - LineEntryIndex]; //any subsequent entries are arguments
                    for (; LineEntryIndex < line.Length; LineEntryIndex++)
                    {
                        arguments[LineEntryIndex - 3] = line[LineEntryIndex];
                    }


                    if (command == "LevelMode")
                    {
                        if (arguments.Length == 0) Debug.Log("no mode selected, line " + LevelFileLine);
                        else if (arguments[0] == "Distance") LevelScrollModeTime = false;
                        else if (arguments[0] == "Time") LevelScrollModeTime = true;
                        else Debug.Log("Invalid scrolling mode: " + arguments[0] + ", line " + LevelFileLine);
                    }


                    else if (command == "ScrollSpeed")
                    {
                        for (int i = 1; i < arguments.Length; i++)
                        {
                            string[] argument = arguments[i].Split(charEquals);
                            if (argument[0] == "Lerp")
                            {
                                ScrollSpeedLerpStart = ScrollSpeed;
                                ScrollSpeedLerpSpeed = 1 / float.Parse(argument[1]);
                                ScrollSpeedLerpTarget = float.Parse(arguments[0]);
                                ScrollSpeedLerpProgress = 0;
                            }
                            else Debug.Log("Invalid argument: " + argument[0] + ", line " + LevelFileLine);
                        }
                        ScrollSpeed = float.Parse(arguments[0]);
                    }


                    else if (command == "Spawn") //Spawn prefabPath x,y,z
                    {
                        string prefabPath = "Prefabs/" + arguments[0];
                        ResourceRequest prefabResource = LoadAssets[prefabPath];

                        string[] positionStrings = arguments[1].Split(charComma);
                        Vector3 spawnPosition = Vector3.zero;
                        for (int i = 0; i < 3; i++) spawnPosition[i] = float.Parse(positionStrings[i]);
                        GameObject spawnedObject = Instantiate(prefabResource.asset as GameObject, spawnPosition, Quaternion.identity);

                        for (int i = 2; i < arguments.Length; i++) //optional arguments
                        {
                            string[] argument = arguments[i].Split(charEquals);
                            if (argument[0] == "Animation") //Animation=animationClipPath
                            {
                                GameObject animParent = new GameObject(spawnedObject.name);
                                animParent.transform.position = spawnedObject.transform.position;
                                spawnedObject.transform.SetParent(animParent.transform);

                                Animation anim = spawnedObject.GetComponent<Animation>();
                                string animPath = "Animations/" + argument[1];
                                AnimationClip animClip = LoadAssets[animPath].asset as AnimationClip;
                                anim.clip = animClip;
                                anim.AddClip(animClip, animClip.name);
                                anim.Play(animClip.name);

                            }
                            else if (argument[0] == "DeathEvent") //DeathEvent
                            {
                                spawnedObject.AddComponent<DecrementHoldOnDeath>();
                            }
                            else if (argument[0] == "HealthDelay") //HealthDelay=delay
                            {
                                spawnedObject.SendMessage("SetHealthDelay", float.Parse(argument[1]));
                            }
                            else if (argument[0] == "SendMessage") //SendMessage=message,valueType,value
                            {
                                string[] subArguments = argument[1].Split(charComma);
                                if (subArguments[1] == "bool") spawnedObject.SendMessage(subArguments[0], bool.Parse(subArguments[2]));
                                else if (subArguments[1] == "int") spawnedObject.SendMessage(subArguments[0], int.Parse(subArguments[2]));
                                else if (subArguments[1] == "float") spawnedObject.SendMessage(subArguments[0], float.Parse(subArguments[2]));
                                else if (subArguments[1] == "string") spawnedObject.SendMessage(subArguments[0], subArguments[2]);
                                else
                                {
                                    Debug.Log("invalid type: " + subArguments[1] + " line " + LevelFileLine);
                                }

                            }
                            else Debug.Log("Invalid argument: " + argument[0] + ", line " + LevelFileLine);
                        }
                    }


                    else if (command == "DisplayText")
                    {
                        string displayText = "";

                        //this whole mess is to allow spaces in the DisplayText string
                        //each word is stored as a separate argument so we have to rebuild the string and then treat everything after as subsequent arguments
                        //I'm tempted to get rid of all this and just require a \s for spaces or something, but I've already gone and written the code so eh
                        int index = 0;
                        bool wordsFinished = false;
                        for (; index < arguments.Length; index++)
                        {
                            string currentWord = arguments[index];
                            if (index == 0) currentWord = currentWord.Remove(0, 1);//lose that opening quotation mark

                            if (currentWord.Substring(currentWord.Length - 1) == "\"")// once we run into a close quote, we're done
                            {
                                currentWord = currentWord.Remove(currentWord.Length - 1); //trim the final quotation mark
                                wordsFinished = true;
                            }

                            displayText += currentWord;

                            if (wordsFinished)
                            {
                                index++; //we're gonna continue using index after this but it's not automatically incremented by the loop if we break
                                break; //whoops we did it we broke oh jeez oh gosh
                            }
                            displayText += " "; //add a space after each word (except the last one)
                        }

                        GameObject textObject = Instantiate(Resources.Load("GenericText", typeof(GameObject)) as GameObject);
                        textObject.transform.SetParent(hudCanvas.transform, false);
                        Text textComponent = textObject.GetComponent<Text>();
                        textComponent.text = displayText;
                        float TextLife = float.Parse(arguments[index]); index++;
                        textObject.GetComponent<EffectKill>().life = TextLife;

                        for (int i1 = index; i1 < arguments.Length; i1++) //and now the optional arguments 
                        {
                            string[] argument = arguments[i1].Split(charEquals);

                            if (argument[0] == "Position") //floats x,y,z
                            {
                                string[] PositionStrings = argument[1].Split(charComma);
                                Vector3 TextPosition = Vector3.zero;
                                for (int i = 0; i < 3; i++) TextPosition[i] = float.Parse(PositionStrings[i]);
                                textObject.transform.localPosition = TextPosition;
                            }
                            else if (argument[0] == "Size") // float size
                            {
                                float TextSize = float.Parse(argument[1]);
                                textComponent.fontSize = (int)TextSize;

                            }
                            else if (argument[0] == "Color") //floats r,g,b,a
                            {
                                string[] colorStrings = argument[1].Split(charComma);
                                Color textColor = Color.black;
                                for (int i = 0; i < 4; i++) textColor[i] = float.Parse(colorStrings[i]);
                                textComponent.color = textColor;
                            }
                            else Debug.Log("Invalid argument: " + argument[0] + ", line " + LevelFileLine);
                        }

                    }

                    else if (command == "PlaySound")
                    {
                        int i = 0;
                        float pitch = 1;
                        float volume = 1;
                        float spatialBlend = 0;
                        Vector3 Position = Vector3.zero;
                        AudioClip sound = LoadAssets["Sounds/" + arguments[0]].asset as AudioClip; i++;

                        for (; i < arguments.Length; i++)
                        {
                            string[] argument = arguments[i].Split(charEquals);

                            if (argument[0] == "Volume")
                            {
                                volume = float.Parse(argument[1]);
                            }
                            else if (argument[0] == "Position")
                            {
                                string[] PositionStrings = argument[1].Split(charComma);
                                for (int i2 = 0; i2 < 3; i2++) Position[i] = float.Parse(PositionStrings[i2]);
                                spatialBlend = 1;
                            }
                            else if (argument[0] == "Pitch")
                            {
                                pitch = float.Parse(argument[1]);
                            }
                            else Debug.Log("Invalid argument: " + argument[0] + ", line " + LevelFileLine);
                        }
                        AudioSource audioSource = GlobalTools.PlaySound(sound, pitch);
                        audioSource.volume = volume;
                        audioSource.spatialBlend = spatialBlend;
                        audioSource.gameObject.transform.position = Position;
                    }

                    else if (command == "PlayMusic")
                    {
                        int i = 0;
                        AudioClip music = Resources.Load("Sounds/Music/" + arguments[0]) as AudioClip; i++;
                        currentMusicSource = !currentMusicSource;

                        musicSources[currentMusicSource ? 0 : 1].clip = music;
                        musicSources[currentMusicSource ? 0 : 1].Play();
                        musicSources[currentMusicSource ? 0 : 1].volume = 1;
                        musicSources[currentMusicSource ? 1 : 0].volume = 0;

                        for (; i < arguments.Length; i++)
                        {
                            string[] argument = arguments[i].Split(charEquals);

                            if (argument[0] == "Crossfade" || argument[0] == "Lerp")//why not be flexible, eh?
                            {
                                musicLerpProgress = 1;
                                musicLerpSpeed = 1 / float.Parse(argument[1]);
                            }
                            else if (argument[0] == "Intro")
                            {
                                AudioClip introMusic = Resources.Load("Sounds/Music/" + argument[1]) as AudioClip;
                                musicSources[currentMusicSource ? 1 : 0].clip = introMusic;
                                musicSources[currentMusicSource ? 1 : 0].Play();
                                musicSources[currentMusicSource ? 1 : 0].volume = 1;
                                musicSources[currentMusicSource ? 1 : 0].loop = false;
                                musicSources[currentMusicSource ? 0 : 1].Stop();
                                musicSources[currentMusicSource ? 0 : 1].PlayScheduled(AudioSettings.dspTime + introMusic.length);
                            }
                        }
                    }

                    else if (command == "HoldForDeath") //HoldForEvent
                    {
                        bool additive = false;
                        int count = 1;
                        for (int i = 0; i < arguments.Length; i++) //optional arguments
                        {
                            string[] argument = arguments[i].Split(charEquals);
                            if (argument[0] == "Count") //Count=count
                            {
                                count = int.Parse(argument[1]);
                            }
                            else if (argument[0] == "Additive") //Additive
                            {
                                additive = true;
                            }
                        }
                        if (additive) LevelHolding += count;
                        else LevelHolding = count;
                        if (LevelHolding > 0) //if we're actually holding
                        {
                            levelParsed.RemoveAt(0);
                            break;
                        }
                    }


                    else if (command == "VictoryAnim")
                    {
                        playerControl.PlayVictoryAnimation();
                    }


                    else if (command == "LevelEnd")
                    {
                        GlobalTools.EndLevel(false);
                        break;//this is probably not necessary but it makes me feel safer
                    }
                    else Debug.Log("invalid command: " + command + ", line " + LevelFileLine);

                    levelParsed.RemoveAt(0);//pop the line we just finished

                    //cleanup just in case a level is left open at the end
                    //this means the player will never see whatever the last command was, of course, since the level ends on the next frame
                    if (levelParsed.Count == 0)
                    {
                        Debug.Log("no LevelEnd in level file! Ending...");
                        levelParsed.Add(new string[] { "+0", "LevelEnd" });
                        break;
                    }
                }

                //update all the position and time variables
                LevelPosition += ScrollSpeed * Time.deltaTime;
                LevelPositionRelative += ScrollSpeed * Time.deltaTime;
                LevelTime += Time.deltaTime;
                LevelTimeRelative += Time.deltaTime;

                if (musicLerpProgress > 0)
                {
                    musicSources[currentMusicSource ? 1 : 0].volume = musicLerpProgress; //old music, ramping down
                    musicSources[currentMusicSource ? 0 : 1].volume = 1 - musicLerpProgress; //new music, ramping up
                    musicLerpProgress -= musicLerpSpeed * Time.deltaTime;
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

        tf.position = GlobalTools.PixelSnap(new Vector3(scrollX, 0, tf.position.z)); //set the position, snapped to pixel

    }
    public void HoldDecrement() //this function is to allow us to resume if the level file is waiting for enemies to die or whatnot
    {
        LevelHolding -= 1;
    }

    public void PlayerDied()
    {
        PlayerDead = true;
    }
    private void OnApplicationFocus(bool focus)
    {
        if (!focus && Time.timeScale > 0) Pause();
    }

    public void Pause()
    {
        Paused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0;

        pauseCanvas.gameObject.SetActive(true);
        playerInput.SwitchCurrentActionMap("Menus");

        foreach (AudioSource musicSource in musicSources)
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