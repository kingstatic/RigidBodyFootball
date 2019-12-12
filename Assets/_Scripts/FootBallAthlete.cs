﻿using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class FootBallAthlete : MonoBehaviour
{

    //public RouteManager startGoal;
    //todo clean up inheritance and add setters and getters instead of public variables. Separate OffPlayers variables from DefPlayers variables
    public Renderer materialRenderer;
    [HideInInspector] public IKControl iK;
    [HideInInspector] public Terrain terrain;
    [HideInInspector] public QB qb;
    internal SphereCollider sphereCollider;

    [HideInInspector] public Color startColor;
    [HideInInspector] public Color rayColor;

    [HideInInspector] public NavMeshAgent navMeshAgent;
    [HideInInspector] public float navStartSpeed;
    [HideInInspector] public float navStartAccel;
    public Color highlightColor;


    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Animator anim;
    //public LineRenderer lr;



    [SerializeField] private float maxAngle = 35;
    [SerializeField] private float maxRadius = 1;

    [HideInInspector] public Vector3 passTarget;
    [HideInInspector] public bool beenPressed = false;
    [HideInInspector] public FootBall footBall;
    public Canvas canvas;
    public Image pressBar;
    [HideInInspector] public bool isHiked = false;
    [HideInInspector] public GameManager gameManager;
    [HideInInspector] public RouteManager routeManager;
    [HideInInspector] public bool isBlocking;
    [HideInInspector] public OffPlay offPlay;
    [HideInInspector] public Transform targetPlayer;

    public Routes[] routes;
    public Routes myRoute;
    [HideInInspector] public int routeSelection;
    internal int totalCuts;
    internal Vector3 lastCutVector;
    internal bool wasAtLastCut = false;
    internal int currentRouteIndex = 0;
    internal float routeCutTolerance = 1.5f;


    internal float timeSinceArrivedAtRouteCut;
    internal Vector3 nextPosition;

    [HideInInspector] public WR[] wideRecievers;
    [HideInInspector] public DB[] defBacks;
    [HideInInspector] public HB[] hbs;
    [HideInInspector] public Oline[] oLine;
    [HideInInspector] public Dline[] dLine;
    [HideInInspector] public DefPlayer[] defPlayers;
    [HideInInspector] public OffPlayer[] offPlayers;
    [Range(5, 10)] public float defZoneSize; //todo should this be determined by the Zone? //Maybe awareness checks

    [HideInInspector] public Zones zone;
    [HideInInspector] public OffPlayer targetReceiver;
    [HideInInspector] public HB targetHb;
    [HideInInspector] public DB targetDb;

    public bool isMan;
    public bool isZone;

    [HideInInspector] public bool isPressing;

    [HideInInspector] public bool isCatching = false;

    [HideInInspector] public CameraFollow cameraFollow;

    [HideInInspector] public UserControl userControl;
    [HideInInspector] public CameraRaycaster cameraRaycaster;
    [HideInInspector] public bool isSelected;

    internal virtual void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        routes = FindObjectsOfType<Routes>();
        routeManager = FindObjectOfType<RouteManager>();
        cameraFollow = FindObjectOfType<CameraFollow>();
        cameraRaycaster = CameraRaycaster.instance;
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        qb = FindObjectOfType<QB>();
        hbs = FindObjectsOfType<HB>();
        defBacks = FindObjectsOfType<DB>();
        dLine = FindObjectsOfType<Dline>();
        oLine = FindObjectsOfType<Oline>();
        wideRecievers = FindObjectsOfType<WR>();
        defPlayers = FindObjectsOfType<DefPlayer>();
        offPlayers = FindObjectsOfType<OffPlayer>();
        //lr.material.color = LineColor;

        AddIk();
        startColor = materialRenderer.material.color;
        navMeshAgent.destination = transform.position;
        navStartSpeed = navMeshAgent.speed;
        navStartAccel = navMeshAgent.acceleration;
        cameraRaycaster.OnMouseOverOffPlayer += OnMouseOverOffPlayer;
        gameManager.clearSelector += ClearSelector;

    }
    public virtual void FixedUpdate()
    {
        //Vector3 forward = transform.TransformDirection(Vector3.forward) * 10;
        //Debug.DrawRay(transform.position, forward, rayColor);
        Vector3 angleFov2 = Quaternion.AngleAxis(maxAngle, transform.up) * transform.forward;
        Vector3 angleFov1 = Quaternion.AngleAxis(-maxAngle, transform.up) * transform.forward;
        Debug.DrawRay(transform.position, angleFov2, rayColor);
        Debug.DrawRay(transform.position, angleFov1, rayColor);
        RaycastForward();
    }

    private void AddIk()
    {
        iK = gameObject.AddComponent<IKControl>();
        iK.isActive = false;
    }

    private void GetTerrain()
    {
        if (terrain == null)
        {
            terrain = FindObjectOfType<Terrain>();
        }
    }

    public void SetUserControl()
    {
        tag = "Player";
        rb = GetComponent<Rigidbody>();
        cameraFollow = FindObjectOfType<CameraFollow>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        userControl = GetComponent<UserControl>();
        userControl.enabled = true; //todo create an method to disable user control on all other players
        rb.isKinematic = false;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, 0, transform.eulerAngles.z);
        navMeshAgent.enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {

        if (this is OffPlayer) return;
        if (!terrain) GetTerrain();
        var collGo = collision.gameObject;
        if (collGo.transform == terrain.transform) return;

        //Debug.Log("Collision " + collGo.name + " and " + name);
        ContactPoint contacts = collision.contacts[0];
        Debug.DrawLine(transform.position, contacts.point);
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal, Color.white);
        }

        if (gameManager.ballOwner == null) return;
        if (collGo == gameManager.ballOwner.gameObject)
        {
            //todo check if player is facing ballOwner is targetPlayer in 'tackle angle'

            FootBallAthlete playerType = collGo.GetComponent<FootBallAthlete>();
            if (playerType is OffPlayer)
            {
                Tackle(GameManager.instance.ballOwner);

            }
        }
        if (collision.relativeVelocity.magnitude > 2)
        {
            Debug.Log("Collision magnitude");
        }


    }


    internal void Tackle(FootBallAthlete instanceBallOwner)
    {
        //need off vs def check
        //anim.SetTrigger("TackleTrigger");
        instanceBallOwner.StartCoroutine("BeTackled");
    }

    internal IEnumerator BeTackled()
    {
        materialRenderer.material.color = Color.red;
        yield return new WaitForSeconds(.5f);

        if (gameManager.isPassStarted == false && !gameManager.isRapidfire) gameManager.ResetScene();

        materialRenderer.material.color = startColor;
    }

    internal void RaycastForward()
    {
        RaycastHit[] hits;
        hits = Physics.RaycastAll(transform.position, transform.forward, 100.0F);
        //if(hits.Length != 0)Debug.Log(hits.Length);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider.isTrigger)
            {
                Transform zoneObject = hit.collider.transform;
                if (zoneObject)
                {
                    //todo something with this info about incoming routes
                }
            }

        }
    }
    internal void OnMouseOverOffPlayer(OffPlayer offPlayer)
    {
        //Debug.Log("Mouse over " + offPlayer.transform.name);
        if (!gameManager.isPass) return;

        //Debug.Log("MouseOverWR");
        if (offPlayer != this) return;
        materialRenderer.material.color = highlightColor;
        gameManager.SetSelector(gameObject);
        //todo this is terrible, maybe a switch?  Plus should we really be calling a pass from the WR???
        int mouseButton;
        if (Input.GetMouseButtonDown(0)) { mouseButton = 0; BeginPass(mouseButton, offPlayer); }
        if (Input.GetMouseButtonDown(1)) { mouseButton = 1; BeginPass(mouseButton, offPlayer); }
        if (Input.GetMouseButtonDown(2)) { mouseButton = 2; BeginPass(mouseButton, offPlayer); }


        // todo bool canBePassedTo, then raycast OnMouseOverWr for QB to throw

    }
    internal void BeginPass(int mouseButton, OffPlayer receiver) //todo, move code to QB or game manager
    {

        //todo adjust throwPower dependent on distance and throw type
        if (mouseButton == 0) //Bullet Pass
        {
            passTarget = transform.position;
            qb.BeginThrowAnim(passTarget, receiver, 1.5f, 23f); //todo fix hardcoded variables, needs to be a measure of distance + qb throw power
        }

        if (mouseButton == 1) // Touch Pass
        {
            passTarget = transform.position;
            qb.BeginThrowAnim(passTarget, receiver, 2.3f, 20f);
        }

        if (mouseButton == 2) // Lob PASS
        {
            //Debug.Log("Pressed middle click.");
            passTarget = transform.position;
            qb.BeginThrowAnim(passTarget, receiver, 3.2f, 19.5f);
        }


    }
    internal void SetDestination(Vector3 dest)
    {
        //if(this.name == "DB") Debug.Log(this.name + " " +"set dest " + dest);
        EnableNavMeshAgent();
        navMeshAgent.SetDestination(dest);
    }
    internal void EnableNavMeshAgent()
    {
        if (!navMeshAgent.enabled)
            navMeshAgent.enabled = true;
        if (navMeshAgent.isStopped)
            navMeshAgent.isStopped = false;
    }
    internal Transform GetClosestHb(HB[] enemies)
    {
        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (HB potentialTarget in enemies)
        {

            Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget.transform;
            }
        }

        return bestTarget;
    }
    internal Transform GetClosestWr(WR[] enemies)
    {
        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (WR potentialTarget in enemies)
        {

            Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget.transform;
            }
        }

        return bestTarget;
    }
    internal Transform GetClosestDB(DB[] enemies)
    {
        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (DB potentialTarget in enemies)
        {
            Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget.transform;
                targetDb = potentialTarget;
            }
        }
        return bestTarget;
    }
    internal Transform GetClosestDefPlayer(DefPlayer[] enemies)
    {
        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (DefPlayer potentialTarget in enemies)
        {

            Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {

                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget.transform;
            }
        }

        return bestTarget;
    }

    internal void AddClickCollider()
    {
        sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.radius = 3f;//todo make inspector settable
        sphereCollider.isTrigger = true;
        sphereCollider.tag = "ClickCollider";
    }

    internal void ClearSelector(bool isClear)
    {
        if (materialRenderer.material.color != startColor)
            materialRenderer.material.color = startColor;
    }
    internal void StopNavMeshAgent()
    {
        if (!navMeshAgent.isStopped)
        {
            navMeshAgent.isStopped = true;
            //Debug.Log("NavAgent Stopped");
        }
    }
}

