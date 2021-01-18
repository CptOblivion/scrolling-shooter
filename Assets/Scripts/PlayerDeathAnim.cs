using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeathAnim : MonoBehaviour
{
    public Transform ExplosionStart;
    public Animation FallAnim;
    public GameObject ExplosionEnd;
    public float GameEndTime = 4;

    bool falling = true;

    // Start is called before the first frame update
    void Start()
    {
        ExplosionStart.SetParent(null);
        ExplosionStart.gameObject.SetActive(true);
        ExplosionEnd.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!falling)
        {
            GameEndTime -= Time.deltaTime;
            if (GameEndTime <= 0)
            {

                GlobalTools.EndLevel(true);
            }
        }
        else if (!FallAnim.isPlaying)
        {
            falling = false;
            ExplosionEnd.SetActive(true);
            FallAnim.gameObject.SetActive(false);
        }
    }
}
