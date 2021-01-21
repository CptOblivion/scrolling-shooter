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
    public enum AvailableCommands { LevelMode, ScrollSpeed, Spawn, DisplayText, PlaySound, PlayMusic, HoldForDeath, VictoryAnim, LevelEnd}
    [System.Serializable]
    public class LevelLine
    {
        public float Position;
        public AvailableCommands Command;
        public LevelCommandArugment[] Arguments;
    }
    [System.Serializable]
    public class Command
    {
        public enum Types {None, Int, Float, String, Vector2, Vector3, Vector4}
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
    public class LevelCommandArugment
    {
        public string Argument;
        public string Value;
        public LevelCommandArugment(string arg, string val)
        {
            Argument = arg;
            Value = val;
        }
    }

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
                    {"Animation", Command.Types.String },
                    {"Path", Command.Types.String },
                    {"DeathEvent",Command.Types.None },
                    {"HealthDelay",Command.Types.Float },
                    {"SendMessage",Command.Types.String },
                    {"Group",Command.Types.String } })},
        
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
                    {"Intro",Command.Types.String },
                    {"Lerp",Command.Types.Float } })},
        
        //TODO: DefineVariable
        
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

    public static List<LevelLine> ParseFile(TextAsset file, out List<GameObject> prefabs)
    {
        //TODO: read file header first
        //TODO: populate prefabs list
        prefabs = null;


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
                currentLine = ParseLine(CurrentLineString, LineCount);
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

    public static LevelLine ParseLine(string fileLine, int LineNumber)
    {
        //Debug.Log($">{fileLine}<");
        LevelLine outputLine = new LevelLine();
        string[] ChunkedString = fileLine.Split(charWhitespace,StringSplitOptions.RemoveEmptyEntries);
        int ChunkCounter = 0;
        string CurrentChunk;

        //line position
        GetNextChunk();
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

        List<LevelCommandArugment> TempArgs = new List<LevelCommandArugment>();
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
                TempArgs.Add(new LevelCommandArugment(OutputString, null));
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
                TempArgs.Add(new LevelCommandArugment(argIndex.ToString(), CurrentChunk));
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
                                TempArgs.Add(new LevelCommandArugment(ArgName, ArgValue));
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
                    TempArgs.Add(new LevelCommandArugment(CurrentChunk, null));
                    //outputLine.Arguments[argIndex] = new LevelCommandArugment(CurrentChunk, null);
                }
                argIndex++;
            }
        }

        //TODO: test for duplicate instances of optional arguments (or just don't bother, it's probably fine)

        outputLine.Arguments = TempArgs.ToArray();
        return outputLine;

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

            /*
            bool TestVector(int size)
            {
                try
                {
                    int TestIndexStart = 0;
                    int TestIndexEnd = ArgVal.IndexOfAny(charComma);
                    for (int i = 0; i < 3; i++)
                    {
                        float.Parse(ArgVal.Substring(TestIndexStart, TestIndexEnd));
                        TestIndexStart = TestIndexEnd + 1;
                        if (i < size-1)
                            TestIndexEnd = ArgVal.Substring(TestIndexStart).IndexOfAny(charComma);
                        else
                            TestIndexEnd = ArgVal.Length - 1;
                    }
                    return true;

                }
                catch
                {
                    ReturnError();
                    return false;
                }
            }
            */
        }
    }

}
