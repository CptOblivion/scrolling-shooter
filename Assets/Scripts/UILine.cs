using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILine : MonoBehaviour
{
    Vector3 OriginPosition;
    Vector3 TargetPosition;
    Transform OriginObject = null;
    Transform TargetObject = null;
    float LineWidth;
    public RawImage image;

    RawImage tail;
    float tailLength = 0;
    public static UILine NewLine(Vector3 start, Vector3 end, float width, RectTransform parent)
    {
        return InitializeBase(width, parent, null, null, start, end);
    }

    public static UILine NewLine(Vector3 start, Transform target, float width, RectTransform parent)
    {
        return InitializeBase(width, parent, null, target, start, Vector3.zero);
    }
    public static UILine NewLine(Transform startObject, Transform target, float width, RectTransform parent)
    {
        return InitializeBase(width, parent, startObject, target, Vector3.zero, Vector3.zero);
    }
    public static UILine NewLine(Transform startObject, Vector3 end, float width, RectTransform parent)
    {
        return InitializeBase(width, parent, startObject, null, Vector3.zero, end);
    }
    public static UILine NewLine(float width, RectTransform parent)
    {
        return InitializeBase(width, parent, null, null, Vector3.zero, Vector3.zero);
    }

    static UILine InitializeBase(float width, RectTransform parent, Transform OriginObject, Transform TargetObject, Vector3 OriginPosition, Vector3 TargetPosition)
    {
        GameObject go = new GameObject("UI Line");
        go.transform.parent = parent;
        UILine newLine = go.AddComponent<UILine>();
        newLine.OriginObject = OriginObject;
        newLine.TargetObject = TargetObject;
        newLine.OriginPosition = OriginPosition;
        newLine.TargetPosition = TargetPosition;
        newLine.image = go.AddComponent<RawImage>();
        RectTransform t = (RectTransform)go.transform;
        t.pivot = new Vector2(.5f, 0);
        t.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1);
        t.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        newLine.SetWidth(width);
        newLine.UpdateLine();
        return newLine;
    }

    public void SetColor(Color color)
    {
        image.color = color;
        if (tail)
        {
            tail.color = color;
        }
    }
    public void SetWidth(float w)
    {
        //TODO: factor in pixel size
        LineWidth = w;
        ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, LineWidth);
        if (tail)
            ((RectTransform)tail.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, LineWidth);

        UpdateLine();
    }

    public void UpdateLine()
    {
        float depth = LevelEditor.current.transform.position.z + 1;
        float ScaleFactor = transform.GetComponentInParent<Canvas>().transform.lossyScale.x;
        if (OriginObject)
            OriginPosition = OriginObject.position;
        if (TargetObject)
            TargetPosition = TargetObject.position;

        Vector2 vec = TargetPosition - OriginPosition;
        if (tail)
        {
            vec -= new Vector2(tailLength, 0);
            tail.transform.position = new Vector3(TargetPosition.x, TargetPosition.y, depth);
            tail.transform.localScale = new Vector3(1, tailLength / ScaleFactor, 1);
        }
        if (vec.magnitude > 0)
        {
            transform.position = new Vector3(OriginPosition.x, OriginPosition.y, depth);
            transform.rotation = Quaternion.LookRotation(vec, Vector3.forward);
            transform.Rotate(new Vector3(90, 0,0), Space.Self);
        }
        //TODO: length should factor in canvas scale


        transform.localScale = new Vector3(1,vec.magnitude/ScaleFactor);
    }

    public void SetTail(float length)
    {
        if (!tail)
        {
            tail = new GameObject($"{gameObject.name}.tail").AddComponent<RawImage>();
            tail.transform.SetParent(transform.parent);
            tail.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 90));
            tail.color = image.color;
            RectTransform t = (RectTransform)tail.transform;
            t.pivot = new Vector2(.5f, 0);
            t.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1);
            t.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, LineWidth);
        }
        tailLength = length;
        UpdateLine();

    }

    public void UpdateLine(Vector3 origin, Vector3 target)
    {
        OriginPosition = origin;
        TargetPosition = target;
        UpdateLine();
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
        if (tail)
            tail.gameObject.SetActive(active);
    }
}
