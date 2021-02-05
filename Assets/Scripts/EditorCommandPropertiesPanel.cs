using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class EditorCommandPropertiesPanel : MonoBehaviour
{
    public static EditorCommandPropertiesPanel current;
    static Vector2? PositionSpace = null;
    static float? PositionTime = null;

    public VerticalLayoutGroup contents;
    static VerticalLayoutGroup newContents;

    public float GroupSpacing = 10;

    public EditorUIControls.EditorUISlider prefabSlider;
    public EditorUIControls.EditorUIToggle prefabToggle;
    public EditorUIControls.EditorUIInputField prefabInputField;

    void Awake()
    {
        current = this;
        newContents = Instantiate(contents, transform);
        contents.gameObject.SetActive(false);
    }

    public static void CallPanel(Vector2 position)
    {
        ClearPanel();
        current.gameObject.SetActive(true);
        PositionSpace = position;
        PositionTime = null;
        UpdatePanelPosition();
    }
    public static void CallPanel(float time)
    {
        ClearPanel();
        current.gameObject.SetActive(true);
        PositionTime = time;
        PositionSpace = null;
        UpdatePanelPosition();
    }

    public static void ClearPanel()
    {
        Layout.NestedLayouts.Clear();
        Destroy(newContents.gameObject);
        //destroying and then re-instantiating the contents object gets around the vertical layout component throwing errors when children are destroyed
        newContents = Instantiate(current.contents, current.transform);
        newContents.gameObject.SetActive(true);
    }
    public static void DismissPanel()
    {
        current.gameObject.SetActive(false);
    }
    public static void UpdatePanelPosition()
    {
        if (PositionSpace != null)
        {
            current.transform.position = (Vector3)PositionSpace;
        }
        else
        {
            //TODO: get position on (or off) screen that corresponds to the time on the timeline
        }
        //TODO: clamp position to level view window
        //TODO: change pivot based on which edges we're closest to
        //TODO: scale panel based on window size
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
            if (NestedLayouts.Count > 0) parent = NestedLayouts[0];
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
    }
}
