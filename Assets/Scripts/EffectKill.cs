using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EffectKill : MonoBehaviour
{
    [Tooltip("Keyframe this to True and the object will tidy itself up")]
    public bool Done;
    public float life = 0;
    bool CountDown = false;
    // Start is called before the first frame update
    void Start()
    {
        if (life > 0) CountDown = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (CountDown && life <= 0) Done = true;
        life -= Time.deltaTime;
        if (Done) Destroy(this.gameObject);
    }
}
