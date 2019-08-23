using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(HealthManager))]
public class PlayerControl : MonoBehaviour
{
    /*
     * NOTES
     */

    Transform tf;
    HealthManager healthManager;

    //movement stuff
    [Tooltip("max movement speed")]
    public float moveSpeed = 20f;

    [Tooltip("number of frames to be invincible after a hit")]
    public float damageIframes = .05f;
    private float damageIframeTimer;
    private int flickerCounter;
    [Tooltip("pattern to flicker when hit. Each entry is a frame count between toggling visibility (starting out invisible)")]
    public int[] flickerPattern = new int[] {1, 2}; //1 is blink every frame, 2 is every other frame, etc
    private int flickerIndex = 0;
    private bool visible;

    //animation stuff

    float animThreshold = .2f;
    [Tooltip("the angle the plane tilts when \"turning\" (moving left/right)")]
    public float bankAngle;

    [Tooltip("drop in all the objects that should be animated as jet flames")]
    public GameObject[] jetFlames;

    [Tooltip("Size to scale the jet flames to when accelerating (moving up)")]
    public float jetScaleAccelerate = 1.5f;
    [Tooltip("Size to scale the jet flames to when decelerating (moving down)")]
    public float jetScaleDecelerate = .75f;

    private float[] jetScales; //keep the jet scales in list form
    private int jetState = 1; //keep track of the current jet state
    public float jetTransitionTime = .1f;
    private float jetTransitionTimer = 0; //and how long the 
    private float bankState = 0;//keep track of the current bank state
    public float bankTransitionTime = .1f;
    private float bankTransitionTimer = 0; //track the state of the bank animation last frame

    [Tooltip("Effect to spawn on each gun that's broken off when taking damge")]
    public GameObject GunBreakEffect;
    [Tooltip("Effect to spawn on each gun that's added when getting an upgrade")]
    public GameObject GunBuildEffect;

    public AnimationClip VictoryAnimation;

    //weapons stuff
    [Tooltip("for now, just drag and drop the bullet prefab into this slot")]
    public GameObject shotType;
    [Tooltip("")]
    public GameObject specialType;//see above

    private float fireDelayCounter, specialDelayCounter = 0;//counters to track corresponding properties
    List<Transform> CurrentGuns; //list of attached guns

    [Tooltip("The parent object for each level of upgrade should be dropped into this list")]
    public GameObject[] UpgradeList;

    [Tooltip("Tracks the current upgrade level of the plane")]
    public int UpgradeLevel = 1;

    public AudioClip UpgradeSound;

    [Tooltip("Amount of time after taking damage down to upgrade level 0 before returning to 1")]
    public float DangerTime = 4;
    private float DangerTimer;

    [Tooltip("Effect to spawn when one hit from death")]
    public GameObject EffectDanger;
    GameObject effectDanger; //the current instance of EffectDanger
    public GameObject EffectDeath;

    [Tooltip("drop in the root graphics object")]
    public Transform rootModel;
    
    void Start()
    {
        tf = GetComponent<Transform>();
        healthManager = GetComponent<HealthManager>();

        visible = true;
        this.gameObject.layer = 9; //set layer to Player

        jetScales = new float[] { jetScaleDecelerate, 1, jetScaleAccelerate };

        UpdateUpgrades(); 

    }


