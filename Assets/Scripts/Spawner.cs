using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Tooltip("object to spawn")]
    public GameObject spawn;
    [Tooltip("animation to use as path for spawned objects to follow")]
    public AnimationClip animPath;
    [Tooltip("if true, the spawned object will be parented to the spawner parent and exist in its local coordinate system")]
    public bool LocalCoordinates = true;

    [Tooltip("number of seconds before first spawn")]
    public float spawnDelay = 0;
    [Tooltip("number of seconds between each spawn")]
    public float spawnInterval = 1;
    private float spawnIntervalTimer; //keep track of the current interval progress
    [Tooltip("random offset for the interval (spawnInterval is the lower bound, spawnInterval+randomInterval is the upper bound")]
    public float randomInterval = 0;
    [Tooltip("if this is true, use ScrollSpeed * Time.deltaTime instead of just deltaTime")]
    public bool IntervalIsDistance = false;

    [Tooltip("number of objects to spawn before ending, 0 for endless")]
    public int spawnCount = 0;

    [Tooltip("random offset for the horizontal position (in either direction)")]
    public float randomX = 0;
    [Tooltip("random offset for the vertical position (in either direction)")]
    public float randomY = 0;


    void Start()
    {
        spawnIntervalTimer = 0;
    }

    void Update()
    {
        if (!GlobalTools.CheckIfPlaying(this)) return;
        if (spawnDelay > 0)
        {
            if (IntervalIsDistance) spawnDelay -= LevelController.current.ScrollSpeed * Time.deltaTime;
            else spawnDelay -= Time.deltaTime;
        }
        else
        {
            if (spawnIntervalTimer > 0)
            {
                if (IntervalIsDistance) spawnIntervalTimer -= LevelController.current.ScrollSpeed * Time.deltaTime;
                else spawnIntervalTimer -= Time.deltaTime;
            }
            else
            {
                GameObject spawnedOb;
                float xOffset = Random.Range(-randomX, randomX);
                float yOffset = Random.Range(-randomY, randomY);
                Vector3 spawnPos = new Vector3(transform.position.x + xOffset, transform.position.y + yOffset, transform.position.z);
                if (LocalCoordinates) spawnedOb = Instantiate(spawn, spawnPos, Quaternion.identity, transform);
                else spawnedOb = Instantiate(spawn, spawnPos, Quaternion.identity);

                if (animPath != null)//if we have an animation to assign to the spawned object
                {
                    //first we make an empty object to parent the spawned object to, otherwise the animation can override the spawned position
                    GameObject spawnParent = new GameObject(spawnedOb.name + "AnimParent");
                    Transform spawnParentTf = spawnParent.GetComponent<Transform>();
                    Transform spawnedObTf = spawnedOb.GetComponent<Transform>();

                    if (LocalCoordinates) spawnParentTf.parent = transform; //if the spawned object is parented to the spawner, pass it along to the spawned parent
                    spawnParentTf.position = spawnPos;
                    spawnParentTf.rotation = transform.rotation;
                    spawnParentTf.localScale = new Vector3(-1, 1, 1);//not sure why it's important to invert the X scale, but apparently it is.
                    spawnedObTf.parent = spawnParentTf;
                    //we don't need to bother setting location for the spawned object back to 0,0,0, 'cause the animation will override it anyways

                    //now we actually assign the animation
                    Animation anim = spawnedOb.GetComponent<Animation>();
                    anim.clip = animPath;
                    anim.AddClip(animPath, animPath.name);
                    anim.Play(animPath.name);
                }

                spawnIntervalTimer = spawnInterval + Random.Range(0, randomInterval);

                spawnCount--;
                if (spawnCount == 0) Destroy(this.gameObject);


            }
        }
    }
}
