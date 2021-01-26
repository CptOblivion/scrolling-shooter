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
    public static UILine NewLine(Vector3 start, Vector3 end, float width)
    {
        return InitializeBase(width, null, null, start, end);
    }

    public static UILine NewLine(Vector3 start, Transform target, float width)
    {
        return InitializeBase(width, null, target, start, Vector3.zero);
    }
    public static UILine NewLine(Transform startObject, Transform target, float width)
    {
        return InitializeBase(width, startObject, target, Vector3.zero, Vector3.zero);
    }
    public static UILine NewLine(Transform startObject, Vector3 end, float width)
    {
        return InitializeBase(width, startObject, null, Vector3.zero, end);
    }
    public static UILine NewLine(float width)
    {
        return InitializeBase(width, null, null, Vector3.zero, Vector3.zero);
    }

    static UILine InitializeBase(float width, Transform OriginObject, Transform TargetObject, Vector3 OriginPosition, Vector3 TargetPosition)
    {
        GameObject go = new GameObject();
        UILine newLine = go.AddComponent<UILine>();
        newLine.OriginObject = OriginObject;
        newLine.TargetObject = TargetObject;
        newLine.OriginPosition = OriginPosition;
        newLine.TargetPosition = TargetPosition;
        newLine.image = go.AddComponent<RawImage>();
        RectTransform t = (RectTransform)go.transform;
        t.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1);
        t.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        newLine.UpdateLine();
        return newLine;
    }

    public void SetColor(Color color)
    {
        image.color = color;
    }
    public void SetWidth(float w)
    {
        LineWidth = w;
        UpdateLine();
    }

    public void UpdateLine()
    {
        if (OriginObject)
            OriginPosition = OriginObject.position;
        if (TargetObject)
            TargetPosition = TargetObject.position;

        Vector2 vec = TargetPosition - OriginPosition;
        if (vec.magnitude > 0)
        {
            transform.position = OriginPosition;
            transform.rotation = Quaternion.LookRotation(vec, Vector3.forward);
        }
        transform.localScale = new Vector3(1,vec.magnitude);
    }

    public void UpdateLine(Vector3 origin, Vector3 target)
    {
        OriginPosition = origin;
        OriginPosition = target;
        UpdateLine();
    }
}
