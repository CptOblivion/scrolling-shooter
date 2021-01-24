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

    static float depthScale = 1.15f;//the closer this is to 1, the "narrower" the field of view (parallax effect is weaker)
                             //at depthScale 2, a depth of 5 is functionally infinitely far away (scrolling speed is ~0.03 of depth 0 speed)
                             //reccommend depthScale 1.15
                             // Start is called before the first frame update
    void Start()
    {
        tf = GetComponent<Transform>();
        GraphicsTf = tf.GetChild(0);
    }

    void Update()
    {
        if (!GlobalTools.CheckIfPlaying(this)) return; //abort if we're spawned in a menu or the editor
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

        float parallaxScale = Mathf.Pow(depthScale, -zOffset);
        Vector3 parallaxOffset = new Vector3(0, -scrollSpeed * Time.deltaTime * parallaxScale, 0);
        return parallaxOffset;
    }

    public static Vector3 ScrollAbsolute(Vector3 StartPosition, float travelDistance)
    {

        float parallaxScale = Mathf.Pow(depthScale, -StartPosition.z);
        return new Vector3(0, -travelDistance * parallaxScale, 0) + StartPosition;
    }

    /// <summary>
    /// determines how far the camera must travel before an object at a given depth will scroll offscreen and despawn
    /// </summary>
    /// <param name="TopEdge"> Y coordinate of the top edge of the object's bounding box</param>
    /// <param name="zOffset"> vertical starting position</param>
    /// <returns></returns>
    public static float DetermineLife(float TopEdge, float zOffset)
    {
        //TODO: this
        //we're just outputting the distance here, this needs to then be converted into time with potential travel speed changes factored in

        return 0;
    }
}
