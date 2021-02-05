using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelEditorCommandDragControl : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum Axes { X, Y, XY}
    public enum ControlInputs { SpawnTime, SpawnLocation}

    new public Collider2D collider;
    public Axes DragAxis = Axes.Y;
    public ControlInputs ControlInput = ControlInputs.SpawnTime;
    public LevelEditorSpawnedCommand.CommandContainer command = null;
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

    public void OnPointerDown(PointerEventData data)
    {
        command.PointerDown(data);
    }
    public void OnBeginDrag(PointerEventData data)
    {
        LevelEditorSpawnedCommand commandComponent = GetComponent<LevelEditorSpawnedCommand>();
        if (commandComponent)
        {
            commandComponent.Select();
        }
        switch (ControlInput)
        {
            case ControlInputs.SpawnTime:
                command.BeginDragTimeline(data);
                break;
            case ControlInputs.SpawnLocation:
                ((LevelEditorSpawnedCommand.LevelPositionalContainer)command).BeginDragPosition(data);
                break;
        }
    }
    public void OnDrag(PointerEventData data)
    {
        //TODO: maybe call a function in LevelEditorSpawnedCommand.CommandContainer and pass along data, so we can override it for different command types
        //TODO: abort drag (reset to OldLocation, don't save undo state) if esc or right mouse is hit
        //TODO: pass along data to OnDrag for all selected commands (drag as group)

        //TODO: scale delta to be relative to game window viewport/level duration

        switch (ControlInput)
        {
            case ControlInputs.SpawnTime:
                command.DragTimeline(data);
                break;
            case ControlInputs.SpawnLocation:
                ((LevelEditorSpawnedCommand.LevelPositionalContainer)command).DragPosition(data);
                break;
        }
    }
    public void OnEndDrag(PointerEventData data)
    {
        //TODO: add to undo buffer
        switch (ControlInput)
        {
            case ControlInputs.SpawnTime:
                command.EndDragTimeline(data);
                break;
            case ControlInputs.SpawnLocation:
                ((LevelEditorSpawnedCommand.LevelPositionalContainer)command).EndDragPosition(data);
                break;
        }
    }
}