public class OffPlayer : FootBallAthlete
{
    public float blockRange = 3;
    public float blockCoolDown = 1f;
    public bool canBlock = true;
    internal bool isBlocker;
    public bool isReciever;
    List<DefPlayer> blockList;

    internal override void Start()
    {
        base.Start();
        gameManager.onBallThrown += BallThrown;
        gameManager.hikeTheBall += HikeTheBall;
        startColor = materialRenderer.material.color;

    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    private void HikeTheBall(bool wasHiked) //event
    {
        anim.SetTrigger("HikeTrigger");
    }

    internal void GetRoute(int routeSelector)
    {
        myRoute = Instantiate(routeManager.allRoutes[routeSelector], transform.position, transform.rotation).GetComponent<Routes>(); //todo get route index selection
        myRoute.transform.name = routeManager.allRoutes[routeSelector].name;

        //myRoute.transform.position = transform.position;

        var childCount = myRoute.transform.childCount;
        totalCuts = childCount - 1; //todo had to subtract 1 because arrays start at 0

        lastCutVector = myRoute.transform.GetChild(totalCuts).position;
        //Debug.Log("total cuts " + totalCuts + "Child Count " + (childCount - 1));
        //Debug.Log(myRoute.transform.name);

        if (!targetPlayer) GetTarget();
        SetDestination(myRoute.GetWaypoint(currentRouteIndex));

    }

    internal void RunRoute()
    {
        if (myRoute == null) return;
        if (!AtRouteCut()) return;

        if (timeSinceArrivedAtRouteCut > myRoute.routeCutDwellTime[0])
        {
            CycleRouteCut();
            nextPosition = GetCurrentRouteCut();
            navMeshAgent.destination = nextPosition;
            timeSinceArrivedAtRouteCut = 0;
            //Debug.Log("start NavMeshAgent");

            if (!AtLastRouteCut()) return;
            wasAtLastCut = true;
            WatchQb();
            //Debug.Log("set last cut true");
            return;

        }

        timeSinceArrivedAtRouteCut += Time.deltaTime;
    }


    internal void WatchQb()
    {
        EnableNavMeshAgent();
        navMeshAgent.SetDestination(lastCutVector);
        anim.SetTrigger("WatchQBTrigger");
        transform.LookAt(qb.transform.position);

    }

    private void CycleRouteCut()
    {
        currentRouteIndex = myRoute.GetNextIndex(currentRouteIndex);
    }

    internal bool IsEndOfRoute()
    {
        var endOfRoute = (totalCuts == currentRouteIndex && (transform.position - navMeshAgent.destination).sqrMagnitude <= 1);
        //Debug.Log("End of Route? = " + endOfRoute);
        return endOfRoute;
    }

    private bool AtRouteCut()
    {
        float distanceToCut = Vector3.Distance(transform.position, GetCurrentRouteCut());
        return distanceToCut < routeCutTolerance;


    }
    private bool AtLastRouteCut()
    {
        float distanceLastCut = Vector3.Distance(transform.position, (myRoute.transform.GetChild(totalCuts).position));
        return distanceLastCut < routeCutTolerance;
    }
    private Vector3 GetCurrentRouteCut()
    {
        return myRoute.GetWaypoint(currentRouteIndex);

    }


    internal void BallThrown(QB thrower, OffPlayer reciever, FootBall ball, Vector3 impactPos, float arcType, float power, bool isComplete) //event
    {
        //todo move receiver to a through pass targetPlayer
        if (reciever == this)
        {
            StartCoroutine("GetToImpactPos", impactPos);
            StartCoroutine("TracktheBall", ball);

            SetTarget(ball.transform);
            footBall = ball;

            isCatching = true;
            if (isComplete) qb.userControl.enabled = false; //todo i dont like this, should be in the qb script?
        }
        else
        {
            StartCoroutine("GetToBlockPosition", impactPos);
        }
    }

    private IEnumerator GetToBlockPosition()
    {
        //todo track ball carrier position and determine blocking position

        yield return new WaitForSeconds(1);
    }

    internal bool CanBePressed()
    {
        if (!beenPressed)
        {
            beenPressed = true;
            return true;
        }
        return false;
    }

    internal void Press(float pressTimeNorm)
    {
        canvas.enabled = true;
        anim.SetTrigger("PressTrigger");
        pressBar.fillAmount = pressTimeNorm;
        navMeshAgent.acceleration = 0f;
        navMeshAgent.speed = 0f;
    }

    internal void ReleasePress()
    {
        anim.SetTrigger("ReleaseTrigger");
        canvas.enabled = !canvas.enabled;
        navMeshAgent.speed = navStartSpeed;
        navMeshAgent.acceleration = navStartAccel;
    }

    internal void SetColor(Color color)
    {
        materialRenderer.material.color = color;
        //throw new System.NotImplementedException();
    }

    internal void BlockProtection() //todo consolidate duplicate code
    {
        if (!canBlock) return;
        ;
        //todo change code to be moving forward with blocks, get to the second level after shedding first defender
        if (targetPlayer == null)
        {
            //sudo get positions of all other blockers
            var blockTargets = gameManager.defPlayers;
            var bestBlockTarget = GetClosestDefPlayer(blockTargets);
            if ((bestBlockTarget.transform.position - transform.position).magnitude < blockRange)


                //todo compare def players who are engaged in a block, double-team or get down-field
                targetPlayer = GetClosestDefPlayer(blockTargets);
        }
        if (targetPlayer == null) return;
        Vector3 directionToTarget = targetPlayer.position - transform.position;
        transform.LookAt(targetPlayer);

        if (gameManager.isRun) SetTargetDlineRun(targetPlayer);
        if (gameManager.isPass) SetTargetDline(targetPlayer);

        float dSqrToTarget = directionToTarget.sqrMagnitude;
        if (dSqrToTarget < 3) //todo setup block range variable
        {
            var defPlayer = targetPlayer.GetComponent<DefPlayer>();
            if (!isBlocking) StartCoroutine(BlockTarget(targetPlayer));
        }
    }


    private IEnumerator GetToImpactPos(Vector3 impact)
    {
        SetDestination(impact);
        yield return new WaitForSeconds(2);
        //todo this is a very bad way to do this
        ResetRoute();
    }

    private IEnumerator TracktheBall(FootBall ball)
    {
        while ((transform.position - ball.transform.position).magnitude > 10f) //todo will have to create some forumla based on throwPower to trigger animations correctly
        {
            iK.isActive = true;
            iK.LookAtBall(ball);
            //todo receiver doesnt get to ball well on second throw
            yield return new WaitForEndOfFrame();
        }

        anim.SetTrigger("CatchTrigger");
        StartCoroutine("CatchTheBall", ball);
    }

    private IEnumerator CatchTheBall(FootBall ball)
    {
        iK.rightHandObj = ball.transform;

        if (ball.isComplete)
        {
            anim.SetBool("hasBall", true);
            while ((transform.position - ball.transform.position).magnitude > 2f)
            {
                cameraFollow.FollowBall(ball);
                yield return new WaitForEndOfFrame();
            }

            if (navMeshAgent.enabled == true)
            {
                navMeshAgent.ResetPath();
                navMeshAgent.enabled = false;
            }

            gameManager.ChangeBallOwner(GameObject.FindGameObjectWithTag("Player"), gameObject);
            gameManager.isPassStarted = false;

        }
        ResetRoute();
    }


    public void ResetRoute() // Called from anim event
    {
        myRoute = null;
        footBall = null;
        targetPlayer = null;
        isCatching = false;
        iK.isActive = false;
        StopAllCoroutines();
    }

    private void SetTarget(Transform _target)
    {
        targetPlayer = _target;
    }

    private void GetTarget()
    {
        if (targetPlayer == null)
        {
            SetDestination(myRoute.GetWaypoint(currentRouteIndex));
        }

        //if ((navMeshAgent.destination - transform.position).magnitude < 1 && targetPlayer == null)
        //{
        //    SetTargetPlayer(startGoal.transform);
        //    SetDestination(startGoal.transform.position);
        //}

        if (navMeshAgent.hasPath && navMeshAgent.remainingDistance < 2 && targetPlayer != null)
        {
            var ball = targetPlayer
                .GetComponent<FootBall>(); //todo this is all bad, attempting to destroy the football and set destination to the route 
            if (ball != null)
            {
                SetDestination(myRoute.GetWaypoint(currentRouteIndex));
            }
        }
    }


    private void SetTargetDlineRun(Transform target) //todo collapse into single function
    {
        var ballOwner = gameManager.ballOwner;
        if (ballOwner != null)
        {
            SetDestination(ballOwner.transform.position + (target.position - ballOwner.transform.position) / 2); // 
        }
        else
        {
            SetDestination(target.position);
        }
    }

    public void SetTargetDline(Transform target)
    {
        //if (isBlocking) return;
        SetDestination(qb.transform.position +
                       (target.position - qb.transform.position) /
                       2); // todo centralize ball carrier, access ballcarrier instead of hard coded transform
    }

    private IEnumerator BlockTarget(Transform target)
    {
        //todo 
        var defPlayer = target.GetComponent<DefPlayer>();

        if (defPlayer.blockPlayers.Contains(this)) yield break;

        float blockTime = 1f; //todo make public variable
        float blockTimeNorm = 0;
        while (blockTimeNorm <= 1f) //todo this counter is ugly and needs to be better
        {
            isBlocking = true;
            blockTimeNorm += Time.deltaTime / blockTime;
            defPlayer.BeBlocked(blockTimeNorm, this);
            yield return new WaitForEndOfFrame();
        }

        defPlayer.ReleaseBlock(this);
        targetPlayer = null;
        isBlocking = false;
        canBlock = false;
        StartCoroutine(BlockCoolDown());
    }

    private IEnumerator BlockCoolDown()
    {
        yield return new WaitForSeconds(blockCoolDown);
        canBlock = true;
    }

}

public class DefPlayer : FootBallAthlete
{
    [HideInInspector] public bool isBlocked;
    public bool wasBlocked = false;
    [SerializeField] internal Image blockBar;
    internal float blockCooldown = 2f;
    internal List<OffPlayer> blockPlayers = new List<OffPlayer>();
    [SerializeField] internal int zoneLayer = 8;
    internal bool isBackingOff = false;
    internal bool isWrIncoming = false;
    internal bool isBlockingPass = false;

