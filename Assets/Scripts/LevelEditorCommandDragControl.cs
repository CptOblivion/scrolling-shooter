using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelEditorCommandDragControl : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum Axes { X, Y, XY}
    public enum ControlInputs { SpawnTime, SpawnLocation}

    new public Collider2D collider;
    public Axes DragAxis = Axes.Y;
    public ControlInputs ControlInput = ControlInputs.SpawnTime;
    Vector3 OldLocation = Vector2.zero;
    public LevelEditorSpawnedCommand.SpawnedObjectContainer command = null;
    private void Awake()
    {
        collider = gameObject.AddComponent<BoxCollider2D>();
        //TODO: make collider bigger than line tail on Y axis
    }
    public void SetActive(bool active)
    {
        enabled = active;
        collider.enabled = active;
    }

    public void OnBeginDrag(PointerEventData data)
    {
        switch (ControlInput)
        {
            case ControlInputs.SpawnTime:
                OldLocation = new Vector2(0, command.TriggerTime);
                break;
            case ControlInputs.SpawnLocation:
                OldLocation = command.StartPosition;
                break;
        }
    }
    public void OnDrag(PointerEventData data)
    {
        //TODO: abort drag (reset to OldLocation, don't save undo state) if esc or right mouse is hit
        //TODO: pass along data to OnDrag for all selected commands (drag as group)

        //scale delta to be relative to game window viewport
        switch (ControlInput)
        {
            case ControlInputs.SpawnTime:
                //TODO: scale delta to match life time of level
                float TempDragScale = 0.1f;

                //TODO: this should probably all go into a function
                //TODO: account for holds in timeline
                command.TriggerTime = Mathf.Clamp(command.TriggerTime+data.delta.y*TempDragScale, 0, LevelEditor.LevelDuration);
                command.TriggerPosition = LevelEditor.GetDistanceTraveledAtTime(command.TriggerTime);
                
                LevelEditor.DetermineCommandLifespan(command);

                if (command.SpawnerChildren != null)
                {
                    foreach(LevelEditorSpawnedCommand.SpawnedObjectContainer childContainer in command.SpawnerChildren)
                    {
                        childContainer.TriggerTime = command.TriggerTime + childContainer.TriggerOffsetTime;
                        childContainer.TriggerPosition = LevelEditor.GetDistanceTraveledAtTime(childContainer.TriggerTime);
                        LevelEditor.DetermineCommandLifespan(childContainer);
                    }
                }
                LevelEditor.UpdateCommand(command);
                break;
            case ControlInputs.SpawnLocation:
                //filter X or Y depending on animation/path (if neither, XY)
                //change origin location
                //clamp to screen? (Or indicate offscreen somehow)
                //update object
                break;
        }
    }
    public void OnEndDrag(PointerEventData data)
    {
        //TODO: add to undo buffer
        switch (ControlInput)
        {
            case ControlInputs.SpawnTime:
                break;
            case ControlInputs.SpawnLocation:
                break;
        }
    }
}
