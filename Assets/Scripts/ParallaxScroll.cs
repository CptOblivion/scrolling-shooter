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
        tf.position = tf.position + Scroll(tf.position.z, LevelController.current.ScrollSpeed);
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

    public static Vector3 Scroll(float zOffset, float scrollSpeed)
    {
        /*
         * this should be used in LateUpdate to make sure the camera has already done its moving this frame
         */

        float depthScale = 1.15f;//the closer this is to 1, the "narrower" the field of view (parallax effect is weaker)
        //at depthScale 2, a depth of 5 is functionally infinitely far away (scrolling speed is ~0.03 of depth 0 speed)
        //reccommend depthScale 1.15
        float parallaxScale = Mathf.Pow(depthScale, -zOffset);
        Vector3 parallaxOffset = new Vector3(0, -scrollSpeed * Time.deltaTime * parallaxScale, 0);
        return parallaxOffset;
    }

    //TODO: move position position update into here, with a version that doesn't rely on GlobalTools.ParallaxScroll
    //so we can call it from the editor
    public void UpdatePosition()
    {

    }
}