    internal override void Start()
    {
        base.Start();
        AddEvents();
    }
    public override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    internal void AddEvents()
    {
        gameManager.ballOwnerChange += BallOwnerChange;

    }

    public void Press(float pressTimeNorm)
    {
        //pressBar.fillAmount = pressTimeNorm;
        DisableNavmeshAgent();
        navMeshAgent.acceleration = 0f;
        navMeshAgent.speed = 0f;
    }
    internal void CreateZone()
    {
        //todo make zoneCenterGO move functions dependent on play developement;
        Vector3 zoneCenterStart = GetZoneStart();
        GameObject zoneGO = Instantiate(new GameObject(), zoneCenterStart, Quaternion.identity);
        zone = zoneGO.AddComponent<Zones>();
        zoneGO.transform.position = transform.position + new Vector3(0, 0, 5);
        zoneGO.transform.name = transform.name + "ZoneObject";
        GameObject zoneObjectContainer = GameObject.FindGameObjectWithTag("ZoneObject"); //Hierarchy Cleanup
        zoneGO.transform.parent = zoneObjectContainer.transform;
        zoneGO.transform.tag = "ZoneObject";
        SphereCollider sphereCollider = zoneGO.gameObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        zoneGO.layer = zoneLayer;
        zone.zoneSize = defZoneSize;
    }

