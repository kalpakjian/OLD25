using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBase : CombatActor
{
    [Header("AI")]
    public float traceRange = 5f;
    public float attackRange = 2f;
    public float attackInterval = 1.2f;

    [Header("Locomotion")]
    public float walkDuration = 1f;
    public float walkAnimSpeed = 0.5f;
    public float runAnimSpeed = 1f;

    [Header("Turn")]
    public float turnSpeed = 120f;
    public float moveAngleThreshold = 25f;
    public float attackAngleThreshold = 12f;

    [Header("Animation")]
    public float speedDampTime = 0.1f;

    [Header("Drop")]
    public GameObject rewardItem;

    protected Transform player;
    protected NavMeshAgent agent;
    protected float playerDist;
    protected float nextAttackTime = 0f;

    protected float chaseStartTime = -999f;
    protected bool wasTracing = false;

    protected override void Awake()
    {
        base.Awake();
        faction = Faction.Enemy;

        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    protected override void Start()
    {
        base.Start();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    protected virtual void Update()
    {
        if (dead || player == null)
        {
            StopMove();
            wasTracing = false;
            return;
        }

        playerDist = Vector3.Distance(player.position, transform.position);
        bool inTraceRange = playerDist <= traceRange;
        bool inAttackRange = playerDist <= attackRange;

        if (!inTraceRange)
        {
            StopMove();
            wasTracing = false;
            return;
        }

        if (!wasTracing)
        {
            chaseStartTime = Time.time;
            wasTracing = true;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            StopMove();
            return;
        }

        RotateToTarget(toPlayer);

        float angleToPlayer = Vector3.Angle(transform.forward, toPlayer.normalized);

        if (inAttackRange && angleToPlayer <= attackAngleThreshold)
        {
            agent.SetDestination(transform.position);
            SetAnimSpeed(0f);

            if (Time.time >= nextAttackTime)
            {
                anim.ResetTrigger("attack");
                anim.SetTrigger("attack");
                nextAttackTime = Time.time + attackInterval;
            }

            return;
        }

        if (angleToPlayer > moveAngleThreshold)
        {
            agent.SetDestination(transform.position);
            SetAnimSpeed(0f);
            return;
        }

        agent.SetDestination(player.position);

        float chaseTime = Time.time - chaseStartTime;
        float targetAnimSpeed = chaseTime < walkDuration ? walkAnimSpeed : runAnimSpeed;
        SetAnimSpeed(targetAnimSpeed);
    }

    protected virtual void LateUpdate()
    {
        if (agent == null)
            return;

        agent.nextPosition = transform.position;
    }

    protected virtual void RotateToTarget(Vector3 toTarget)
    {
        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }

    protected virtual void StopMove()
    {
        if (agent != null)
            agent.SetDestination(transform.position);

        SetAnimSpeed(0f);
    }

    protected virtual void SetAnimSpeed(float value)
    {
        anim.SetFloat("speed", value, speedDampTime, Time.deltaTime);
    }

    protected override void OnTakeDamage(AttackData attack)
    {
        nextAttackTime = Time.time + attackInterval;
        SetAnimSpeed(0f);
    }

    protected override void OnDie()
    {
        StopMove();

        if (rewardItem)
            Instantiate(rewardItem, transform.position, Quaternion.identity);

        gameObject.layer = 0;
        Invoke(nameof(Remove), 5f);
    }

    protected virtual void Remove()
    {
        Destroy(gameObject);
    }
}