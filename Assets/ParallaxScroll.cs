using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxScroll : MonoBehaviour
{
    [Tooltip("destroy this object if it goes offscreen")]
    public bool KillOffScreen = true;
    Transform GraphicsTf;
    float killDelay = 1; //give the object a second to get onscreen after spawning before checking for offscreen
    Transform tf;
    // Start is called before the first frame update
    void Start()
    {
        tf = GetComponent<Transform>();
        GraphicsTf = tf.GetChild(0);
    }

    void Update()
    {
        tf.position = tf.position + GlobalTools.ParallaxScroll(tf.position.z);
        GraphicsTf.position = GlobalTools.PixelSnap(tf.position);

        
        if (KillOffScreen)
        {
            if (killDelay > 0) killDelay -= Time.deltaTime;
            else
            {
                if (!GlobalTools.CheckVisibility(this.gameObject)) Destroy(this.gameObject); //if offscreen, destroy (this method factors in children and shadowcasting)
            }
        }
    }
}
