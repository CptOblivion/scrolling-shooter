using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffect : MonoBehaviour
{
    [Tooltip("if true, use parallax scroll, if false follow camera")]
    public bool parallax = true;
    [Tooltip("Number of seconds to persist")]
    public float Life = 1f;
    private float LifeTimer;

    Transform tf;
    // Start is called before the first frame update
    void Start()
    {
        LifeTimer = Life;
        tf = GetComponent<Transform>();
        tf.position = GlobalTools.PixelSnap(tf.position);
        
    }

    // Update is called once per frame
    void Update()
    {
        if (LifeTimer <= 0) Destroy(this.gameObject);
        LifeTimer -= Time.deltaTime;

        float lifeNormal = 1 - (LifeTimer / Life);
        
        foreach (Animation anim in GetComponentsInChildren<Animation>())
        {
            foreach (AnimationState state in anim)
            {
                state.normalizedTime = lifeNormal;
            }
        }

        if (parallax)
        {
            tf.position = tf.position + ParallaxScroll.Scroll(tf.position.z, LevelController.current.ScrollSpeed);

            if (tf.childCount == 1 && tf.GetChild(0).name == tf.gameObject.name)//if we have one child with the same name as us, we're a container object
            {
                Transform childTf = tf.GetChild(0).GetComponent<Transform>();
                childTf.position = GlobalTools.PixelSnap(tf.position);
            }
            else tf.position = GlobalTools.PixelSnap(tf.position);

        }
    }
}
