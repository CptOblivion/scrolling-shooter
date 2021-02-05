
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelEditorSpawnedCommand : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    //TODO: make this a control or something, somewhere
    static readonly float CommandThickness = .5f;
    [System.Serializable]
    public class CommandContainer
    {
        static readonly float CommandAfterHoldOffset = .01f;
        public GameObject obj;
        public LevelParser.AvailableCommands CommandType;
        public float EditorTriggerTime;
        public float TriggerTime;
        public int CommandIndex;
        public LevelEditorCommandDragControl SliderSpawnTime = null;
        public RectTransform rect;

        protected Vector3 OldLocation = Vector2.zero;
        protected Vector2 ClickOrigin = Vector2.zero;
        //public bool Hovered { get; private set; } = false;
        public bool Selected { get; private set; } = false;

        protected virtual void InstantiateBase()
        {
            LevelEditorSpawnedCommand com = obj.GetComponent<LevelEditorSpawnedCommand>();
            if (com)
                com.command = this;
            else
                obj.AddComponent<LevelEditorSpawnedCommand>().command = this;
        }
        public virtual void SummonPropertiesPanel()
        {
            EditorCommandPropertiesPanel.CallPanel("Unknown command", this);
        }

        public virtual void HoverEnter()
        {
            if (!Selected)
            {
                SetVisualsHovered();
            }
        }
        public virtual void HoverExit()
        {

            if (!Selected)
            {
                SetVisualsIdle();
            }
        }
        public virtual void Select()
        {
            Selected = true;
            SetVisualsSelected();
            SummonPropertiesPanel();
        }
        public virtual void Deselect()
        {
            Selected = false;
            SetVisualsIdle();
            EditorCommandPropertiesPanel.DismissPanel();
        }
        public virtual void SetVisualsIdle()
        {

        }
        public virtual void SetVisualsHovered()
        {

        }

        public virtual void SetVisualsSelected()
        {
            SetVisualsHovered();
        }

        public virtual void AddVisualsToTimeline()
        {
            obj.transform.SetParent(LevelEditor.current.commandsTrack, false);
            rect = obj.AddComponent<RectTransform>();
            UpdateTimelineVisuals();

        }
        public virtual void UpdateTimelineVisuals()
        {
            rect = (RectTransform)obj.transform;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.anchorMin = new Vector2(0, EditorTriggerTime / LevelEditor.LevelDurationEditor);
            rect.anchorMax = new Vector2(1, EditorTriggerTime + CommandThickness / LevelEditor.LevelDurationEditor);
        }
        public virtual void PointerDown(PointerEventData data)
        {
            ClickOrigin = data.position;
        }
        public virtual void BeginDragTimeline(PointerEventData data)
        {
            OldLocation = new Vector2(0, EditorTriggerTime);
        }
        public virtual void DragTimeline(PointerEventData data)
        {
            TriggerTime = EditorTriggerTime = Mathf.Clamp(OldLocation.y + (data.position.y - ClickOrigin.y) / LevelEditor.CommandDragScale * LevelEditor.LevelDurationEditor, 0, LevelEditor.LevelDurationEditor);
            foreach (LevelHoldContainer hold in LevelEditor.LevelHolds)
            {
                if (EditorTriggerTime > hold.EditorTriggerTime)
                {
                    if (EditorTriggerTime < hold.EditorTriggerTime + LevelEditor.EditorHoldDelay)
                    {
                        TriggerTime = hold.TriggerTime;
                        EditorTriggerTime = hold.EditorTriggerTime;
                    }
                    else
                    {
                        TriggerTime -= LevelEditor.EditorHoldDelay;
                    }
                }
            }
            UpdateTime();
        }
        public virtual void EndDragTimeline(PointerEventData data)
        {

        }


        public virtual void AddTimeCollider(GameObject OverrideOb = null)
        {
            if (OverrideOb)
            {
                SliderSpawnTime = OverrideOb.AddComponent<LevelEditorCommandDragControl>();
            }
            else
            {
                SliderSpawnTime = obj.AddComponent<LevelEditorCommandDragControl>();
            }
            SliderSpawnTime.DragAxis = LevelEditorCommandDragControl.Axes.Y;
            SliderSpawnTime.ControlInput = LevelEditorCommandDragControl.ControlInputs.SpawnTime;
            SliderSpawnTime.command = this;
        }
        public virtual void UpdateTime()
        {

        }
    }

    public class LevelPositionalContainer : CommandContainer
    {
        public Vector3 StartPosition;
        public float TriggerPosition;
        public float Life;
        public Animation anim = null;
        public EnemyPath path = null;
        public bool parallaxScroll = false;
        public UILine line = null;
        public Color LineBaseColor;
        public LevelEditorCommandDragControl ControlSpawnLocation = null;

        public virtual void BeginDragPosition(PointerEventData data)
        {
            OldLocation = StartPosition;
        }

        public virtual void DragPosition(PointerEventData data)
        {
            //filter X or Y depending on animation/path (if neither, XY)
            //change origin location
            //clamp to screen? (Or indicate offscreen somehow)
            //update object
        }
        public virtual void EndDragPosition(PointerEventData data)
        {

        }
        protected virtual void InstantiateBase(GameObject gameObject, Vector3 startPosition, float triggerTime, float editorTime, float triggerPos)
        {
            obj = gameObject;
            StartPosition = startPosition;
            TriggerTime = triggerTime;
            EditorTriggerTime = editorTime;
            TriggerPosition = triggerPos;
            Life = -1;
            CommandType = LevelParser.AvailableCommands.Spawn;
            base.InstantiateBase();
        }

        public override void SetVisualsHovered()
        {
            if (line)
            {
                line.SetColor(LevelEditor.current.CommandLineSelectedColor);
                line.SetWidth(LevelEditor.current.UILineHoveredWidth);
                line.transform.SetAsLastSibling();
                if (line.tail)
                    //TODO: instead of setaslastsibling, move selected and hovered lines to a different higher-level parent
                    line.tail.transform.SetAsLastSibling();
            }
        }

        public override void SetVisualsIdle()
        {
            if (line)
            {
                line.SetColor(LineBaseColor);
                line.SetWidth(LevelEditor.current.UILineWidth);
                //TODO: reparent line and line.tail to line parent
            }
        }

        public override void Select()
        {
            base.Select();
            if (SliderSpawnTime)
            {
                SliderSpawnTime.SetActive(true);
            }
        }

        public override void Deselect()
        {
            base.Deselect();
            if (SliderSpawnTime)
            {
                SliderSpawnTime.SetActive(false);
            }
            if (line)
            {
                line.SetActive(line.gameObject.activeInHierarchy);
            }
        }

        public override void UpdateTime()
        {
            base.UpdateTime();
            TriggerPosition = LevelEditor.GetDistanceTraveledAtTime(EditorTriggerTime);

            LevelEditor.DetermineCommandLifespan(this);
            LevelEditor.UpdateCommand(this);
        }
    }

    public class LevelHoldContainer : CommandContainer
    {
        public float DelayPreview = 5;
        public Image imageBorder;
        public LevelHoldContainer(GameObject ob, int commandIndex, float triggerTime, float editorTime)
        {
            CommandType = LevelParser.AvailableCommands.HoldForDeath;
            CommandIndex = commandIndex;
            TriggerTime = triggerTime;
            EditorTriggerTime = editorTime;
            obj = ob;
            InstantiateBase();
        }
        public override void DragTimeline(PointerEventData data)
        {
            base.DragTimeline(data);
            LevelEditor.RebuildScrollSpeedCache();
            foreach(LevelHoldContainer hold in LevelEditor.LevelHolds)
            {
                hold.UpdateTimelineVisuals();
            }
        }
        public override void UpdateTimelineVisuals()
        {
            base.UpdateTimelineVisuals();
            rect.anchorMax = new Vector2(1, (EditorTriggerTime + LevelEditor.EditorHoldDelay) / LevelEditor.LevelDurationEditor);
        }
        public override void AddVisualsToTimeline()
        {
            base.AddVisualsToTimeline();
            obj.AddComponent<RawImage>().color = LevelEditor.current.TimelineHoldColor;
            imageBorder = new GameObject().AddComponent<Image>();
            imageBorder.transform.SetParent(rect, false);
            RectTransform r = imageBorder.gameObject.GetComponent<RectTransform>();
            r.offsetMin = r.offsetMax = r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            imageBorder.sprite = LevelEditor.current.imageCommandFrame;
            imageBorder.type = Image.Type.Sliced;
            SetVisualsIdle();
        }
        public override void SetVisualsIdle()
        {
            base.SetVisualsIdle();
            imageBorder.color = Color.black;
        }
        public override void SetVisualsHovered()
        {
            base.SetVisualsHovered();
            imageBorder.color = LevelEditor.current.CommandLineSelectedColor;
        }
        public override void SetVisualsSelected()
        {
            base.SetVisualsSelected();
            imageBorder.color = LevelEditor.current.CommandLineSelectedColor;
        }
    }

    public class ScrollSpeedContainer: CommandContainer
    {
        public float NewSpeed;
        public float LerpTime;
        public float OldSpeed = 0;
        public RawImage imageSlope;
        public RawImage imageBlock;
        public Image imageFrame;
        public RectTransform rectSlope;
        public RectTransform rectBlock;
        public RectTransform rectFill;
        public int ScrollSpeedIndex = 0;
        public ScrollSpeedContainer(GameObject ob, int commandIndex, float triggerTime, float editorTime, float speed, float lerp)
        {
            CommandType = LevelParser.AvailableCommands.ScrollSpeed;
            CommandIndex = commandIndex;
            TriggerTime = triggerTime;
            EditorTriggerTime = editorTime;
            NewSpeed = speed;
            LerpTime = lerp;
            obj = ob;
            InstantiateBase();
        }

        public override void SummonPropertiesPanel()
        {
            base.SummonPropertiesPanel();
            EditorCommandPropertiesPanel.SetTitle("Set Scroll Speed");
            EditorUIControls.EditorUISlider newSlider;
            newSlider = EditorCommandPropertiesPanel.Layout.AddSlider("New Speed", 0, LevelEditor.TempMaxScrollSpeed, false);
            newSlider.slider.onValueChanged.AddListener(SetSpeed);
            newSlider.slider.value = NewSpeed;
            newSlider = EditorCommandPropertiesPanel.Layout.AddSlider("Lerp Time", 0, 5, false);
            newSlider.slider.onValueChanged.AddListener(SetLerp);
            newSlider.slider.value = LerpTime;

            EditorCommandPropertiesPanel.Layout.FinalizeLayout();
        }

        void SetSpeed(float f)
        {
            NewSpeed = f;
            LevelEditor.RebuildScrollSpeedCache();
            UpdateTimelineVisuals();
            if (LevelEditor.ScrollSpeeds.Count > ScrollSpeedIndex + 1)
            {
                LevelEditor.ScrollSpeeds[ScrollSpeedIndex + 1].UpdateTimelineVisuals();
            }
        }

        void SetLerp(float f)
        {
            LerpTime = f;
            AvoidCollisions();
            if (LevelEditor.ScrollSpeeds.Count > ScrollSpeedIndex + 1)
            {
                LevelEditor.ScrollSpeeds[ScrollSpeedIndex + 1].UpdateTimelineVisuals();
            }
            //TODO: test intersection with other scrollspeeds now that lerp has changed
        }

        void AvoidCollisions()
        {
            //TODO: clamp so we can't be pushed out the bottom or top of the timeline
            //TODO: case for there's no valid space to put this
            float HighPosition = EditorTriggerTime;
            float LowPosition = EditorTriggerTime;

            float HighPositionReal = TriggerTime;
            foreach (ScrollSpeedContainer container in LevelEditor.ScrollSpeeds)
            {
                if (IntersectsWithScroll(HighPosition, container))
                {
                    HighPosition = container.EditorTriggerTime + container.LerpTime;

                    HighPositionReal = container.TriggerTime + container.LerpTime;
                }
            }

            if (HighPosition != EditorTriggerTime)
            {
                //if we're intersecting with another lerp and needed to move, run the series the other way to see if we can find a valid spot closer to the cursor
                float LowPositionReal = TriggerTime;
                for (int i = LevelEditor.ScrollSpeeds.Count - 1; i >= 0; i--)
                {
                    if (IntersectsWithScroll(LowPosition, LevelEditor.ScrollSpeeds[i]))
                    {
                        LowPosition = LevelEditor.ScrollSpeeds[i].EditorTriggerTime - LerpTime;
                        LowPositionReal = LevelEditor.ScrollSpeeds[i].TriggerTime - LerpTime;
                    }
                }
                if (Mathf.Abs(HighPosition - EditorTriggerTime) > Mathf.Abs(LowPosition - EditorTriggerTime))
                {
                    EditorTriggerTime = LowPosition;
                    TriggerTime = LowPositionReal;
                }
                else
                {
                    EditorTriggerTime = HighPosition;
                    TriggerTime = HighPositionReal;
                }
                //TODO: block overlap with level holds as well

            }

            LevelEditor.RebuildScrollSpeedCache();

            bool IntersectsWithScroll(float InputTime, ScrollSpeedContainer container)
            {
                return container != this &&
                    ((InputTime <= container.EditorTriggerTime && InputTime + LerpTime >= container.EditorTriggerTime) ||
                    (InputTime >= container.EditorTriggerTime && InputTime <= container.EditorTriggerTime + container.LerpTime));
            }

        }

        public override void UpdateTime()
        {
            base.UpdateTime();
            UpdateTimelineVisuals();
        }

        public override void DragTimeline(PointerEventData data)
        {
            base.DragTimeline(data);
            AvoidCollisions();
        }

        public override void AddVisualsToTimeline()
        {
            GameObject slopeOb = new GameObject("slope");
            slopeOb.transform.SetParent(obj.transform, false);
            rectSlope = slopeOb.AddComponent<RectTransform>();
            imageSlope = slopeOb.AddComponent<RawImage>();
            imageSlope.texture = LevelEditor.current.imageScrollSpeedLerp;
            imageSlope.color = LevelEditor.current.ScrollSpeedNodeColor;
            rectSlope.offsetMin = rectSlope.offsetMax = Vector2.zero;

            GameObject blockOb = new GameObject("body");
            blockOb.transform.SetParent(obj.transform, false);
            rectBlock = blockOb.AddComponent<RectTransform>();
            imageBlock = blockOb.AddComponent<RawImage>();
            imageBlock.color = LevelEditor.current.ScrollSpeedNodeColor;
            rectBlock.anchorMax = Vector2.one;
            rectBlock.offsetMin = rectBlock.offsetMax = Vector2.zero;

            GameObject fillOb = new GameObject("scrollspeedFill");
            fillOb.transform.SetParent(LevelEditor.current.commandsTrack, false);
            rectFill = fillOb.AddComponent<RectTransform>();

            RawImage imageFill = fillOb.AddComponent<RawImage>();
            imageFill.color = LevelEditor.current.ScrollSpeedFillColor;
            imageFill.raycastTarget = false;

            rectFill.offsetMax = rectFill.offsetMin = Vector2.zero;

            base.AddVisualsToTimeline();

            UpdateTimelineVisuals();

            imageFrame = obj.AddComponent<Image>();
            imageFrame.sprite = LevelEditor.current.imageCommandFrame;
            imageFrame.type = Image.Type.Sliced;
            SetVisualsIdle();
        }

        public override void UpdateTimelineVisuals()
        {
            base.UpdateTimelineVisuals();
            rect.anchorMax = new Vector2(1, (EditorTriggerTime + Mathf.Max(LerpTime, CommandThickness)) / LevelEditor.LevelDurationEditor);
            if (NewSpeed > OldSpeed)
            {
                imageSlope.uvRect = new Rect(0, 1, 1, -1);
            }
            else
            {
                imageSlope.uvRect = new Rect(0, 0, 1, 1);
            }

            float Midpoint = 1-(Mathf.Min(OldSpeed, NewSpeed) / LevelEditor.TempMaxScrollSpeed);
            float max = 1-(Mathf.Max(OldSpeed, NewSpeed) / LevelEditor.TempMaxScrollSpeed); 
            rectBlock.anchorMin = new Vector2(Midpoint, 0);
            rectSlope.anchorMax = new Vector2(Midpoint, 1);
            rectSlope.anchorMin = new Vector2(max, 0);

        }

        public void UpdateFill()
        {
            float TempMaxScrollSpeed = 40;
            rectFill.anchorMin = new Vector2(1-(NewSpeed / TempMaxScrollSpeed), rect.anchorMax.y);

            if (ScrollSpeedIndex == LevelEditor.ScrollSpeeds.Count - 1)
            {
                rectFill.anchorMax = new Vector2(1, 1);
            }
            else
            {
                rectFill.anchorMax = new Vector2(1, LevelEditor.ScrollSpeeds[ScrollSpeedIndex + 1].rect.anchorMin.y);
            }
        }
        public override void SetVisualsIdle()
        {
            base.SetVisualsIdle();
            imageFrame.color = LevelEditor.current.ScrollSpeedNodeColor;
        }

        public override void SetVisualsHovered()
        {
            base.SetVisualsHovered();
            imageFrame.color = LevelEditor.current.CommandLineSelectedColor;
        }
        public override void SetVisualsSelected()
        {
            base.SetVisualsSelected();
            imageFrame.color = LevelEditor.current.CommandLineSelectedColor;
        }
    }
    public class SpawnedObjectContainer: LevelPositionalContainer
    {
        public float TriggerOffsetTime = 0;
        public int ParentSpawnedObject;
        public SpawnedObjectContainer[] SpawnerChildren;

        public SpawnedObjectContainer(GameObject gameObject, int commandIndex, Vector3 startPosition, float triggerTime, float editorTime, float triggerPos)
        {
            CommandIndex = commandIndex;
            InstantiateBase(gameObject, startPosition, triggerTime, editorTime, triggerPos);
        }

        public SpawnedObjectContainer(GameObject gameObject, Vector3 startPosition, float triggerTime, float editorTime, float triggerPos)
        {
            InstantiateBase(gameObject, startPosition, triggerTime, editorTime, triggerPos);
        }

        public override void SummonPropertiesPanel()
        {
            base.SummonPropertiesPanel();
            EditorCommandPropertiesPanel.SetTitle("Spawn object");
        }

        protected override void InstantiateBase(GameObject gameObject, Vector3 startPosition, float triggerTime, float editorTime, float triggerPos)
        {
            base.InstantiateBase(gameObject, startPosition, triggerTime, editorTime, triggerPos);
            ParentSpawnedObject = -1;
        }

        public override void HoverEnter()
        {
            base.HoverEnter();
            if (!Selected)
            {
                if (SpawnerChildren != null)
                {
                    foreach (SpawnedObjectContainer child in SpawnerChildren)
                    {
                        child.SetVisualsHovered();
                    }
                }
            }
        }
        public override void HoverExit()
        {
            base.HoverExit();
            if (!Selected)
            {
                if (SpawnerChildren != null)
                {
                    foreach (SpawnedObjectContainer child in SpawnerChildren)
                    {
                        child.SetVisualsIdle();
                    }
                }
            }
        }
        public override void Select()
        {
            base.Select();
            if (SpawnerChildren != null)
            {
                foreach(SpawnedObjectContainer child in SpawnerChildren)
                {
                    child.SetVisualsSelected();
                }
            }
        }

        public override void Deselect()
        {
            base.Deselect();
            if (SpawnerChildren != null)
            {
                foreach (SpawnedObjectContainer child in SpawnerChildren)
                {
                    child.SetVisualsIdle();
                }
            }
        }
        public override void AddTimeCollider(GameObject ob)
        {
            base.AddTimeCollider(ob);
            //TODO: make tail collider thicker
        }
        public override void UpdateTime()
        {
            base.UpdateTime();

            if (SpawnerChildren != null)
            {
                foreach (SpawnedObjectContainer childContainer in SpawnerChildren)
                {
                    childContainer.EditorTriggerTime = EditorTriggerTime + childContainer.TriggerOffsetTime;
                    childContainer.TriggerPosition = LevelEditor.GetDistanceTraveledAtTime(childContainer.EditorTriggerTime);
                    LevelEditor.DetermineCommandLifespan(childContainer);
                }
            }
            LevelEditor.UpdateCommand(this);
        }
    }

    //public SpawnedObjectContainer command;
    public CommandContainer command;
    bool Hovered = false;

    private void Awake()
    {
        foreach(Transform child in transform.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = 5;
        }
    }

    public void OnPointerEnter(PointerEventData data)
    {
        if (!data.dragging)
        {
            command.HoverEnter();
        }
    }
    public void OnPointerExit(PointerEventData data)
    {
        if (!data.dragging)
        {
            command.HoverExit();
        }
    }

    public void OnPointerClick(PointerEventData data)
    {
        if (!data.dragging)
        {
            Select();
        }
    }

    public void Select()
    {
        LevelEditor.SelectCommand(this);
    }

    public void Deselect()
    {
        command.Deselect();
    }

}
