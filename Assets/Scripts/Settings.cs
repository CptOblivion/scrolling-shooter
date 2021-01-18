using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
public class Settings : MonoBehaviour
{
    public AudioMixer mixer;
    public Camera uiCam;
    Canvas settingsCanvas;
    public GlobalTools.ScalingModeTypes StartingScalingMode;

    public Slider[] VolSliders;
    public Text labelScalingMode;

    public int FrameRate = 60;

    private void Awake()
    {
        if (uiCam == null) Debug.LogError("No UI Camera Selected!");
        settingsCanvas = GetComponent<Canvas>();

    }
    void Start()
    {
        GlobalTools.TargetWidth = 640;
        GlobalTools.TargetHeight = 480;
        GlobalTools.ScalingMode = StartingScalingMode;
        SetFrameRate(FrameRate);
        GlobalTools.globalTools.UpdateScalingMode();
    }

    private void OnEnable()
    {
        //set volume sliders to their current values
        float value;
        string[] volGroups = new string[] { "VolMaster", "VolMusic", "VolSFX" };
        for(int i = 0; i<3; i++)
        {
            mixer.GetFloat(volGroups[i], out value);
            value = Mathf.Pow(10, value/20);
            VolSliders[i].value = value;
        }
        labelScalingMode.text = GlobalTools.ScalingModeNames[(int)GlobalTools.ScalingMode];
    }
    public void SetScalingMode(int scalingMode)
    {
        int scalingModeLength = System.Enum.GetValues(typeof(GlobalTools.ScalingModeTypes)).Length;
        if (scalingMode >= scalingModeLength) Debug.Log("Invalid scaling mode!");
        else GlobalTools.ScalingMode = (GlobalTools.ScalingModeTypes)scalingMode;
        GlobalTools.globalTools.UpdateScalingMode();
        labelScalingMode.text = GlobalTools.ScalingModeNames[(int)GlobalTools.ScalingMode];
    }

    public void CycleScalingMode(bool up = true)
    {
        int scalingModeLength = System.Enum.GetValues(typeof(GlobalTools.ScalingModeTypes)).Length;
        if (up)
        {
            GlobalTools.ScalingMode++;
            if ((int)GlobalTools.ScalingMode == scalingModeLength) GlobalTools.ScalingMode = (GlobalTools.ScalingModeTypes)0;
        }
        else
        {
            if ((int)GlobalTools.ScalingMode == 0) GlobalTools.ScalingMode = (GlobalTools.ScalingModeTypes)scalingModeLength;
            GlobalTools.ScalingMode--;
        }
        GlobalTools.globalTools.UpdateScalingMode();
        labelScalingMode.text = GlobalTools.ScalingModeNames[(int)GlobalTools.ScalingMode];
    }
    public void SetFrameRate(int frameRate)
    {
        Application.targetFrameRate = frameRate;
    }

    public void OnCloseSettings(InputAction.CallbackContext context)
    {
        CloseSettings();
    }
    public void CloseSettings()
    {
        settingsCanvas.gameObject.SetActive(false);
    }

    float VolumeCurve(float volume)
    {
        volume = Mathf.Clamp(volume, .0001f, 1);

        volume = Mathf.Log10(volume) * 20;
        return volume;
    }
    public void SetVolMain(float volume)
    {
        mixer.SetFloat("VolMaster",VolumeCurve(volume));
    }
    public void SetVolMusic(float volume)
    {
        mixer.SetFloat("VolMusic", VolumeCurve(volume));

    }
    public void SetVolSFX(float volume)
    {
        mixer.SetFloat("VolSFX", VolumeCurve(volume));

    }
}
