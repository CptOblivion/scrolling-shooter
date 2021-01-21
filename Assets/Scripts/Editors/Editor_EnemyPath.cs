using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(EnemyPath))]
public class Editor_EnemyPaths : Editor
{
    //TODO: get actual screen height and maximum level width
    float LevelHalfHeight = 24;
    float LevelHalfWidth = 48;
    int[] NodeControlArray;
    EnemyPath enemyPath;

    int SelectedNode;
    bool SelectHandleOrEdge;

    bool DrawSegmentTimes = false;
    bool DrawSegmentSpeeds = false;
    bool MaintainSegmentSpeed = true;
    double LastEditorTime;
    float PreviewSegmentTime;
    int PreviewIndex;
    float DeltaTime;
    SerializedProperty TotalTime;
    SerializedProperty Path;
    SerializedProperty PathScale;
    SerializedProperty PathStart;
    SerializedProperty PathEnd;
    private void OnEnable()
    {
        SelectedNode = -1;
        PreviewIndex = -1;
        Path = serializedObject.FindProperty("Path");
        PathScale = serializedObject.FindProperty("PathScale");
        TotalTime = serializedObject.FindProperty("TotalTime");
        PathStart = serializedObject.FindProperty("PathStart");
        PathEnd = serializedObject.FindProperty("PathEnd");
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
        GUILayout.BeginHorizontal();
        {
            EditorGUIUtility.labelWidth = 60;
            EditorGUILayout.PropertyField(PathStart, GUILayout.ExpandWidth(false));
            GUILayout.Space(30);
            EditorGUILayout.PropertyField(PathEnd, GUILayout.ExpandWidth(false));
        }GUILayout.EndHorizontal();
        if (GUILayout.Button("Reset Path"))
        {
            ResetPath();
        }
        EditorGUILayout.PropertyField(Path);
        EditorGUILayout.Slider(PathScale, 0.01f, 10);
        UpdateTotalTime();


        GUIStyle style = GUIStyle.none;
        style.normal.textColor = Color.white;
        EditorGUILayout.LabelField($"Path time: {TotalTime.floatValue * PathScale.floatValue}", style);

        EditorGUILayout.Space();

        //play preview animation
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("Preview Path", GUILayout.ExpandWidth(false)))
            {
                if (PreviewIndex >= 0)
                {
                    PreviewIndex = -1;
                }
                else
                {
                    PreviewSegmentTime = 0;
                    PreviewIndex = 0;
                }
            }
            GUILayout.Space(10);
            DrawSegmentSpeeds = GUILayout.Toggle(DrawSegmentSpeeds, "Draw segment speeds");
            GUILayout.Space(10);
            DrawSegmentTimes = GUILayout.Toggle(DrawSegmentTimes, "Draw segment times");
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();
        MaintainSegmentSpeed = GUILayout.Toggle(MaintainSegmentSpeed, "Keep segment Speed");

        if (SelectedNode >= 0) {
            if (SelectHandleOrEdge)
            {
                //TODO: delete node button
            }
            else
            {
                GUILayout.Space(30);
                SerializedProperty Time = Path.GetArrayElementAtIndex(SelectedNode).FindPropertyRelative("Time");
                EditorGUILayout.BeginVertical("Box");
                {
                    GUILayout.Label($"Segment time: {Time.floatValue}");
                    Time.floatValue = GUILayout.HorizontalSlider(Time.floatValue, 0, 1);
                    UpdateTotalTime();
                    GUILayout.Space(15);
                }
                EditorGUILayout.EndVertical();
            }
        }

        SerializedProperty PathStartPosition = Path.GetArrayElementAtIndex(0).FindPropertyRelative("Position");
        SerializedProperty PathEndPosition = Path.GetArrayElementAtIndex(Path.arraySize-1).FindPropertyRelative("Position");
        Vector3 swapSpace;
        swapSpace = PathStartPosition.vector3Value;
        switch (PathStart.enumValueIndex)
        {
            case 0:
                swapSpace.x = -LevelHalfWidth;
                break;
            case 1:
                swapSpace.y = LevelHalfHeight;
                break;
            case 2:
                swapSpace.y = -LevelHalfHeight ;
                break;
        }
        PathStartPosition.vector3Value = swapSpace;
        swapSpace = PathEndPosition.vector3Value;
        switch (PathEnd.enumValueIndex)
        {
            case 0:
                swapSpace.x = LevelHalfWidth;
                break;
            case 1:
                swapSpace.y = LevelHalfHeight;
                break;
            case 2:
                swapSpace.y = -LevelHalfHeight;
                break;
        }
        PathEndPosition.vector3Value = swapSpace;
        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (Event.current.type == EventType.MouseUp && NodeControlArray != null)
        {
            for(int i = 0; i < NodeControlArray.Length; i++)
            {
                if (NodeControlArray[i]-1 == GUIUtility.hotControl)
                {
                    SelectedNode = i;
                    SelectHandleOrEdge = true;
                }
            }
        }

