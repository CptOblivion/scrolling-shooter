using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    CanvasScaler canvasScaler;
    public Camera uiCam;

    public int FrameRate = 60;
    public enum ScalingModeTypes { FitToScreen, NearestPixelMultiple}
    public ScalingModeTypes ScalingMode;
    float TargetWidth; //hook for changing the pixelart resolution, just in case
    float TargetHeight;


    // Start is called before the first frame update
    void Start()
    {
        TargetWidth = 640;
        TargetHeight = 480;
        canvasScaler = GetComponent<CanvasScaler>();
        SetFrameRate(FrameRate);
        UpdateScalingMode();
    }

    void Update()
    {
        if (ScalingMode == ScalingModeTypes.NearestPixelMultiple) UpdateScalingMode(); //in case of window resizes
    }

    public void UpdateScalingMode()
    {
        if(ScalingMode == ScalingModeTypes.FitToScreen)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            uiCam.orthographicSize = 24;
        }
        else
        {
            float ScaleReference;
            float ScreenDimension;
            float ScreenRatio = (float)Screen.height / (float)Screen.width;
            float TargetRatio = TargetHeight / TargetWidth;
            if (ScreenRatio < TargetRatio)
            {
                ScaleReference = TargetHeight;
                ScreenDimension = Screen.height - (Screen.height % ScaleReference);
            }
            else
            {
                ScaleReference = TargetWidth;
                ScreenDimension = Screen.width - (Screen.width % ScaleReference);
            }

            float scaleFactor = ScreenDimension/ScaleReference;

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasScaler.scaleFactor = scaleFactor;
            //don't forget to scale the UI camera inversely to the game render size
            uiCam.orthographicSize = ((float)Screen.height / (TargetHeight * scaleFactor)) * 24;
        }
    }

    public void SetScalingMode(int scalingMode)
    {
        int scalingModeLength = System.Enum.GetValues(typeof(ScalingModeTypes)).Length;
        if (scalingMode >= scalingModeLength) Debug.Log("Invalid scaling mode!");
        else ScalingMode = (ScalingModeTypes)scalingMode;
        UpdateScalingMode();
    }

    public void CycleScalingMode(bool up = true)
    {
        int scalingModeLength = System.Enum.GetValues(typeof(ScalingModeTypes)).Length;
        if (up)
        {
            ScalingMode++;
            if ((int)ScalingMode == scalingModeLength) ScalingMode = (ScalingModeTypes)0;
        }
        else
        {
            if ((int)ScalingMode == 0) ScalingMode = (ScalingModeTypes)scalingModeLength;
            ScalingMode--;
        }
        UpdateScalingMode();
    }
    public void SetFrameRate(int frameRate)
    {
        Application.targetFrameRate = frameRate;
    }
}