    internal Vector3 GetZoneStart()
    {
        return transform.position + new Vector3(5, 0, 0); //todo this needs to be dependent on player responsibilities
    }

    internal void PlayReact()
    {
        var reactTime = 0f;
        while (reactTime < 1.5f)
        {
            reactTime += Time.deltaTime;
        }

        targetPlayer = GetClosestHb(hbs);
        SetTargetHb(targetPlayer);
    }

    public void ReleasePress()
    {
        canvas.enabled = !canvas.enabled;
        EnableNavMeshAgent();
        navMeshAgent.speed = navStartSpeed;
        navMeshAgent.acceleration = navStartAccel;
    }

    internal void PlayZone()
    {
        //todo access WR route to see if it will pass through zone and then move towards intercept point
        if (targetReceiver == null)
        {
            //has return if enemy set
            if (CheckZone()) return;
        }

        SetDestination(zone.zoneCenter);

    }

    private bool CheckZone()
    {
        var possibleEnemy = GetClosestWr(wideRecievers);
        Vector3 wrZoneCntrDist = possibleEnemy.position - zone.transform.position;
        //Debug.Log(wrZoneCntrDist.magnitude);
        if (wrZoneCntrDist.magnitude < zone.zoneSize)
        {
            //todo this will break with mulitple recievers, need to determine coverage responsablilty
            SetTargetOffPlayer(possibleEnemy);
            return true;
        }

        foreach (var reciever in wideRecievers)
        {
            RaycastHit[] hits = Physics.RaycastAll(reciever.transform.position, reciever.transform.forward, 100.0F);
            //if(hits.Length != 0)Debug.Log(hits.Length);

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider.tag == "ZoneObject")
                {
                    Zones zoneObject = hit.collider.gameObject.GetComponent<Zones>();

                    if (zoneObject == zone)
                    {
                        //Maybe Method
                        SetDestination(hit.point);
                        Debug.DrawLine(transform.position, hit.point, Color.red);
                        return true;

                    }

                }

            }
        }