        UpdateDeltaTime();

        //TODO: Indicate path direction
        //TODO: smooth path?

        Handles.DrawWireCube(Vector3.zero, new Vector3(LevelHalfWidth*2, LevelHalfHeight*2, 10));

        enemyPath = (EnemyPath)target;
        if (SelectedNode > enemyPath.Path.Length)
        {
            SelectedNode = -1;
        }

        Vector3 newPosition;
        float ButtonSize;
        float ButtonPickSize;

        if (enemyPath.Path.Length < 2)
        {
            ResetPath();
        }

        if (Event.current.type == EventType.Layout)
        {
            NodeControlArray = new int[enemyPath.Path.Length];
        }

        for (int i = 0; i < enemyPath.Path.Length; i++)
        {

            newPosition = enemyPath.Path[i].Position;
            ButtonSize = HandleUtility.GetHandleSize(newPosition) * .12f;
            ButtonPickSize = ButtonSize * 1.2f;

            EditorGUI.BeginChangeCheck();
            newPosition = Handles.FreeMoveHandle(newPosition, Quaternion.identity, ButtonSize, Vector3.one, Handles.RectangleHandleCap);
            NodeControlArray[i] = GUIUtility.GetControlID(FocusType.Passive);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(enemyPath, "Move Path Node");
                if (i == 0)
                {
                    switch (enemyPath.PathStart)
                    {
                        case EnemyPath.PathCap.Side:
                            newPosition.x = -LevelHalfWidth;
                            break;
                        case EnemyPath.PathCap.Top:
                            newPosition.y = LevelHalfHeight;
                            break;
                        case EnemyPath.PathCap.Bottom:
                            newPosition.y = -LevelHalfHeight;
                            break;
                    }
                }
                else if (i == enemyPath.Path.Length-1)
                {
                    switch (enemyPath.PathEnd)
                    {
                        case EnemyPath.PathCap.Side:
                            newPosition.x = LevelHalfWidth;
                            break;
                        case EnemyPath.PathCap.Top:
                            newPosition.y = LevelHalfHeight;
                            break;
                        case EnemyPath.PathCap.Bottom:
                            newPosition.y = -LevelHalfHeight;
                            break;
                    }
                }

                if (MaintainSegmentSpeed)
                {
                    if (i == 0)
                    {
                        float D2 = (enemyPath.Path[i].Position - enemyPath.Path[i + 1].Position).magnitude;
                        enemyPath.Path[i].Position = newPosition;
                        enemyPath.Path[i + 1].Time *= (enemyPath.Path[i].Position - enemyPath.Path[i + 1].Position).magnitude / D2;
                    }
                    else if (i == enemyPath.Path.Length - 1)
                    {
                        float D1 = (enemyPath.Path[i - 1].Position - enemyPath.Path[i].Position).magnitude;
                        enemyPath.Path[i].Position = newPosition;
                        enemyPath.Path[i].Time *= (enemyPath.Path[i - 1].Position - enemyPath.Path[i].Position).magnitude / D1;
                    }
                    else
                    {
                        float D1 = (enemyPath.Path[i - 1].Position - enemyPath.Path[i].Position).magnitude;
                        float D2 = (enemyPath.Path[i].Position - enemyPath.Path[i + 1].Position).magnitude;
                        enemyPath.Path[i].Position = newPosition;
                        enemyPath.Path[i].Time *= (enemyPath.Path[i - 1].Position - enemyPath.Path[i].Position).magnitude / D1;
                        enemyPath.Path[i + 1].Time *= (enemyPath.Path[i].Position - enemyPath.Path[i + 1].Position).magnitude / D2;
                    }
                }
                else
                {
                    enemyPath.Path[i].Position = newPosition;
                }
            }

