using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuActivation : MonoBehaviour
{
    Transform tf;
     EventSystem eventSystem;

    public GameObject DefaultMenuItem;
    
    // Start is called before the first frame update
    void Awake()
    {
        tf = this.transform;
        eventSystem = FindObjectOfType<EventSystem>();

    }

    void OnEnable()
    {
        if (DefaultMenuItem) eventSystem.SetSelectedGameObject(DefaultMenuItem);
        foreach (Button button in tf.GetComponentsInChildren<Button>())
        {
            
        }
    }
}
