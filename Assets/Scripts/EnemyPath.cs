using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyPath_", menuName = "ScriptableObjects/EnemyPath", order = 1)]
public class EnemyPath : ScriptableObject
{
    public enum NodeCommands {None, Turn, Divebomb};
    [System.Serializable]
    public class EnemyPathNode
    {
        public float Time = 1;
        public Vector3 Position = Vector3.zero;
        public NodeCommands NodeCommand = NodeCommands.None;
    }

    public float PathScale = 1;
    public float TotalTime;
    public EnemyPathNode[] Path = new EnemyPathNode[0];
}
