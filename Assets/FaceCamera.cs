using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    public enum Axis {x, y, z};
    public Axis axis = Axis.y;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 alignAxis;
        if (axis == Axis.x)
        {
            alignAxis = this.transform.right;
            this.transform.right = new Vector3(alignAxis.x, alignAxis.y, 0);
        }
        else if (axis == Axis.y)
        {
            alignAxis = this.transform.up;
            this.transform.up = new Vector3(alignAxis.x, alignAxis.y, 0);
        }
        else
        {
            alignAxis = this.transform.forward;
            this.transform.forward = new Vector3(alignAxis.x, alignAxis.y, 0);
        }
    }
}