    void Update()
    {
        if (Time.timeScale > 0) //if we're not paused
        {
            InputActionMap gameplayActions = GlobalTools.levelController.actionMapGameplay;
            Vector2 inputMoveDir = gameplayActions.GetAction("Move").ReadValue<Vector2>();

            //controls stuff

            inputMoveDir = Vector3.ClampMagnitude(inputMoveDir, 1); //fix those diagonals (would just normalize but analog input is available)
            tf.Translate(inputMoveDir.x * Time.deltaTime * moveSpeed, inputMoveDir.y * Time.deltaTime * moveSpeed, 0);

            Vector3 positionConstrained = tf.position;
            float levelEdgeX = GlobalTools.levelController.LevelWidth / 2 - GlobalTools.levelController.PlayerWidth;
            if (positionConstrained.x > levelEdgeX) positionConstrained.x = levelEdgeX;
            else if (positionConstrained.x < -levelEdgeX) positionConstrained.x = -levelEdgeX;

            float playerHeight = GetComponent<CompositeCollider2D>().bounds.extents.y;
            float levelEdgeY1 = GlobalTools.renderCam.orthographicSize - playerHeight;
            float levelEdgeY2 = -GlobalTools.renderCam.orthographicSize + playerHeight;
            if (positionConstrained.y > levelEdgeY1) positionConstrained.y = levelEdgeY1;
            else if (positionConstrained.y < levelEdgeY2) positionConstrained.y = levelEdgeY2;

            tf.position = positionConstrained;

            //banking animation (moving left/right)
            int bankTarget = 0;
            if (inputMoveDir.x >= animThreshold) bankTarget = -1;
            else if (inputMoveDir.x <= -animThreshold) bankTarget = 1;

            if (bankTarget != bankState)
            {
                if (bankTransitionTimer > 0) bankTransitionTimer -= Time.deltaTime;
                else
                {
                    if (bankState != 0) bankState = 0;
                    else bankState = bankTarget;
                    bankTransitionTimer = bankTransitionTime;
                }
            }
            else bankTransitionTimer = bankTransitionTime;

            //bankState = bankAngle;//forced bank for debug

            rootModel.localEulerAngles = new Vector3(0, (bankState * bankAngle) + 180, 0);

            //jet flame animation (moving forward/back)
            int jetTarget = 1;
            if (inputMoveDir.y >= animThreshold) jetTarget = 2;
            else if (inputMoveDir.y <= -animThreshold) jetTarget = 0;
            if (jetTarget != jetState)
            {
                if (jetTransitionTimer > 0) jetTransitionTimer -= Time.deltaTime;
                else
                {
                    if (jetTarget > jetState) jetState++;
                    else jetState--;
                    jetTransitionTimer = jetTransitionTime;
                }
            }
            else if (jetTarget == jetState) jetTransitionTimer = jetTransitionTime;

            foreach (GameObject jetFlame in jetFlames)
            {
                jetFlame.GetComponent<Transform>().localScale = new Vector3(jetScales[jetState], jetScales[jetState], jetScales[jetState]);
            }

            //health stuff
            if (damageIframeTimer > 0)
            {
                if (flickerCounter >= flickerPattern[flickerIndex])//if we're moving to the next index in the flickerpattern
                {
                    visible = !visible; //toggle visibility
                    flickerCounter = 0;
                    flickerIndex++;
                    if (flickerIndex == flickerPattern.Length) flickerIndex = 0; //loop back around to the start
                }
                flickerCounter++;

                foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>()) meshRenderer.enabled = visible; //apply visibility
                foreach (SkinnedMeshRenderer meshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>()) meshRenderer.enabled = visible; //for skinned meshes too

                damageIframeTimer -= Time.deltaTime;

                if (damageIframeTimer <= 0)
                {
                    this.gameObject.layer = 9; //set layer back to Player
                    foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>()) meshRenderer.enabled = true;//make sure all the bits are visible
                    foreach (SkinnedMeshRenderer meshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>()) meshRenderer.enabled = true; //for skinned meshes too
                }
            }

            else
            {
                if (healthManager.Health <= 0)
                {
                    UpgradeLevel--;
                    ArrayList changedGuns = UpdateUpgrades();

                    if (GunBreakEffect != null)
                    {
                        foreach (Transform changedGun in changedGuns)
                        {
                            GameObject brokenEffect = Instantiate(GunBreakEffect, changedGun.position, Quaternion.identity);
                        }
                    }
                    if (UpgradeLevel == 0)
                    {
                        DangerTimer = DangerTime;
                        if (EffectDanger != null) effectDanger = Instantiate(EffectDanger, tf.position, tf.rotation, tf);
                    }
                    else if (UpgradeLevel <= 0)
                    {
                        //game over
                        if (EffectDeath != null) Instantiate(EffectDeath, GlobalTools.PixelSnap(tf.position), tf.rotation, tf.parent);
                        GlobalTools.levelController.PlayerDied();
                        this.gameObject.SetActive(false);
                    }

                    this.gameObject.layer = 8; //set layer to Invincible
                    healthManager.Health = 1;
                    damageIframeTimer = damageIframes;
                    visible = false;
                    flickerCounter = 0;
                    flickerIndex = 0;

                }
            }

            if (UpgradeLevel == 0)
            {
                if (DangerTimer <= 0)
                {
                    UpgradeLevel = 1;
                    UpdateUpgrades();
                    if (effectDanger != null)
                    {
                        Destroy(effectDanger);
                        effectDanger = null;
                    }
                }
                DangerTimer -= Time.deltaTime;
            }

