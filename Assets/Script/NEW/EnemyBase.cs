using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBase : CombatActor
{
    [Header("AI")]
    public float traceRange = 5f;
    public float attackRange = 2f;
    public float attackInterval = 2f;

    [Header("Drop")]
    public GameObject rewardItem;

    protected Transform player;
    protected NavMeshAgent agent;
    protected float playerDist;
    protected float nextAttackTime = 0f;

    protected override void Awake()
    {
        base.Awake();
        faction = Faction.Enemy;
        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = false;
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
            return;

        playerDist = Vector3.Distance(player.position, transform.position);

        if (playerDist < attackRange)
        {
            anim.SetBool("walk", false);

            if (Time.time >= nextAttackTime)
            {
                anim.SetTrigger("attack");
                nextAttackTime = Time.time + attackInterval;
            }
        }
        else if (playerDist < traceRange)
        {
            agent.SetDestination(player.position);
            anim.SetBool("walk", true);
        }
        else
        {
            anim.SetBool("walk", false);
            agent.SetDestination(transform.position);
        }
    }

    protected virtual void LateUpdate()
    {
        if (agent == null)
            return;

        agent.nextPosition = transform.position;
        agent.updateRotation = allowRotate;
    }

    protected override void OnTakeDamage(AttackData attack)
    {
        nextAttackTime = Time.time + attackInterval;
    }

    protected override void OnDie()
    {
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