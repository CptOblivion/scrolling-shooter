
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelEditorSpawnedCommand : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [System.Serializable]
    public class CommandContainer
    {
        static readonly float CommandAfterHoldOffset = .01f;
        static readonly float TempDragScale = 0.1f;
        public GameObject obj;
        public LevelParser.AvailableCommands CommandType;
        public float EditorTriggerTime;
        public float TriggerTime;
        public int CommandIndex;
        public LevelEditorCommandDragControl SliderSpawnTime = null;

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
            TriggerTime = EditorTriggerTime = Mathf.Clamp(OldLocation.y + (data.position.y - ClickOrigin.y) * TempDragScale, 0, LevelEditor.LevelDurationEditor);
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

    [System.Serializable]
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

    [System.Serializable]
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
            RectTransform r = (RectTransform)obj.transform;
            r.anchorMin = new Vector2(0, EditorTriggerTime / LevelEditor.LevelDurationEditor);
            r.anchorMax = new Vector2(1, (EditorTriggerTime + LevelEditor.EditorHoldDelay) / LevelEditor.LevelDurationEditor);
        }
    }

    [System.Serializable]
    public class ScrollSpeedContainer: CommandContainer
    {
        public float NewSpeed;
        public float LerpTime;
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

            RectTransform ScrollSpeedRect = obj.GetComponent<RectTransform>();
            ScrollSpeedRect.offsetMin = ScrollSpeedRect.offsetMax = Vector2.zero;
            ScrollSpeedRect.anchorMin = new Vector2(0, EditorTriggerTime / LevelEditor.LevelDurationEditor);
            ScrollSpeedRect.anchorMax = new Vector2(1, (EditorTriggerTime + Mathf.Max(LerpTime, .5f)) / LevelEditor.LevelDurationEditor);
        }

        public override void DragTimeline(PointerEventData data)
        {
            base.DragTimeline(data);
            LevelEditor.RebuildScrollSpeedCache();

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