            if (fireDelayCounter <= 0)
            {
                if (gameplayActions.GetAction("Fire").phase == InputActionPhase.Started)//insert "fire button pressed" here
                {
                    FireGuns(shotType);
                    fireDelayCounter = shotType.GetComponent<Bullet>().fireDelay;
                }
            }
            else fireDelayCounter -= Time.deltaTime;

            if (specialDelayCounter <= 0)
            {
                if (gameplayActions.GetAction("Special Fire").phase == InputActionPhase.Started) //insert "special button pressed" here
                {
                    FireGuns(specialType);
                    specialDelayCounter = specialType.GetComponent<Bullet>().fireDelay;
                }
            }
            else specialDelayCounter -= Time.deltaTime;
        }


    }

    void FireGuns(GameObject ShotType)
    {
        bool muteBullets = false;
        AudioSource bulletAudioSource = ShotType.GetComponent<AudioSource>();
        if (bulletAudioSource)
        {
            muteBullets = true;
            if (bulletAudioSource.clip)
            {
                AudioSource newBulletAudioSource = GlobalTools.PlaySound(bulletAudioSource.clip);
                newBulletAudioSource.volume = bulletAudioSource.volume;
            }
        }
        foreach (Transform gun in CurrentGuns)
        {
            GameObject bullet;
            Vector3 bulletPos = new Vector3(gun.position.x, gun.position.y, 0);
            //if we have a parent (probably the camera, or a spawner attached to the camera), parent the bullets to that too
            if (tf.parent != null) bullet = Instantiate(ShotType, bulletPos, Quaternion.identity, tf.parent);//spawn a bullet at each gun, parented
            else bullet = Instantiate(ShotType, bulletPos, Quaternion.identity);//spawn a bullet at each gun
            bullet.GetComponent<Transform>().up = new Vector3(gun.up.x, gun.up.y, 0);
            if (muteBullets) bullet.GetComponent<AudioSource>().enabled = false;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<Powerup>())
        {
            if (UpgradeLevel < UpgradeList.Length)
            {
                UpgradeLevel++;
                if(UpgradeSound) GlobalTools.PlaySound(UpgradeSound);
                if (UpgradeLevel == 1)
                {
                    UpgradeLevel = 2;
                    if (effectDanger)
                    {
                        Destroy(effectDanger);
                        effectDanger = null;
                    }
                }
                ArrayList changedGuns = UpdateUpgrades();
                if (GunBuildEffect)
                {
                    foreach (Transform changedGun in changedGuns)
                    {
                        GameObject brokenEffect = Instantiate(GunBuildEffect, changedGun.position, Quaternion.identity);
                    }
                }
            }
            Destroy(collision.gameObject);
        }
    }

    void LateUpdate()
    {
        rootModel.position = GlobalTools.PixelSnap(tf.position);
    }

    ArrayList UpdateUpgrades()
    {
        ArrayList ChangedGuns = new ArrayList(); //a list of guns that were created or destroyed in this update
        for (int i = 0; i < UpgradeList.Length; i++)
        {
            if (i < UpgradeLevel)
            {
                if (!UpgradeList[i].activeInHierarchy)
                {
                    foreach (Gun gun in UpgradeList[i].GetComponentsInChildren<Gun>())
                    {
                        ChangedGuns.Add(gun.gameObject.transform);
                    }
                }
                UpgradeList[i].SetActive(true);
            } //we could do a similar thing to below, but for freshly added guns (sparks or something)
            else
            {
                //if the object that's being removed was previously active, add it to a list of freshly destroyed guns
                if (UpgradeList[i].activeInHierarchy)
                {
                    foreach (Gun gun in UpgradeList[i].GetComponentsInChildren<Gun>())
                    {
                        ChangedGuns.Add(gun.gameObject.transform);
                    }
                }
                UpgradeList[i].SetActive(false); //now we do the actual removal
            }
        }

        //populate a list of all the currently active guns
        CurrentGuns = new List<Transform>();
        foreach (Gun child in GetComponentsInChildren<Gun>())
        {
            if (child.gameObject.activeInHierarchy == true) CurrentGuns.Add(child.gameObject.transform);
        }
        return ChangedGuns;
    }

    public void PlayVictoryAnimation()
    {
        Animation anim = tf.GetChild(0).GetComponent<Animation>();
        anim.clip = VictoryAnimation;
        anim.AddClip(VictoryAnimation, VictoryAnimation.name);
        anim.Play(VictoryAnimation.name);
        this.enabled = false; //hopefully this switches off PlayerControl but leaves the object active...
    }
}