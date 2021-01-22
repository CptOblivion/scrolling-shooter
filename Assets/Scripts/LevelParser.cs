using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LevelParser : MonoBehaviour
{

    public static readonly char[] charWhitespace = new char[] { ' ', '\t' };
    public static readonly char[] charEquals =  new char[] { '=' };
    public static readonly char[] charComma = new char[] { ',' };
    public static readonly string strCommentStarter = "//";
    public static readonly string[] charNewline = new string[] {"\n", "\r\n" };
    public static readonly string strQuotes = "\"";
    public static readonly string strAdditive = "+";
    public enum AvailableCommands { LevelMode, ScrollSpeed, Spawn, DisplayText, PlaySound, PlayMusic, HoldForDeath, VictoryAnim, LevelEnd}
    [System.Serializable]
    public class LevelLine
    {
        public bool RelativePosition;
        public float Position;
        public AvailableCommands Command;
        public LevelCommandArgument[] Arguments;
    }
    [System.Serializable]
    public class Command
    {
        //TODO: add bool
        //TODO: can we store variables as string, but just use the bits directly and treat them as whatever type we want? Is that worth the trouble?
        public enum Types {None, Bool, Int, Float, String, Vector2, Vector3, Vector4, ERROR} //don't assign the ERROR case, that's for error handling internally
        public Types[] RequiredArgs;
        public Dictionary<string, Types> OptionalArgs;
        public Command(Types[] required, Dictionary<string, Types> optional)
        {
            RequiredArgs = required;
            OptionalArgs = optional;
        }
        public Command (Types[] required)
        {
            RequiredArgs = required;
        }
    }
    [System.Serializable]
    public class LevelCommandArgument
    {
        public string Argument;
        public string Value;
        public LevelCommandArgument(string arg, string val)
        {
            Argument = arg;
            Value = val;
        }
    }

    //TODO: maybe add some set of valid parameters, for error checking (or just handle error checking at the end of the ParseLine function)
    static readonly Dictionary<AvailableCommands, Command> Commands = new Dictionary<AvailableCommands, Command> {
        
        {AvailableCommands.LevelMode,
            new Command(new Command.Types[]{Command.Types.String })},
        
        {AvailableCommands.ScrollSpeed,
            new Command(new Command.Types[]{Command.Types.Float }, new Dictionary<string, Command.Types>{
                {"Lerp", Command.Types.Float }})},

        {AvailableCommands.Spawn,
            new Command(new Command.Types[]{
                Command.Types.String,
                Command.Types.Vector3 },
                new Dictionary<string, Command.Types>{
                    {"Animation", Command.Types.String }, //animation file to play (generally don't use with Path)
                    {"Path", Command.Types.String }, //path to follow (generally don't use with Animation)
                    {"DeathEvent",Command.Types.None }, //tag this unit to count towards level hold counter on death //TO BE DEPRECATED ONCE GROUPS ARE IMPLEMENTED
                    {"HealthDelay",Command.Types.Float }, //time until unit can receive damage
                    {"Group",Command.Types.String }, //NOT IMPLEMENTED YET: add unit to a named group, for use with events and triggers (EG end level hold when group is empty)
                    {"SendMessage",Command.Types.String }, //Bad, find another way to do this.
                    {"Multiple", Command.Types.Vector3 },
                    {"Repeat", Command.Types.Vector3 },
                    {"Several", Command.Types.Vector3 } })}, //spawn several identical copies of this unit in a row- Vector2 entries are number of units, and delay between spawns
        
        {AvailableCommands.DisplayText,
            new Command(new Command.Types[]{
                Command.Types.Float },
                new Dictionary<string, Command.Types>{
                    {"Position",Command.Types.Vector3 },
                    {"Color", Command.Types.Vector4 },
                    {"Size", Command.Types.Float }, })},
        
        {AvailableCommands.PlaySound,
            new Command(new Command.Types[]{
                Command.Types.String },
                new Dictionary<string, Command.Types>{
                    {"Volume",Command.Types.Float },
                    {"Position", Command.Types.Vector3 },
                    {"Pitch", Command.Types.Float  }, })},
        
        {AvailableCommands.PlayMusic,
            new Command(new Command.Types[]{
                Command.Types.String },
                new Dictionary<string, Command.Types>{
                    {"Crossfade", Command.Types.Float},
                    {"Lerp",Command.Types.Float },
                    {"Intro",Command.Types.String } })},
        
        //TODO: DefineVariable
        //TODO: figure out if there's actually a use for DefineVariable
        
        {AvailableCommands.HoldForDeath,
            new Command(
                null,
                new Dictionary<string, Command.Types>{
                    {"Count", Command.Types.Int },
                    {"Additive",Command.Types.None } })},
        //TODO: HoldForGroup
        
        //TODO: KillGroup
        
        //TODO: LoopEventStart
        
        //TODO: LoopEventEnd
        
        //TODO: EffectEvent (EG camera shake, color shift- rename this command)
        
        {AvailableCommands.VictoryAnim,
            new Command(null)},
        
        {AvailableCommands.LevelEnd,
            new Command(null)},
    };

    public static List<LevelLine> ParseFile(TextAsset file, out List<string> LoadAssets)
    {
        //TODO: read file header first
        //TODO: populate prefabs list
        LoadAssets = new List<string>();


        List<LevelLine> output = new List<LevelLine>();
        LevelLine currentLine;
        int CurrentLineStart = 0;
        string CurrentLineString;

        int LineCount = 1;
        int NewlineLength;
        int LineEnd;
        while (CurrentLineStart > -1 && CurrentLineStart < file.text.Length)
        {
            LineEnd = IndexOfAny(file.text, charNewline, CurrentLineStart, out NewlineLength);
            if (LineEnd > -1)
            {
                CurrentLineString = file.text.Substring(CurrentLineStart, LineEnd - CurrentLineStart);
                CurrentLineStart += CurrentLineString.Length + NewlineLength;
            }
            else
            {
                //reached end of file
                CurrentLineString = file.text.Substring(CurrentLineStart);
                CurrentLineStart = -1;
            }
            //Debug.Log($"Newline Length: {NewlineLength}");
            CurrentLineString.TrimStart(charWhitespace);
            if (!CurrentLineString.StartsWith(strCommentStarter) && CurrentLineString.Length > 0)
            {
                currentLine = ParseLine(CurrentLineString, LineCount, LoadAssets);
                if (currentLine != null)
                {
                    output.Add(currentLine);
                }
            }
            LineCount++;
        }
        Debug.Log($"File {file.name} loaded. File Lines: {LineCount - 1}, Commands: {output.Count}");
        return output;
    }

    public static bool WriteLevel(TextAsset text, List<GameObject> prefabs, List<LevelLine> commands)
    {
        //TODO: this
        return false;
    }

    public static int IndexOfAny(string input, string[] strings, int StartIndex, out int MatchLength)
    {
        int swap;
        int Output = -1;

        MatchLength = 0;

        foreach (string str in strings)
        {
            swap = input.IndexOf(str, StartIndex);
            if (swap > -1 && (Output == -1 || swap < Output))
            {
                Output = swap;
                MatchLength = str.Length;
            }
        }
        return Output;
    }

    public static LevelLine ParseLine(string fileLine, int LineNumber, List<string> LoadAssets)
    {
        //Debug.Log($">{fileLine}<");
        LevelLine outputLine = new LevelLine();
        string[] ChunkedString = fileLine.Split(charWhitespace,StringSplitOptions.RemoveEmptyEntries);
        int ChunkCounter = 0;
        string CurrentChunk;

        //line position
        GetNextChunk();

        outputLine.RelativePosition = CurrentChunk.StartsWith(strAdditive);
        outputLine.Position = float.Parse(CurrentChunk);
        //Debug.Log($"Line position: {outputLine.Position}");

        //line command
        GetNextChunk();
        foreach (AvailableCommands command in Enum.GetValues(typeof(AvailableCommands)))
        {
            if (Enum.GetName(typeof(AvailableCommands), command) == CurrentChunk)
            {
                outputLine.Command = command;
                //Debug.Log($"line command: {Enum.GetName(typeof(LevelCommands), command)}");
                goto line_command_found;
            }
        }
        Debug.LogError($"Invalid level Command: {fileLine} (Line {LineNumber})");
        return null;
        line_command_found:

        List<LevelCommandArgument> TempArgs = new List<LevelCommandArgument>();
        //outputLine.Arguments = new LevelCommandArugment[RemainingLine.Split(charWhitespace).Length - 1];
        int argIndex = 0;
        string ArgName;
        string ArgValue;
        int ArgSeparatorIndex;
        Command currentCommand;

        currentCommand = Commands[outputLine.Command];

        if (outputLine.Command == AvailableCommands.DisplayText)
        {
            string OutputString = "";
            //because display text supports spaces in strings, we have to handle it a bit differently at first
            GetNextChunk();
            if (!CurrentChunk.StartsWith(strQuotes))
            {
                Debug.LogError($"Expected string beginning with quotes, instead got >{CurrentChunk}< (Line {LineNumber})");
                return null;
            }
            else
            {
                CurrentChunk = CurrentChunk.Substring(1); //trim leading quotes
                while (true)
                {
                    //TODO: allow for escape characters
                    //TODO: maybe this should happen before extraneous whitespace is cut out of the command
                    //TODO: no error for reached end of line with no close quote
                    if (CurrentChunk.EndsWith(strQuotes))
                    {
                        OutputString += CurrentChunk.Substring(0, CurrentChunk.Length - 1);//trim trailing quotes
                        break;
                    }
                    OutputString += CurrentChunk + " ";
                    GetNextChunk();
                }
                TempArgs.Add(new LevelCommandArgument(OutputString, null));
            }
        }
        if (currentCommand.RequiredArgs != null)
        {
            foreach(Command.Types arg in currentCommand.RequiredArgs)
            {
                GetNextChunk();
                if (!TestParseArgument(CurrentChunk, currentCommand.RequiredArgs[argIndex]))
                {
                    return null;
                }
                TempArgs.Add(new LevelCommandArgument(argIndex.ToString(), CurrentChunk));
                argIndex++;
            }
        }
        if (currentCommand.OptionalArgs != null)
        {
            while (GetNextChunk())
            {
                if (CurrentChunk.StartsWith(strCommentStarter))
                {
                    break;
                }
                ArgSeparatorIndex = CurrentChunk.IndexOfAny(charEquals);
                if (ArgSeparatorIndex >= 0)
                { 
                    ArgName = CurrentChunk.Substring(0, ArgSeparatorIndex);
                    if (ArgSeparatorIndex == CurrentChunk.Length-1)
                    {
                        //equals sign is at end of chunk (no following value)
                        Debug.LogError($"Expected argument value in argument \"{CurrentChunk}\" (Line {LineNumber})");
                    }
                    else
                    {
                        if (!currentCommand.OptionalArgs.ContainsKey(ArgName))
                        {
                            //argument name doesn't match list of optional arguments for current command
                            Debug.LogError($"Invalid argument \"{CurrentChunk}\" (Line {LineNumber})");
                        }
                        else
                        {
                            ArgValue = CurrentChunk.Substring(ArgSeparatorIndex + 1);
                            if (!TestParseArgument(ArgValue, currentCommand.OptionalArgs[ArgName]))
                            {
                                //ArgValue string can't be parsed into expected value type (error is logged from TestParse function)
                            }
                            else
                            {
                                //no error found
                                TempArgs.Add(new LevelCommandArgument(ArgName, ArgValue));
                            }
                        }
                    }
                }
                else if (!currentCommand.OptionalArgs.ContainsKey(CurrentChunk))
                {
                    //no equals sign in command, and chunk isn't found to be an argument on its own
                    Debug.LogError($"Invalid argument \"{CurrentChunk}\" (Line {LineNumber})");
                }
                else if (currentCommand.OptionalArgs[CurrentChunk] != Command.Types.None)
                {
                    //chunk is an argument, but the argument type was expecting a value and no equals sign was present
                    Debug.LogError($"Argument expected values: \"{CurrentChunk}\" (Line {LineNumber})");
                }
                else
                {
                    //no error found, current chunk is a standalone argument (type None)
                    TempArgs.Add(new LevelCommandArgument(CurrentChunk, null));
                    //outputLine.Arguments[argIndex] = new LevelCommandArugment(CurrentChunk, null);
                }
                argIndex++;
            }
        }

        //TODO: test for duplicate instances of optional arguments (or just don't bother, it's probably fine)

        //error checking: (maybe I'm only checking for errors in LevelMode, I could probably lose the switch statement)
        switch (outputLine.Command)
        {
            case AvailableCommands.LevelMode:
                string mode = TempArgs[0].Value;
                if ( mode != "Distance" && mode != "Time")
                {
                    Debug.LogError($"Invalid scrolling mode: {mode} (line{LineNumber})");
                    return null;
                }
                break;
            case AvailableCommands.Spawn:
                foreach(LevelCommandArgument arg in TempArgs)
                {
                    if (arg.Argument == "SendMessage")
                    {
                        string[] components = arg.Value.Split(charComma);
                        Command.Types argType = components[1] switch
                        {
                            "null" => Command.Types.None,
                            "bool" => Command.Types.Bool,
                            "int" => Command.Types.Int,
                            "float" => Command.Types.Float,
                            "string" => Command.Types.String,
                            _ => Command.Types.ERROR
                        };
                        if (argType == Command.Types.ERROR)
                        {
                            Debug.LogError($"Invalid type for SendMessage: {components[1]} (Line {LineNumber})");
                        }
                        else if (argType != Command.Types.None)
                        {
                            TestParseArgument(components[2], argType);
                        }
                    }
                }
                break;
        }

        outputLine.Arguments = TempArgs.ToArray();

        //check if we need to add assets to load
        switch (outputLine.Command)
        {
            case AvailableCommands.Spawn:
                TryAddLoadAsset("Prefabs/", 0);
                for (int i = 0; i < outputLine.Arguments.Length; i++)
                {
                    if (outputLine.Arguments[i].Argument == "Animation")
                    {
                        TryAddLoadAsset("Animations/", i);
                        break;
                    }
                    if (outputLine.Arguments[i].Argument == "Path")
                    {
                        TryAddLoadAsset("Paths/", i);
                        break;
                    }
                }
                break;
            case AvailableCommands.PlaySound:
                TryAddLoadAsset("Sounds/", 0);
                break;
        }

        return outputLine;

        //TODO: hopefully, by passing LoadAssets as an argument in the function, we're using a pointer and not a copy- so adding to it should update the version that's returned up top. (confirm this)
        void TryAddLoadAsset(string prefix, int arg)
        {
            string PrefabString = prefix + outputLine.Arguments[arg].Value;
            if (!LoadAssets.Contains(PrefabString))
            {
                LoadAssets.Add(PrefabString);
            }
        }

        ///Gets next piece of text in the line, separated by a space. Returns false if the next chunk is empty or begins a comment (line end reached)
        bool GetNextChunk()
        {
            if (ChunkCounter >= ChunkedString.Length)
            {
                CurrentChunk = "";
                return false;
            }
            CurrentChunk = ChunkedString[ChunkCounter];
            ChunkCounter++;
            if (!CurrentChunk.StartsWith(strCommentStarter))
            {
                return true;
            }
            return false;
        }

        bool TestParseArgument(string ArgVal, Command.Types type)
        {
            void ReturnError()
            {
                Debug.LogError($"Command expected {type.ToString()}, instead got \"{ArgVal}\" (Line {LineNumber})");
            }
            switch (type)
            {
                case Command.Types.Float:
                    try
                    {
                        float.Parse(ArgVal);
                    }
                    catch
                    {
                        ReturnError();
                        return false;
                    }
                    break;
                case Command.Types.Int:
                    try
                    {
                        int.Parse(ArgVal);
                    }
                    catch
                    {
                        ReturnError();
                        return false;
                    }
                    break;
                /* no need to test if a string is a string, leaving this in to shame myself
                case Command.Types.String:
                  break; */
                case Command.Types.Vector2:
                    TestVector(2);
                    break;
                case Command.Types.Vector3:
                    TestVector(3);
                    break;
                case Command.Types.Vector4:
                    TestVector(4);
                    break;
            }
            return true;

            bool TestVector(int size)
            {
                string[] components = ArgVal.Split(',');
                if (components.Length != size)
                {
                    ReturnError();
                    return false;
                }
                foreach(string str in components)
                {
                    try
                    {
                        float.Parse(str);
                    }
                    catch
                    {
                        ReturnError();
                        return false;
                    }
                }
                return true;
            }
        }
    }
    public static Vector2 ParseVector2(string str)
    {
        string[] components = str.Split(',');
        return new Vector2(float.Parse(components[0]), float.Parse(components[1]));
    }
    public static Vector3 ParseVector3(string str)
    {
        string[] components = str.Split(',');
        return new Vector3(float.Parse(components[0]), float.Parse(components[1]), float.Parse(components[2]));
    }
    public static Vector4 ParseVector4(string str)
    {
        string[] components = str.Split(',');
        return new Vector4(float.Parse(components[0]), float.Parse(components[1]), float.Parse(components[2]), float.Parse(components[3]));
    }

}
