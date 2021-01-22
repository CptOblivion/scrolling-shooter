using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HealthManager))]
public class EnemyGeneric : MonoBehaviour
{
    /*
     * NOTES
     * movement should be handled with an animation (this script expects there to be one present)
     * 
     * TO DO:
     * trigger start (for when scrolling happens, starts when camera reaches certain point)
     * alternate shooting mode where shots are timed out on animation
     */

    Transform tf;
    HealthManager healthManager;
    Animation anim;

    [Tooltip("if true, exists as a background scenery element that can't crash into the player")]
    public bool Scenery = false;

    Transform GraphicsTf;

    [Tooltip("spawns this upon being killed (but not when despawning naturally)")]
    public GameObject DeathEffect;

    [Tooltip("number of seconds after spawning before this can take damage")]
    public float HealthDelay = 0;

    [Tooltip("time in seconds to wait before moving and beginning fireDelay")]
    public float SpawnDelay = 0;
    int lastHealth;

    public Animation[] DamageAnimations;

    //weapons stuff
    [Tooltip("for now, just drag and drop the bullet prefab into this slot")]
    public GameObject ShotType;

    [Tooltip("noumber of seconds before first shot")]
    public float FireStartup = 0;

    [Tooltip("Number of seconds between shots")]
    public float FireDelay = 1;
    private float fireDelay = 0;

    List<Transform> guns; //list of attached guns

    [Tooltip("amount of damage this enemy takes when it hits something")]
    public int CrashDamage = 0;

    public float AnimSpeed = 1;

    [Tooltip("in the animation, keyframe this to true and the enemy will clean itself up")]
    public bool animDone = false; //

    [Tooltip("Drop in all the animation components that should play when shooting")]
    public Animation[] FiringAnims;

    [Tooltip("Spawn this as an effect when shooting")]
    public GameObject FiringEffect;

    [Tooltip("The effect will spawn when fireDelay counter reaches this number, allowing \" shot charging \" effects")]
    public float FireEffectOffset = 0;

    bool FireEffectDone = false;

    // Start is called before the first frame update
    void Start()
    {
        tf = GetComponent<Transform>();
        anim = GetComponent<Animation>();
        healthManager = GetComponent<HealthManager>();
        GraphicsTf = tf.GetChild(0);

        lastHealth = healthManager.Health;

        fireDelay = FireStartup;
        if (HealthDelay > 0) healthManager.AcceptingDamage = false;

        if (Scenery) this.gameObject.layer = 13; //set layer to EnemyScenery
        else this.gameObject.layer = 10; //set layer to Enemy

        if (anim != null)
        {
            if (SpawnDelay > 0) foreach (AnimationState state in anim) state.speed = 0; //stop animation until spawncounter is done
            else foreach (AnimationState state in anim) state.speed = AnimSpeed;
        }

        //populate a list of all the guns attached
        guns = new List<Transform>();
        foreach (Gun child in GetComponentsInChildren<Gun>())
        {
            guns.Add(child.gameObject.transform);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (SpawnDelay > 0) //waiting our turn to spawn
        {
            SpawnDelay-= Time.deltaTime;
            if (SpawnDelay <= 0)
            {
                foreach (AnimationState state in anim) state.speed = AnimSpeed; //start animations
            }
        }

        else //most of the stuff goes in here (done waiting to spawn from here on out)
        {
            if (HealthDelay > 0)
            {
                HealthDelay -= Time.deltaTime;
                if (HealthDelay <= 0) healthManager.AcceptingDamage = true;
            }
            else if (healthManager.Health <= 0)
            {
                if (DeathEffect != null) Instantiate(DeathEffect, tf.position, Quaternion.identity);
                SendMessage("Death");
            }
            else if (healthManager.Health < lastHealth)
            {
                lastHealth = healthManager.Health;
                if (DamageAnimations != null)
                {
                    foreach (Animation anim in DamageAnimations)
                    {
                        anim[anim.clip.name].normalizedTime = 0;
                        anim.Play();
                    }
                }
            }



            if (fireDelay <= FireEffectOffset && !FireEffectDone) //allow the firing effect to spawn before the shot, to give enemies a tell
            {
                foreach (Transform gun in guns)
                {
                    if (FiringEffect != null)
                    {
                        GameObject firingEffect;
                        firingEffect = Instantiate(FiringEffect, FiringEffect.transform.position, FiringEffect.transform.rotation, FiringEffect.transform.parent);
                        firingEffect.SetActive(true); //just in case
                    }

                    for (int i = 0; i < FiringAnims.Length; i++)
                    {
                        FiringAnims[i].Play();
                    }
                    FireEffectDone = true;

                }
            }
            if (fireDelay > 0) fireDelay-= Time.deltaTime; //reloading...
            else //shootin'
            {
                foreach (Transform gun in guns)
                {
                    Vector3 gunRotation = gun.eulerAngles;
                    Quaternion bulletRotation = Quaternion.Euler(new Vector3(0, 0, gunRotation.z));

                    //if we have a parent (probably the camera, or a spawner attached to the camera), parent the bullets to that too
                    if (tf.parent != null) Instantiate(ShotType, gun.position, bulletRotation, tf.parent);//spawn a bullet at each gun, parented
                    else Instantiate(ShotType, gun.position, bulletRotation);//spawn a bullet at each gun
                }
                fireDelay = FireDelay;
                FireEffectDone = false;
            }

            if (animDone == true)
            {
                Death();
            }
        }

    }

    private void LateUpdate()
    {
        GraphicsTf.position = GlobalTools.PixelSnap(tf.position);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HealthManager targetHealthManager = collision.gameObject.GetComponent<HealthManager>();
        if (targetHealthManager)
        {
            targetHealthManager.Damage(1);
            healthManager.Damage(CrashDamage);
        }
    }

    public void SetHealthDelay(float delay)
    {
        HealthDelay = delay;
    }

    public void SetFireStartup(float delay)
    {
        FireStartup = delay;
    }
    void Death()
    {
        if (tf.parent && tf.parent.name == this.name + "AnimParent") Destroy(tf.parent.gameObject);
        else Destroy(tf.gameObject);
    }
}
