﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class GlobalTools : MonoBehaviour
{
    public static GlobalTools globalTools;
    public static Camera renderCam;
    public static LevelController levelController;
    public static AudioMixer audioMixer;

    void Awake()
    {
        globalTools = this;
        renderCam = this.GetComponent<Camera>();
        levelController = this.GetComponent<LevelController>();

    }
    public static float PixelSize()
    {
        return renderCam.orthographicSize / renderCam.targetTexture.height * 2; //orthographicSize is half the height of the camera
    }
    
    public static Vector3 PixelSnap(Vector3 position)
    {
        /*
         * Snaps an object to pixel space
         * recommended to use an empty for analog movement, and then pixelsnap the child with the actual graphics
         * Try to use in LateUpdate whenever possible, in case any other movement gets applied to the object throughout the frame
         */
        if (renderCam.targetTexture)
        {
            float gridSize = PixelSize();
            float scale = 1 / gridSize;

            Vector3 pos = new Vector3((Mathf.Round(position.x * scale)) * gridSize,
                (Mathf.Round(position.y * scale)) * gridSize,
                (Mathf.Round(position.z * scale)) * gridSize);

            return pos;
        }
        else return position;
    }
    
    public static Vector3 ParallaxScroll(float zOffset)
    {
        /*
         * this should be used in LateUpdate to make sure the camera has already done its moving this frame
         */

        float scrollSpeed = levelController.ScrollSpeed;
        float depthScale = 1.15f;//the closer this is to 1, the "narrower" the field of view (parallax effect is weaker)
        //at depthScale 2, a depth of 5 is functionally infinitely far away (scrolling speed is ~0.03 of depth 0 speed)
        //reccommend depthScale 1.15
        float parallaxScale = Mathf.Pow(depthScale, -zOffset);
        Vector3 parallaxOffset = new Vector3(0, -scrollSpeed * Time.deltaTime * parallaxScale, 0);
        return parallaxOffset;
    }

    public static AudioSource PlaySound(AudioClip audioClip, float Pitch = 1)
    {
        GameObject tempOb = new GameObject("TempSoundObject");
        AudioSource audioSource = tempOb.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.pitch = Pitch;
        audioSource.Play();
        if (audioMixer) audioSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Sound Effects")[0];
        Destroy(tempOb, audioClip.length * (1 / Pitch));
        return audioSource;
    }

    public static bool CheckVisibility(GameObject ob)
    {
        bool visible = false;
        Renderer renderer = ob.GetComponent<Renderer>();
        if (renderer != null && CheckBoundsInLevel(renderer)) visible = true;
        else
        {
            foreach (Renderer childRenderer in  ob.GetComponentsInChildren<Renderer>())
            {
                if (CheckBoundsInLevel(childRenderer)) visible = true;
                break; //no need to keep iterating if we've found a part inside the level
            }
        }
        return visible;
    }

    public static bool CheckBoundsInLevel(Renderer renderer)
    {
        Vector3 min = renderer.bounds.min;
        Vector3 max = renderer.bounds.max;
        float borderMargin = 10;

        if (max.x + borderMargin < -levelController.LevelWidth / 2) return false;
        else if (max.y  + borderMargin < -renderCam.orthographicSize) return false;
        else if (min.x - borderMargin > levelController.LevelWidth / 2) return false;
        else if (min.y - borderMargin > renderCam.orthographicSize) return false;
        else return true;
    }

    public static void EndLevel(bool death)
    {
        //this will be a return to menu later, once there's a menu to return to
        //but for now it's:
        ExitToSystem();
    }

    public static void ExitToSystem()
    {
        //different quit methods depending on the runtime environment
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif

    }
}