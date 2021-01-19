using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LevelParser : MonoBehaviour
{

    public static readonly char whitespace = ' ';
    public static readonly char equals = '=';
    public enum LevelCommands { LevelMode, ScrollSpeed, Spawn, DisplayText, PlaySound, PlayMusic, HoldForDeath, VictoryAnim, LevelEnd}

    public class LevelCommandArugment
    {
        public string Argument;
        public string[] Values; 
    }
    public class LevelLine
    {
        public float Position;
        public LevelCommands Command;
        public LevelCommandArugment[] Arguments;
    }

    class Command
    {
        //first, pull as many arguments as the value in RequiredArgs
        //Next, if there are any entries in OptionalArgs, check each remaining argument against OptionalArgs keys and assign the value according to the type
        //Intended type is listed so we can error check during this phase, instead of while level is running
        //the actual value is stored as a string and parsed during level run, though (unless I can figure out storing unknown types later)
        public enum Types {None, Int, Float, String, Vector2, Vector3}
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

    Dictionary<LevelCommands, Command> Commands = new Dictionary<LevelCommands, Command> {
        
        {LevelCommands.LevelMode,
            new Command(new Command.Types[]{Command.Types.String })},
        
        {LevelCommands.ScrollSpeed,
            new Command(new Command.Types[]{Command.Types.Float }, new Dictionary<string, Command.Types>{
                {"Lerp", Command.Types.Float }})},

        {LevelCommands.Spawn,
            new Command(new Command.Types[]{
                Command.Types.String,
                Command.Types.Vector3 },
                new Dictionary<string, Command.Types>{
                    {"Path", Command.Types.String},
                    {"DeathEvent",Command.Types.None },
                    {"HealthDelay",Command.Types.Float },
                    {"SendMessage",Command.Types.String },
                    {"Group",Command.Types.String } })},
        
        {LevelCommands.DisplayText,
            new Command(new Command.Types[]{
                Command.Types.String }, //TODO: figure out a better way to store this 
                new Dictionary<string, Command.Types>{
                    //TODO: Optional args
                     })},
        
        {LevelCommands.PlaySound,
            new Command(new Command.Types[]{
                Command.Types.String },
                new Dictionary<string, Command.Types>{
                    {"Volume",Command.Types.Float },
                    {"Position", Command.Types.Vector3 },
                    {"Pitch", Command.Types.Float  }, })},
        
        {LevelCommands.PlayMusic,
            new Command(new Command.Types[]{
                Command.Types.String },
                new Dictionary<string, Command.Types>{
                    {"Crossfade", Command.Types.Float},
                    {"Intro",Command.Types.String } })},
        
        //TODO: DefineVariable
        
        {LevelCommands.HoldForDeath,
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
        
        {LevelCommands.VictoryAnim,
            new Command(null)},
        
        {LevelCommands.LevelEnd,
            new Command(null)},
    };

    public static LevelLine ParseLine(string fileLine)
    {

        LevelLine outputLine = new LevelLine();
        string CurrentChunk;
        string RemainingLine = string.Copy(fileLine);

        //line position
        GetNextChunk();
        outputLine.Position = float.Parse(CurrentChunk);
        Debug.Log($"Line position: {outputLine.Position}");

        //line command
        GetNextChunk();
        //TODO: instead of the enum method, instead check if string in Commands keys
        foreach (LevelCommands command in Enum.GetValues(typeof(LevelCommands)))
        {
            if (Enum.GetName(typeof(LevelCommands), command) == CurrentChunk)
            {
                outputLine.Command = command;
                Debug.Log($"line command: {Enum.GetName(typeof(LevelCommands), command)}");
                goto line_command_found;
            }
        }
        Debug.LogError($"Invalid level Command: {fileLine}");
        return null;
        line_command_found:

        outputLine.Arguments = new LevelCommandArugment[RemainingLine.Split(whitespace).Length - 1];
        int argIndex = 0;
        int InvalidArgs = 0;
        string ArgValues;

        //TODO: Parse commands:
        //  get command type from Commands dictionary
        //  run through each argument in RequiredArgs
        //    error check each value
        //  then check each remaining arg against OptionalArgs
        //    error check each value
        //  tag invalid arguments for trimming (see next segment)

        //outputLine.Arguments[argIndex].Argument = CurrentChunk.Substring(0, CurrentChunk.IndexOf(equals));
        //ArgValues = CurrentChunk.Substring(outputLine.Arguments[argIndex].Argument.Length + 1);

        //trim out invalid arguments
        if (InvalidArgs > 0)
        {
            LevelCommandArugment[] NewArgs = new LevelCommandArugment[outputLine.Arguments.Length - InvalidArgs];
            int CurrentIndex = 0;
            foreach(LevelCommandArugment arg in outputLine.Arguments)
            {
                if (arg.Argument != null)
                {
                    NewArgs[CurrentIndex] = arg;
                    CurrentIndex++;
                }
            }
            outputLine.Arguments = new LevelCommandArugment[NewArgs.Length];
            Array.Copy(NewArgs, outputLine.Arguments, NewArgs.Length);
        }

        return outputLine;

        bool GetNextChunk()
        {
            if (RemainingLine.IndexOf(whitespace) > 0)
            {
                CurrentChunk = RemainingLine.Substring(0, RemainingLine.IndexOf(whitespace));
                RemainingLine = RemainingLine.Substring(CurrentChunk.Length + 1);
            }
            else
            {
                CurrentChunk = RemainingLine;
                RemainingLine = "";
            }
            //Debug.Log(">"+CurrentChunk + "<");
            if (CurrentChunk.Length > 0)
            {
                return true;
            }
            Debug.Log("EOL");
            return false;
        }

        void FoundInvalidArgument(int argIndex)
        {
            Debug.LogError("Invalid argument");
            outputLine.Arguments[argIndex].Argument = null;
            InvalidArgs++;
        }
    }
}
