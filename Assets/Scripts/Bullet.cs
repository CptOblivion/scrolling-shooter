using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    /*
     * NOTES
     * don't forget to set your bullet layer!
     * (maybe this should be done in the script)
     * playerBullets only collides with enemies and vice versa
     * (the player and enemy layers can collide with each other though)
     * 
     * TO DO:
     * split health of all objects off into separate script so it's easy to check for and reference!
     */

    Transform tf;

    [Tooltip("object to spawn when hitting a target")]
    public GameObject hitEffect;

    [Tooltip("Object to spawn when hitting a target but not dealing damage")]
    public GameObject PingEffect;

    [Tooltip("initial speed")]
    public float speed = 20;

    [Tooltip("damage on a hit")]
    public int damage = 1;

    [Tooltip("number of seconds between shots (player only)")]
    public float fireDelay = .15f;
    
    public float acceleration = 0;

    [Tooltip("if true, pass through players and hit enemies. If false, ignore enemies and hit players.")]
    public bool playerShot = false;

    public bool DieOnHit = true;

    public bool ContinueSoundAfterDeath = false;

    void Start()
    {
        tf = GetComponent<Transform>();
        AudioSource audioSource = GetComponent<AudioSource>();

        if (playerShot == true) this.gameObject.layer = 11; //set layer to PlayerBullets
        else this.gameObject.layer = 12; //set layer to EnemyBullets

        tf.position = GlobalTools.PixelSnap(tf.position);

        tf.position = new Vector3(tf.position.x, tf.position.y, 0); //bullets always exist on the gameplay plane
        if (ContinueSoundAfterDeath && audioSource)
        {
            AudioSource tempAudioSource = GlobalTools.PlaySound(audioSource.clip);
            tempAudioSource.volume = audioSource.volume;
            audioSource.enabled = false;
        }
    }
    
    void Update()
    {
        tf.Translate(0, speed * Time.deltaTime, 0);
        tf.GetChild(0).position = GlobalTools.PixelSnap(tf.position);

        speed += acceleration * Time.deltaTime;

        if (!GlobalTools.CheckVisibility(this.gameObject)) Destroy(this.gameObject); //if offscreen, destroy (this method factors in children and shadowcasting)
    }
    
    void OnCollisionEnter2D (Collision2D collision)
    {
        bool DamageDealt;
        HealthManager healthManager = collision.gameObject.GetComponent<HealthManager>();
        if (healthManager)
        {
            DamageDealt = healthManager.Damage(damage);
        }
        else DamageDealt = false;


        if (DamageDealt && hitEffect) Instantiate(hitEffect, tf.position, Quaternion.identity);
        else if (PingEffect) Instantiate(PingEffect, tf.position, Quaternion.identity);
        Destroy(this.gameObject);
    }
}
