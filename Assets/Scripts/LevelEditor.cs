using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelEditor : MonoBehaviour
{
    public TextAsset Level;
    float LevelLength;
    int ActiveCommand;
    int[] SelectedCommands;
    readonly float ScreenHeight = 48;
    readonly float LevelWidth = 48 * (16 / 9);
    readonly List<string> LevelLines = new List<string>();
    readonly List<GameObject> Prefabs = new List<GameObject>();
    void Awake()
    {
        if (Level != null)
        {
            PopulateLevel();
        }
        else
        {
            SelectLevel();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void PopulateLevel()
    {
        LevelParser.ParseLine("2.5 ScrollSpeed 5 12 poop");
        //TODO: this
        //read file header
        //populate prefabs, load them
        //parse the rest of the file into separate strings for lines
        //run through each line, place in scene
        //update levelLength
    }

    void SelectLevel()
    {
        //TODO: popup to select existing file or create new
    }

}
