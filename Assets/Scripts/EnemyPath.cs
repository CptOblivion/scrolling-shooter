using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyPath_", menuName = "ScriptableObjects/EnemyPath", order = 1)]
public class EnemyPath : ScriptableObject
{
    //TODO: implement prevent/allow despawn
    //Prevent and allow despawn controls whether the enemy can dip out of the screen or not
    //(prevent despawn if it's expecting to come back  into the screen, allow despawn if the rest of the path is tail that travels off screen)
    public enum NodeCommands {None, Turn, Divebomb, PreventDespawn, AllowDespawn};
    public enum PathCap { Side, Top, Bottom, free}
    [System.Serializable]
    public class EnemyPathNode
    {
        public float Time = 1;
        public Vector3 Position = Vector3.zero;
        public NodeCommands NodeCommand = NodeCommands.None;
    }

    public PathCap PathStart;
    public PathCap PathEnd;
    public float PathScale = 1;
    public float TotalTime;
    public EnemyPathNode[] Path = new EnemyPathNode[0];
}
