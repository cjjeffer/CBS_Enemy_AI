using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Transition;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Andtech.ProTracer;

public class EnemyShooterAI : MonoBehaviour
{
    [SerializeField]
    private bool dead;
    [SerializeField] Animator anim;
    public enum entranceMove {moveForward, moveRight, peek, timeline}
    [SerializeField]
    private float moveAmount = 0f;
    [SerializeField]
    private float howQuick = 0f;
    
    //Gun FX
    [SerializeField]
    private GameObject gunFireFX;
    [SerializeField]
    private GameObject bulletImpacterFooley; //Affect the environment surrounding bullet impact


    private Vector3 playerDir;

    //The active ragdoll is collasping immediately, keep the collision threshold unobtainably high until the timeline sequences starts
    [SerializeField]
    float startingImmunityTime = 3f;

    bool readyToAttack;
    public entranceMove eMove = new entranceMove();
    Transform player;

    [SerializeField]
    private Vector3 coverPositionToTransitionTo;
    private bool transitionToCoverPosition;
    bool transitionToCoverTriggered = false;

    [SerializeField]
    GameObject TimelineObj;
    [SerializeField]
    GameObject fallBehavior;
    [SerializeField]
    RootMotion.Dynamics.BehaviourPuppet behaviorPuppet;
    [SerializeField]
    GameObject OnDeathPackage;
    bool timelineHasStartedAtLeastOnce = false;
    bool timelineOver = false;

    [SerializeField]
    RootMotion.FinalIK.LookAtController lookAtController;

    [SerializeField]
    RootMotion.Dynamics.PuppetMaster puppetM;


    [SerializeField]
    Bullet bulletPrefab;
    [SerializeField]
    SmokeTrail smokeTrailPrefab;

    bool staggering = false;
    Vector3 lastPos;

    float countToDeadline;
    // Start is called before the first frame update
    void Start()
    {
        if(behaviorPuppet != null)
        {
            behaviorPuppet.collisionThreshold = 70f;
            Invoke("DropImmunity", startingImmunityTime);
        }


        if(eMove == entranceMove.timeline)
        {
            //TimelineObj.SetActive(true);
            Invoke("FireGunOnIntervals", Random.Range(0f, 5f));
        }

        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
        //lookAtController.target = player;
        //Invoke("FireGunOnIntervals", Random.Range(0f,5f));
    }

    // Update is called once per frame
    void Update()
    {

        if(player != null)
        {
            if (lookAtController != null)
            {
                lookAtController.target = player.transform;
            }
        }

        if(eMove == entranceMove.timeline)
        {
            if (TimelineObj != null)
            {
                
                PlayableDirector dir = TimelineObj.GetComponent<PlayableDirector>();

                if (!timelineHasStartedAtLeastOnce)
                {
                    if (dir.state == PlayState.Playing)
                    {
                        timelineHasStartedAtLeastOnce = true;
                        //add the code to stop a timeline if the fallbehavior is active.. you just added a serializefield gameobject fall behavior up above. check if its active
                        
                    }
                }

                if (dir.state == PlayState.Paused && timelineHasStartedAtLeastOnce && !transitionToCoverTriggered)
                {
                    //transitionToCoverPosition = true;
                }

                if (fallBehavior.GetComponent<RootMotion.Dynamics.BehaviourBipedStagger>().enabled)
                {
                    if (dir.state == PlayState.Playing)
                    {
                        dir.gameObject.SetActive(false);
                        GameObject.Destroy(dir.gameObject);
                        if (OnDeathPackage != null)
                        {
                            OnDeathPackage.SetActive(true);
                        }
                    }
                    lookAtController.ik.enabled = false;
                    lookAtController.enabled = false;
                    dead = true;
                }
            }
        }

        //move to position after initial movement routine
        if (transitionToCoverPosition && !transitionToCoverTriggered)
        {
            transform.positionTransition(coverPositionToTransitionTo, Vector3.Distance(transform.TransformPoint(transform.GetChild(0).localPosition), coverPositionToTransitionTo) / 3f, LeanEase.Linear);
            transitionToCoverTriggered = true;
            Invoke("MoveALittle",(Vector3.Distance(transform.TransformPoint(transform.GetChild(0).localPosition), coverPositionToTransitionTo) / 3f) + 10f); 
        }
        //after reaches cover position
        //transition to cover spot
        //peak out

        if (!staggering)
        {
            //ControlAnimations();
        }

        if (dead)
        {
            if (countToDeadline >= 10f && puppetM.state == RootMotion.Dynamics.PuppetMaster.State.Alive)
            {
                puppetM.state = RootMotion.Dynamics.PuppetMaster.State.Dead;
            }
            else
            {
                countToDeadline += Time.deltaTime;
            }
        }


        Debug.DrawRay(gunFireFX.transform.TransformPoint(gunFireFX.transform.localPosition), gunFireFX.transform.forward * 3, Color.blue);
        Debug.DrawRay(gunFireFX.transform.TransformPoint(gunFireFX.transform.localPosition) + gunFireFX.transform.forward, gunFireFX.transform.forward * 3, Color.yellow);
        //print(-transform.TransformDirection(gunFireFX.transform.forward));
    }

    void StartTrackingPlayer()
    {
        readyToAttack = true;
        anim.Play("Pistol_Idle", 0);
        if(coverPositionToTransitionTo != Vector3.zero)
        {
            transitionToCoverPosition = true;
        }
    }

    void FireGunOnIntervals()
    {
        if(gunFireFX != null)
        {
            gunFireFX.SetActive(true);
        }

        anim.Play("Pistol_ShootOnce", 1);
        BulletAffectEnemy();

        if (!dead)
        {
            Invoke("FireGunOnIntervals", Random.Range(0.1f, 0.7f));
        }
    }

    public void BulletAffectEnemy()
    {
        float randX = Random.Range(-14f, 14f);
        float randY = Random.Range(-0.2f, 0.2f);
        GameObject obj = Instantiate(bulletImpacterFooley, gunFireFX.transform.TransformPoint(gunFireFX.transform.localPosition) + gunFireFX.transform.forward, Quaternion.identity);
        obj.GetComponent<Rigidbody>().AddForce(gunFireFX.transform.forward * 30f + new Vector3(randX, randY, 0), ForceMode.Impulse);


        //all of the below code is for the visual representation of the bullet only
        Bullet bullet = Instantiate(bulletPrefab);
        SmokeTrail smokeTrail = Instantiate(smokeTrailPrefab);

        bullet.DrawRay(gunFireFX.transform.TransformPoint(gunFireFX.transform.localPosition), gunFireFX.transform.forward + new Vector3(randX, randY, 0), 10.0F + (3 - 1) * 50.0F, 300, 0, true);
        smokeTrail.DrawRay(gunFireFX.transform.TransformPoint(gunFireFX.transform.localPosition), gunFireFX.transform.forward + new Vector3(randX, randY, 0), 10.0F + (3 - 1) * 50.0F, 25.0F, 0);
    }

    //The staggering behaviour is currently active
    public void IsStaggering()
    {
        staggering = true;
    }

    //Has the enemy recovered from active ragdoll induced staggering
    public void GottenUp()
    {
        staggering = false;
    }

    

    


    void DropImmunity()
    {
        // 1 will be the base collision threshold
        behaviorPuppet.collisionThreshold = 1f;
    }

    public bool isDead()
    {
        return dead;
    }
}
