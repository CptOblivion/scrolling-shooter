
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
        }
        public virtual void Deselect()
        {
            Selected = false;
            SetVisualsIdle();
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
            OldLocation = new Vector2(0, TriggerTime);
        }
        public virtual void DragTimeline(PointerEventData data)
        {
            TriggerTime = EditorTriggerTime = Mathf.Clamp(OldLocation.y + (data.position.y - ClickOrigin.y) * LevelEditor.DragScale / LevelEditor.ScrollZoom, 0, LevelEditor.LevelDurationEditor);
            foreach (LevelHoldContainer hold in LevelEditor.LevelHolds)
            {
                if (EditorTriggerTime > hold.EditorTriggerTime)
                {
                    //TODO: match position to cursor position better
                    //  using EditorTriggerTime as tbe base and subtracting holds back out of TriggerTime caused flickering in commands that caused level scroll cache rebuilds
                    //  maybe that's fixable, but it might just be easier to hack it from this direction instead
                    EditorTriggerTime += LevelEditor.EditorHoldDelay;
                    if (EditorTriggerTime < hold.EditorTriggerTime + LevelEditor.EditorHoldDelay)
                    {
                        EditorTriggerTime = hold.EditorTriggerTime + LevelEditor.EditorHoldDelay + CommandAfterHoldOffset;
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
        public LevelHoldContainer(GameObject ob, int commandIndex, float triggerTime, float editorTime)
        {
            CommandType = LevelParser.AvailableCommands.HoldForDeath;
            CommandIndex = commandIndex;
            TriggerTime = triggerTime;
            EditorTriggerTime = editorTime;
            obj = ob;
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
        }
    }

    public class ScrollSpeedContainer: CommandContainer
    {
        public float NewSpeed;
        public float LerpTime;
        public float OldSpeed = 0;
        public RawImage imageSlope;
        public RawImage imageBlock;
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
        }

        public override void UpdateTime()
        {
            base.UpdateTime();
            UpdateTimelineVisuals();
        }

        public override void DragTimeline(PointerEventData data)
        {
            base.DragTimeline(data);
            LevelEditor.RebuildScrollSpeedCache();
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

            Destroy(obj.GetComponent<RawImage>());

        }

        public override void UpdateTimelineVisuals()
        {
            float TempMaxScrollSpeed = 40;
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

            float Midpoint = 1-(Mathf.Min(OldSpeed, NewSpeed) / TempMaxScrollSpeed);
            float max = 1-(Mathf.Max(OldSpeed, NewSpeed) / TempMaxScrollSpeed); 
            rectBlock.anchorMin = new Vector2(Midpoint, 0);
            rectSlope.anchorMax = new Vector2(Midpoint, 1);
            rectSlope.anchorMin = new Vector2(max, 0);


            //TODO: case for OldSpeed and NewSpeed are both 0 (just fill the back of the node with a rectangle at all times?)
        }

        public void UpdateFill()
        {
            float TempMaxScrollSpeed = 40;
            rectFill.anchorMin = new Vector2(1-(NewSpeed / TempMaxScrollSpeed), rect.anchorMax.y);
            //TODO: replace the following anchormax.y with the anchormin.y of the *next* scroll speed change (or 1, if we're the last chagne)

            if (ScrollSpeedIndex == LevelEditor.ScrollSpeeds.Count - 1)
            {
                rectFill.anchorMax = new Vector2(1, 1);
            }
            else
            {
                rectFill.anchorMax = new Vector2(1, LevelEditor.ScrollSpeeds[ScrollSpeedIndex + 1].rect.anchorMin.y);
            }
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

    private void Awake()
    {
        foreach(Transform child in transform.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = 5;
        }
    }

    public void OnPointerEnter(PointerEventData data)
    {
        HoverEnter();
    }
    public void OnPointerExit(PointerEventData data)
    {
        HoverExit();
    }

    public void OnPointerClick(PointerEventData data)
    {
        LevelEditor.SelectCommand(this);
    }


    public void HoverEnter()
    {
        command.HoverEnter();
    }
    public void HoverExit()
    {
        command.HoverExit();
    }

    public void Select()
    {
        command.Select();
    }

    public void Deselect()
    {
        command.Deselect();
    }

}
