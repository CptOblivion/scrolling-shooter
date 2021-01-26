
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelEditorSpawnedCommand : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public class SpawnedObjectContainer
    {
        public GameObject obj;
        public Vector3 StartPosition;
        public float TriggerTime;
        public float TriggerPosition;
        public float Life;
        public Animation anim = null;
        public EnemyPath path = null;
        public bool parallaxScroll = false;
        public int CommandIndex;
        public int ParentSpawnedObject;
        public SpawnedObjectContainer[] SpawnerChildren;
        public UILine line = null;
        public Color baseColor;

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
            if (line)
            {
                line.SetColor(LevelEditor.current.CommandLineSelectedColor);
                line.SetWidth(LevelEditor.current.UILineHoveredWidth);
                line.transform.SetAsLastSibling();
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
                    }
                }
            }
    }
        public void HoverExit()
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

        public void Selected()
        {

        }
        public void Deselected()
        {

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
        command.HoverEnter();
    }
    public void OnPointerExit(PointerEventData data)
    {
        command.HoverExit();
    }


}
