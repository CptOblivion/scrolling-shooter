using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class displayImage : MonoBehaviour
{
    Camera rendercam;
    void Start()
    {
        rendercam = GameObject.FindObjectOfType<LevelController>().gameObject.GetComponent<Camera>();
    }
    void OnPostRender()
    {
        float width = Screen.width;
        float height = Screen.height;
        float ratio = height / width;
        float BorderX, BorderY;

        if (ratio < .75)
        {
            BorderX = width - (height / .75f);
            BorderY = 0;
        }
        else
        {
            BorderX = 0;
            BorderY = width / (4 / 3);
        }

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, width, height, 0);
        Graphics.DrawTexture(new Rect(BorderX / 2, BorderY / 2, width - BorderX, height - BorderY), rendercam.targetTexture);

        GL.PopMatrix();
    }
}
