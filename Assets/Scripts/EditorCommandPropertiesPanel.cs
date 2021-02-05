using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class EditorCommandPropertiesPanel : MonoBehaviour
{
    public float PivotOffset = .1f;
    public static EditorCommandPropertiesPanel current;
    public Text title;
    public RectTransform FlexibleSpace;
    static LevelEditorSpawnedCommand.CommandContainer command = null;

    public VerticalLayoutGroup contents;
    static VerticalLayoutGroup newContents;

    public float GroupSpacing = 10;

    public EditorUIControls.EditorUISlider prefabSlider;
    public EditorUIControls.EditorUIToggle prefabToggle;
    public EditorUIControls.EditorUIInputField prefabInputField;

    static RectTransform parentRect;
    static RectTransform currentRect;

    void Awake()
    {
        current = this;
        currentRect = (RectTransform)transform;
        parentRect = (RectTransform)transform.parent;
        newContents = Instantiate(contents, transform);
        contents.gameObject.SetActive(false);
    }
    public static void CallPanel(string Title, LevelEditorSpawnedCommand.CommandContainer Command)
    {
        current.title.text = Title;
        ClearPanel();
        current.gameObject.SetActive(true);
        command = Command;
        UpdatePanelPosition();
    }

    public static void ClearPanel()
    {
        Layout.NestedLayouts.Clear();
        current.FlexibleSpace.SetParent(current.transform);
        Destroy(newContents.gameObject);
        //destroying and then re-instantiating the contents object gets around the vertical layout component throwing errors when children are destroyed
        newContents = Instantiate(current.contents, current.transform);
        current.FlexibleSpace.SetParent(newContents.transform);
        newContents.gameObject.SetActive(true);
    }
    public static void DismissPanel()
    {
        current.gameObject.SetActive(false);
    }
    public static void SetTitle(string Title)
    {
        current.title.text = Title;
    }
    public static void UpdatePanelPosition()
    {
        if (current.gameObject.activeInHierarchy)
        {
            LevelEditorSpawnedCommand.LevelPositionalContainer posCommand = command as LevelEditorSpawnedCommand.LevelPositionalContainer;
            if (posCommand != null && posCommand.obj.activeInHierarchy)
            {
                current.transform.position = posCommand.obj.transform.position;
            }
            else
            {
                current.transform.position = new Vector3(LevelEditor.LevelWidth / 2, LevelEditor.GetTimelinePosition(command.EditorTriggerTime), 0);
            }

            Vector3 LocalPos = current.transform.localPosition;



            LocalPos = new Vector3(Mathf.Clamp(LocalPos.x ,- parentRect.rect.width/2, parentRect.rect.width / 2),
                Mathf.Clamp(LocalPos.y , - parentRect.rect.height / 2, parentRect.rect.height / 2), 0);

            Vector2 Pivot = new Vector2(-current.PivotOffset, -current.PivotOffset);

            if (LocalPos.x > 0)
            {
                Pivot.x = 1 + current.PivotOffset;
            }
            if (LocalPos.y > 0)
            {
                Pivot.y = 1 + current.PivotOffset;
            }
            currentRect.pivot = Pivot;
            current.transform.localPosition = LocalPos;
        }
    }
    public class Layout
    {
        public static readonly List<RectTransform> NestedLayouts = new List<RectTransform>();
        public static void StartGroupHorizontal()
        {
            RectTransform newHorizontal = new GameObject("Horizontal Layout").AddComponent<RectTransform>();

            if (NestedLayouts.Count == 0)
                newHorizontal.SetParent(newContents.transform, false);
            else
            {
                newHorizontal.SetParent(NestedLayouts[0], false);
            }

            NestedLayouts.Insert(0, newHorizontal);

            HorizontalLayoutGroup group = newHorizontal.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = current.GroupSpacing;
        }
        public static void StartGroupVertical()
        {

            RectTransform newVertical = new GameObject("Vertical Layout").AddComponent<RectTransform>();

            if (NestedLayouts.Count == 0)
                newVertical.SetParent(newContents.transform, false);
            else
            {
                newVertical.SetParent(NestedLayouts[0], false);
            }

            NestedLayouts.Insert(0, newVertical);

            VerticalLayoutGroup group = newVertical.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = current.GroupSpacing;
        }
        public static void EndGroup()
        {
            if (NestedLayouts.Count > 0)
                NestedLayouts.RemoveAt(0);
            else
                Debug.LogError("Removing too many groups!");
        }
        public static EditorUIControls.EditorUISlider AddSlider(string label, float min, float max, bool IsInt)
        {
            RectTransform parent = (RectTransform)newContents.transform;
            if (NestedLayouts.Count > 0)parent = NestedLayouts[0];
            EditorUIControls.EditorUISlider newSlider = Instantiate(current.prefabSlider, parent);
            newSlider.label.text = label;
            newSlider.slider.minValue = min;
            newSlider.slider.maxValue = max;
            return newSlider;
        }
        public static EditorUIControls.EditorUIToggle AddToggle(string label)
        {
            RectTransform parent = (RectTransform)newContents.transform;
            if (NestedLayouts.Count > 0) parent = NestedLayouts[0];
            EditorUIControls.EditorUIToggle newToggle = Instantiate(current.prefabToggle, parent);
            newToggle.label.text = label;
            return newToggle;
        }
        public static EditorUIControls.EditorUIInputField AddInputField(string label)
        {
            RectTransform parent = (RectTransform)newContents.transform;
            if (NestedLayouts.Count > 0) parent = NestedLayouts[0];
            EditorUIControls.EditorUIInputField newInputField = Instantiate(current.prefabInputField, parent);
            newInputField.label.text = label;
            return newInputField;
        }
        public static void FinalizeLayout()
        {
            current.FlexibleSpace.SetAsLastSibling();
            if (NestedLayouts.Count > 0)
            {
                Debug.LogWarning("Didn't close all horizontal and vertical groups, careful with that!");
            }
            //TODO: re-scale panel based on size of contents
        }
    }
}