            if (SelectHandleOrEdge && SelectedNode == i)
            {

                Handles.BeginGUI();
                {
                    GUILayout.BeginArea(new Rect(HandleUtility.WorldToGUIPoint(newPosition), Vector2.one * 100));
                    GUILayout.BeginVertical("Box");
                    if (GUILayout.Button(" - ", GUILayout.ExpandWidth(false)))
                    {
                        SelectedNode = -1;
                        Undo.RecordObject(enemyPath, "Remove Path Node");
                        List<EnemyPath.EnemyPathNode> newPath = new List<EnemyPath.EnemyPathNode>(enemyPath.Path);
                        if (i < enemyPath.Path.Length - 1)
                        {
                            newPath[i + 1].Time += enemyPath.Path[i].Time;
                        }
                        newPath.RemoveAt(i);
                        enemyPath.Path = newPath.ToArray();
                        GUILayout.EndVertical();
                        GUILayout.EndArea();
                        Handles.EndGUI();
                        break;
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndArea();
                }
                Handles.EndGUI();
            }

            if (i > 0)
            {
                Vector3 OldPosition = enemyPath.Path[i - 1].Position;

                //draw path line, with tighter dots the slower the movement (dot count is consistent over time, so the smaller the segment for a given time interval the tighter the dots)
                Handles.DrawDottedLine(newPosition, OldPosition, 1f/enemyPath.Path[i].Time * (newPosition-OldPosition).magnitude / 100);
                Vector3 ButtonPosition = (newPosition + OldPosition) / 2;
                if (DrawSegmentTimes || DrawSegmentSpeeds) {
                    string segmentLabel = "";
                    float pathTime = enemyPath.Path[i].Time * enemyPath.PathScale;
                    if (DrawSegmentSpeeds)
                    {

                        segmentLabel += (enemyPath.Path[i-1].Position - enemyPath.Path[i].Position).magnitude/pathTime;
                        if (DrawSegmentTimes) segmentLabel += ", ";
                    }
                    if (DrawSegmentTimes)
                    {
                        segmentLabel += pathTime;
                    }
                    Handles.Label(ButtonPosition, segmentLabel);
                }
                ButtonSize = HandleUtility.GetHandleSize(ButtonPosition) * .1f;
                ButtonPickSize = ButtonSize * 1.2f;

                if (Handles.Button(ButtonPosition, Quaternion.identity, ButtonSize, ButtonPickSize, Handles.CircleHandleCap))
                {
                    SelectHandleOrEdge = false;
                    SelectedNode = i;
                }
                if (!SelectHandleOrEdge && SelectedNode == i)
                {
                    Handles.BeginGUI();
                    {
                        GUILayout.BeginArea(new Rect(HandleUtility.WorldToGUIPoint(ButtonPosition), Vector2.one * 100));
                        if (DrawSegmentSpeeds || DrawSegmentTimes)
                        {
                            GUILayout.Space(15);
                        }
                        GUILayout.BeginVertical("Box");
                        GUILayout.Label(enemyPath.Path[i].Time.ToString());
                        enemyPath.Path[i].Time = GUILayout.HorizontalSlider(enemyPath.Path[i].Time, 0, 1);
                        UpdateTotalTime();
                        GUILayout.Space(15);
                        if (GUILayout.Button(" + ", GUILayout.ExpandWidth(false)))
                        {
                            SelectedNode = -1;
                            Undo.RecordObject(enemyPath, "Insert Path Node");
                            List<EnemyPath.EnemyPathNode> newPath = new List<EnemyPath.EnemyPathNode>(enemyPath.Path);
                            newPath.Insert(i - 1, new EnemyPath.EnemyPathNode());
                            newPath[i].Position = ButtonPosition;
                            newPath[i - 1].Time = enemyPath.Path[i - 1].Time;
                            newPath[i].Time = newPath[i + 1].Time = enemyPath.Path[i].Time / 2;
                            newPath[i - 1].Position = OldPosition; //not sure why this is necessary, but if this line isn't here the previous node moves to 0,0,0
                            enemyPath.Path = newPath.ToArray();
                            GUILayout.EndVertical();
                            GUILayout.EndArea();
                            Handles.EndGUI();
                            break;
                        }
                        GUILayout.EndVertical();
                        GUILayout.EndArea();
                    }Handles.EndGUI();
                }
            }
        }

        if (PreviewIndex >= 0)
        {
            float SegmentTime = enemyPath.Path[PreviewIndex + 1].Time;
            Handles.DrawSolidDisc(Vector3.Lerp(enemyPath.Path[PreviewIndex].Position, enemyPath.Path[PreviewIndex+1].Position, PreviewSegmentTime/SegmentTime), Vector3.forward, 2);
            PreviewSegmentTime += DeltaTime / enemyPath.PathScale;
            while (PreviewSegmentTime > SegmentTime)
            {
                PreviewSegmentTime -= SegmentTime;

                PreviewIndex++;
                if (PreviewIndex > enemyPath.Path.Length - 2)
                {
                    PreviewIndex = 0;
                }
                SegmentTime = enemyPath.Path[PreviewIndex + 1].Time;
            }
        }

    }

    void UpdateDeltaTime()
    {
        //TODO: make sure this runs every frame
        DeltaTime = (float)(EditorApplication.timeSinceStartup - LastEditorTime);
        LastEditorTime = EditorApplication.timeSinceStartup;
    }
    void ResetPath()
    {
        //TODO: doesn't work when called from OnInspectorGUI
        enemyPath.Path = new EnemyPath.EnemyPathNode[] { new EnemyPath.EnemyPathNode(), new EnemyPath.EnemyPathNode() };
        enemyPath.Path[0].Position = new Vector3(-50, 0, 0);
        enemyPath.Path[1].Position = new Vector3(50, 0, 0);
        enemyPath.Path[1].Time = 1;
        enemyPath.Path[0].Time = 0;
        SelectedNode = -1;
        Debug.Log("path reset");
    }
    void UpdateTotalTime()
    {
        TotalTime.floatValue = 0;
        for (int i = 1; i < Path.arraySize; i++)
        {
            TotalTime.floatValue += Path.GetArrayElementAtIndex(i).FindPropertyRelative("Time").floatValue;
        }
    }
}
