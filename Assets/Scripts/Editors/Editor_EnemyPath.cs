using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(EnemyPath))]
public class Editor_EnemyPaths : Editor
{
    double LastEditorTime;
    int SelectedNode;
    float PreviewPath;
    float DeltaTime;
    SerializedProperty TotalTime;
    SerializedProperty Path;
    SerializedProperty PathScale;
    private void OnEnable()
    {
        SelectedNode = -1;
        PreviewPath = -1;
        Path = serializedObject.FindProperty("Path");
        PathScale = serializedObject.FindProperty("PathScale");
        TotalTime = serializedObject.FindProperty("TotalTime");
        SceneView.duringSceneGui += OnSceneGUI;
        LastEditorTime = EditorApplication.timeSinceStartup;
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(Path);
        EditorGUILayout.Slider(PathScale, 0.01f, 10);
        TotalTime.floatValue = 0;
        for(int i  = 1; i < Path.arraySize; i++)
        {
            TotalTime.floatValue+= Path.GetArrayElementAtIndex(i).FindPropertyRelative("Time").floatValue;
        }


        GUIStyle style = GUIStyle.none;
        style.normal.textColor = Color.white;
        EditorGUILayout.LabelField($"Path time: {TotalTime.floatValue * PathScale.floatValue}", style);

        EditorGUILayout.Space();
        //TODO: change button size to small
        //play preview animation
        if (GUILayout.Button("Preview Path", GUILayout.ExpandWidth(false)))
        {
            if (PreviewPath >= 0)
            {
                PreviewPath = -1;
            }
            else
            {
                PreviewPath = 0;
            }
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        UpdateDeltaTime();
        //TODO: adjust whole-path length in onscenegui?
        //TODO: delete node
        //TODO: draw path direction
        //TODO: clamp ends of path horizontally to -50 and 50
        //TODO: adjust time between nodes (don't forget to update TotalTime too)
        //  Scroll wheel while moused over line segment?
        //TODO: play/pause button for preview
        //TODO: if we click and drag on a button, can we immediately initiate a drag on the newly spawned widget?

        EnemyPath enemyPath = (EnemyPath)target;

        Vector3 newPosition;
        if (SelectedNode >= 0)
        {
            if (SelectedNode >= enemyPath.Path.Length)
            {
                SelectedNode = -1;
            }
            EditorGUI.BeginChangeCheck();
            newPosition = Handles.PositionHandle(enemyPath.Path[SelectedNode].Position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(enemyPath, "Move Path Node");
                enemyPath.Path[SelectedNode].Position = newPosition;
            }
        }

        if (enemyPath.Path.Length < 2)
        {
            enemyPath.Path = new EnemyPath.EnemyPathNode[] { new EnemyPath.EnemyPathNode(), new EnemyPath.EnemyPathNode()};
            enemyPath.Path[0].Position = new Vector3(-50, 0, 0);
            enemyPath.Path[1].Position = new Vector3(50, 0, 0);
            enemyPath.Path[1].Time = 1;
            SelectedNode = -1;
        }

        for (int i = 0; i < enemyPath.Path.Length; i++)
        {
            newPosition = enemyPath.Path[i].Position;
            float ButtonSize = HandleUtility.GetHandleSize(newPosition) * .12f;
            float ButtonPickSize = ButtonSize * 1.2f;
            if (i != SelectedNode && Handles.Button(newPosition, Quaternion.identity, ButtonSize, ButtonPickSize, Handles.RectangleHandleCap))
            {
                SelectedNode = i;
            }
            if (i > 0)
            {
                Vector3 OldPosition = enemyPath.Path[i - 1].Position;

                //draw path line, with tighter dots the slower the movement (dot count is consistent over time, so the smaller the segment for a given time interval the tighter the dots)
                Handles.DrawDottedLine(newPosition, OldPosition, 1f/enemyPath.Path[i].Time * (newPosition-OldPosition).magnitude / 100);
                Vector3 ButtonPosition = (newPosition + OldPosition) / 2;

                Handles.Label(ButtonPosition, (enemyPath.Path[i].Time * enemyPath.PathScale).ToString());
                ButtonSize = HandleUtility.GetHandleSize(ButtonPosition) * .1f;
                ButtonPickSize = ButtonSize * 1.2f;

                if ((i == SelectedNode || i - 1 == SelectedNode) && Handles.Button(ButtonPosition, Quaternion.identity, ButtonSize,ButtonPickSize, Handles.CircleHandleCap))
                {
                    if (i-1 == SelectedNode)
                    {
                        SelectedNode++;
                    }
                    Undo.RecordObject(enemyPath, "Insert Path Node");
                    List<EnemyPath.EnemyPathNode> newPath = new List<EnemyPath.EnemyPathNode>(enemyPath.Path);
                    newPath.Insert(i - 1, new EnemyPath.EnemyPathNode());
                    newPath[i].Position = ButtonPosition;
                    newPath[i - 1].Time = enemyPath.Path[i - 1].Time;
                    newPath[i].Time = newPath[i+1].Time = enemyPath.Path[i].Time / 2;
                    newPath[i - 1].Position = OldPosition; //not sure why this is necessary, but if this line isn't here the previous node moves to 0,0,0
                    enemyPath.Path = newPath.ToArray();
                    break;
                }
            }
        }

        if (PreviewPath >= 0)
        {

            float previewTime = 0;
            float previewTimeOld = 0;

            for (int previewI = 1; previewI < enemyPath.Path.Length; previewI++)
            {
                previewTime += enemyPath.Path[previewI].Time;
                if (previewTime > PreviewPath)
                {
                    float t = (PreviewPath - previewTimeOld) / enemyPath.Path[previewI].Time;
                    Handles.DrawSolidDisc(Vector3.Lerp(enemyPath.Path[previewI - 1].Position, enemyPath.Path[previewI].Position, t), Vector3.forward, 2);
                    break;
                }
                previewTimeOld = previewTime;
            }
            PreviewPath += DeltaTime / enemyPath.PathScale;
            if (PreviewPath > enemyPath.TotalTime)
            {
                PreviewPath = 0;
            }
        }

    }

    public void UpdateDeltaTime()
    {
        //TODO: make sure this runs every frame
        DeltaTime = (float)(EditorApplication.timeSinceStartup - LastEditorTime);
        LastEditorTime = EditorApplication.timeSinceStartup;
    }

}