        return false;

    }

    internal void SetTargetOffPlayer(Transform targetTransform)
    {
        EnableNavMeshAgent();
        SetDestination(targetTransform.position);
        targetPlayer = targetTransform;
        targetReceiver = targetTransform.GetComponentInParent<OffPlayer>();
        //Debug.Log("Target Changed");
    }
    internal IEnumerator WrPress(OffPlayer offPlayer)
    {
        isPressing = true;
        float pressTime = .5f;
        anim.SetTrigger("PressTrigger");
        SetDestination(offPlayer.transform.position + transform.forward);
        float pressTimeNorm = 0;
        //Vector3 dir = (offPlayer.transform.position - transform.position).normalized * pressTime;
        //rb.velocity = dir;
        while (pressTimeNorm <= 1f)
        {
            StayInfront();
            pressTimeNorm += Time.deltaTime / pressTime;
            offPlayer.Press(pressTimeNorm);
            yield return new WaitForEndOfFrame();
        }

        anim.SetTrigger("ReleaseTrigger");
        StartCoroutine(BackOffPress(offPlayer));

    }

    private IEnumerator BackOffPress(OffPlayer offPlayer)
    {
        //read receiver route, move backwards, release receiver to new defender, moves towards next receiver

        offPlayer.ReleasePress();
        isPressing = false;
        StartCoroutine(TurnTowardsLOS());
        var backOffTime = 0f;
        while (backOffTime < 1)
        {
            //Vector3 dir = targetPlayer.position - transform.position;

            isBackingOff = true;
            //Debug.Log("isBackingOff true");
            BackOff(offPlayer);
            backOffTime += Time.deltaTime;
            yield return new WaitForFixedUpdate();
        }

        anim.SetTrigger("InZoneTrigger");
        isBackingOff = false;
        //Debug.Log("isBackingOff false");
        //todo this needs a stat machine to determine if the DB needs to chase the WR past the Zone, Does he have overhead help
    }

    private IEnumerator TurnTowardsLOS()
    {
        //todo write this function
        yield return new WaitForFixedUpdate();
    }

    private void StayInfront()
    {
        DisableNavmeshAgent();

        float speed = 3; //todo needs to be a calculation of WR release and DB press
        Vector3 dir = (targetPlayer.position + targetPlayer.transform.forward - transform.position).normalized * speed;
        float v = dir.z;
        float h = dir.x;

        anim.SetFloat("VelocityX", h * speed);
        anim.SetFloat("VelocityZ", v * speed);

        rb.velocity = dir;
    }


    private void BackOff(OffPlayer offPlayer)
    {
        DisableNavmeshAgent();
        // turn around and run, cut left, or cut right

        float speed = 5; //todo this should be a calculation of offPlayer release vs db press
        Vector3 dir = (offPlayer.transform.position + offPlayer.transform.forward - transform.position).normalized * speed;
        float v = dir.z;
        float h = dir.x;

        anim.SetFloat("VelocityX", h * speed);
        anim.SetFloat("VelocityZ", v * speed);

        rb.velocity = dir;
    }

    public void BeBlocked(float blockTimeNorm, OffPlayer blocker)
    {
        canvas.enabled = true;
        canvas.transform.LookAt(Camera.main.transform);

        //todo make way around multiple defenders
        SetTargetPlayer(blocker.transform);
        blockBar.fillAmount = blockTimeNorm;
        navMeshAgent.acceleration = 0f;
        navMeshAgent.speed = 0f;
        isBlocked = true;

        if (!blockPlayers.Contains(blocker))
        {
            //print(blocker.name + " added to list");
            blockPlayers.Add(blocker);
        }

    }

    public void ReleaseBlock(OffPlayer blocker)
    {
        canvas.enabled = !canvas.enabled;
        isBlocked = false;
        wasBlocked = true;
        SetTargetPlayer(GameObject.FindGameObjectWithTag("Player").transform);
        navMeshAgent.speed = navStartSpeed;
        navMeshAgent.acceleration = navStartAccel;
        gameManager.RaiseShedBlock(this);
        blockPlayers.Remove(blocker);
    }

    public void SetTargetPlayer(Transform targetSetter)
    {
        EnableNavMeshAgent();
        navMeshAgent.SetDestination(targetSetter.position);
        targetPlayer = targetSetter;
    }

    internal void BallThrown(QB thrower, OffPlayer reciever, FootBall ball, Vector3 impactPos, float arcType, float power, bool isComplete)
    {

    }

    internal void PassAttempt(QB thrower, OffPlayer reciever, FootBall ball, float arcType, float power)
    {
        if (InVincintyOfPass(reciever))
        {
            if (arcType == 1.5f)
            {
                //todo roll dist vs stats
                AniciptateThrow(thrower, reciever, ball, arcType, power);
            }

            if (arcType == 2.3f) { }
            if (arcType == 3.2f) { }
        }
    }

    private void AniciptateThrow(QB thrower, OffPlayer reciever, FootBall ball, float arcType, float power)
    {
        //todo this code is used three times now 
        Vector3 targetPos = reciever.transform.position;
        Vector3 diff = targetPos - transform.position;
        Vector3 diffGround = new Vector3(diff.x, 0f, diff.z);
        Vector3 fireVel, impactPos;
        Vector3 velocity = reciever.navMeshAgent.velocity;


        //FTS Calculations https://github.com/forrestthewoods/lib_fts/tree/master/projects/unity/ballistic_trajectory
        float gravity;

        if (Ballistics.solve_ballistic_arc_lateral(transform.position, power, targetPos + Vector3.up, velocity, arcType,
            out fireVel, out gravity, out impactPos))
        {
            //todo: get distance to impact pos and match it against DB position and speed, 
            //pass outcome of roll to the football call an onInterception or onBlock event
            //figure out how to add a second impluse to the thrown football in the case of a blocked pass


            if ((transform.position - impactPos).magnitude < 5) // todo create range variableStat
            {
                EnableNavMeshAgent();
                StopAllCoroutines();
                navMeshAgent.speed += power; // todo fix this terrible code, basically speeds up character to get in position
                SetDestination(impactPos);
                Debug.Log("PassBlock");
                StartCoroutine("BlockPass", ball);
                reciever.SetColor(Color.red);
                ball.isComplete = false;
            }

        }
    }

    private IEnumerator BlockPass(FootBall ball)
    {
        isBlockingPass = true;
        anim.SetTrigger("BlockPass");
        canvas.gameObject.SetActive(true);

        while ((transform.position - ball.transform.position).magnitude > 2.7) //todo, this should be a calculation of anim time vs distance of football to targetPlayer.
        {
            //Debug.Log((transform.position - ball.transform.position).magnitude);
            yield return new WaitForEndOfFrame();
        }
        ball.BlockBallTrajectory();
        navMeshAgent.speed = navStartSpeed;
        isBlockingPass = false;
        canvas.gameObject.SetActive(false);
    }

    private bool InVincintyOfPass(OffPlayer receiver)
    {
        float distToWr = Vector3.Distance(transform.position, receiver.transform.position);
        if (distToWr <= 5) return true; //todo create coverage range variable
        else return false;
    }

    internal bool IsTargetInZone(Transform coverTarget)
    {
        if ((coverTarget.transform.position - zone.transform.position).magnitude <= zone.zoneSize) return true; //targetPlayer is inside zone
        else return false;
    }


    internal void SetTargetHb(Transform targetTransform)
    {
        SetDestination(targetTransform.position);
        targetPlayer = targetTransform; targetHb = targetTransform.GetComponentInParent<HB>();
        //Debug.Log("Target Changed");
    }

    public bool CanBePressed() //todo rename to block
    {
        if (!beenPressed)
        {
            beenPressed = true;
            return true;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        if (zone)
        {
            // Draw zone sphere 
            Gizmos.color = new Color(0, 0, 255, .5f);
            Gizmos.DrawWireSphere(zone.transform.position, defZoneSize);
        }
    }
    private void TurnAround(Vector3 destination)
    {

        navMeshAgent.updateRotation = false;
        Vector3 direction = (destination - transform.position).normalized;
        Quaternion qDir = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, qDir, Time.deltaTime * 20);
    }

    //https://answers.unity.com/questions/1170087/instantly-turn-with-nav-mesh-agent.html


    public IEnumerator BlockCoolDown()
    {
        yield return new WaitForSeconds(blockCooldown);
        wasBlocked = false;
    }

    public void BallOwnerChange(FootBallAthlete ballOwner)
    {
        SetTargetPlayer(ballOwner.transform);
    }
    internal void DisableNavmeshAgent()
    {
        navMeshAgent.enabled = false;
    }
}


