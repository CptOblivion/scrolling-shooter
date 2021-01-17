using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;

public class FaceCamera : MonoBehaviour
{
    public enum Axis { X, Y, Z, NegX, NegY, NegZ };
    public Axis axis = Axis.Z;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (axis == Axis.X) this.transform.right = Vector3.forward;
        else if (axis == Axis.Y) this.transform.up = Vector3.forward;
        else if (axis == Axis.Z) this.transform.forward = Vector3.forward;
        else if (axis == Axis.NegX) this.transform.right = -Vector3.forward;
        else if (axis == Axis.NegY) this.transform.up = -Vector3.forward;
        else this.transform.forward = -Vector3.forward;
    }
}
