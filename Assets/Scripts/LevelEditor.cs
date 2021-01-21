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
    readonly float LevelWidth = 48 * (16 / 9); //TODO: check if this is actually 80 (currently 85.something)
    public List<LevelParser.LevelLine> LevelLines = new List<LevelParser.LevelLine>();
    public List<GameObject> Prefabs = new List<GameObject>();
    void Awake()
    {
        if (Level == null)
        {
            SelectLevel();
        }
        else
        {
            PopulateLevel();
        }
    }

    void PopulateLevel()
    {
        LevelLines = LevelParser.ParseFile(Level, out Prefabs);
        //load prefabs
        //run through each line, place in scene
        //update levelLength as we go
    }

    void SelectLevel()
    {
        //TODO: popup to select existing file or create new
    }


}
