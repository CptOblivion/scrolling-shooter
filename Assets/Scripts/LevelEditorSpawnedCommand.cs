
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelEditorSpawnedCommand : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public class SpawnedObjectContainer
    {
        public GameObject obj;
        public Vector3 StartPosition;
        public float TriggerTime;
        public float TriggerPosition;
        public float TriggerOffsetTime = 0;
        public float Life;
        public Animation anim = null;
        public EnemyPath path = null;
        public bool parallaxScroll = false;
        public int CommandIndex;
        public int ParentSpawnedObject;
        public SpawnedObjectContainer[] SpawnerChildren;
        public UILine line = null;
        public Color baseColor;
        public LevelEditorCommandDragControl ControlSpawnTime = null;
        public LevelEditorCommandDragControl ControlSpawnLocation = null;
        public bool Hovered { get; private set; } = false;
        public bool Selected { get; private set; } = false;

        public SpawnedObjectContainer(GameObject gameObject, int commandIndex, Vector3 startPosition, float triggerTime, float triggerPos)
        {
            CommandIndex = commandIndex;
            InstantiateBase(gameObject, startPosition, triggerTime, triggerPos);
        }

        public SpawnedObjectContainer(GameObject gameObject, Vector3 startPosition, float triggerTime, float triggerPos)
        {
            InstantiateBase(gameObject, startPosition, triggerTime, triggerPos);
        }

        private void InstantiateBase(GameObject gameObject, Vector3 startPosition, float triggerTime, float triggerPos)
        {
            obj = gameObject;
            StartPosition = startPosition;
            TriggerTime = triggerTime;
            TriggerPosition = triggerPos;
            Life = -1;
            ParentSpawnedObject = -1;
        }
        public void HoverEnter()
        {
            if (!Selected)
            {
                if (line)
                {
                    line.SetColor(LevelEditor.current.CommandLineSelectedColor);
                    line.SetWidth(LevelEditor.current.UILineHoveredWidth);
                    line.transform.SetAsLastSibling();
                    if (line.tail)
                        line.tail.transform.SetAsLastSibling();
                }
                if (SpawnerChildren != null)
                {
                    foreach (SpawnedObjectContainer child in SpawnerChildren)
                    {
                        if (child.line)
                        {
                            child.line.SetColor(LevelEditor.current.CommandLineSelectedColor);
                            child.line.SetWidth(LevelEditor.current.UILineHoveredWidth);
                            child.line.transform.SetAsLastSibling();
                            if (child.line.tail)
                                child.line.tail.transform.SetAsLastSibling();
                        }
                    }
                }
            }
        }
        public void HoverExit()
        {
            if (!Selected)
            {
                if (line)
                {
                    line.SetColor(baseColor);
                    line.SetWidth(LevelEditor.current.UILineWidth);
                }
                if (SpawnerChildren != null)
                {
                    foreach (SpawnedObjectContainer child in SpawnerChildren)
                    {
                        if (child.line)
                        {
                            child.line.SetColor(child.baseColor);
                            child.line.SetWidth(LevelEditor.current.UILineWidth);
                        }
                    }
                }
            }
        }

        public void Select()
        {
            Selected = true;
            if (ControlSpawnTime)
            {
                ControlSpawnTime.SetActive(true);
            }
            if (!Hovered)
                HoverEnter();
        }
        public void Deselect()
        {
            Selected = false;
            if (ControlSpawnTime)
            {
                ControlSpawnTime.SetActive(false);
            }
            if (!Hovered)
                HoverExit();
            if (line)
            {
                line.SetActive(line.gameObject.activeInHierarchy);
            }
        }
    }

    public SpawnedObjectContainer command;

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
