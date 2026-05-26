using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    [HideInInspector] public float attackDamage;
    public float weaponDamage;
    [HideInInspector] public AttackType type = AttackType.normal;
    [HideInInspector] public int strength = 1;

    [SerializeField] private bool canHitTreasure = false;
    [SerializeField] private bool logValidHit = true;
    [SerializeField] private bool logInvalidHit = false;

    protected CombatActor owner;

    protected virtual void Start()
    {
        owner = GetComponentInParent<CombatActor>();
        if (owner == null && transform.root != null)
            owner = transform.root.GetComponentInChildren<CombatActor>(true);

        if (owner == null)
            Debug.LogWarning($"[WeaponHitbox] {name} owner = NULL");
    }

    protected virtual void OnTriggerEnter(Collider col)
    {
        if (owner == null) return;
        if (attackDamage <= 0) return;

        if (col.transform.root == owner.transform.root) return;

        if (canHitTreasure && col.CompareTag("Treasure"))
        {
            if (logValidHit)
                Debug.Log($"[WeaponHitbox] {name} hit treasure {col.name}");

            col.SendMessage("Hit", SendMessageOptions.DontRequireReceiver);
            return;
        }

        if (!col.CompareTag("Enemy") && !col.CompareTag("Player"))
        {
            if (logInvalidHit)
                Debug.Log($"[WeaponHitbox] ignore {col.name}, tag={col.tag}");
            return;
        }

        CombatActor targetActor = FindCombatActor(col);
        if (targetActor == null)
        {
            if (logInvalidHit)
                Debug.Log($"[WeaponHitbox] no CombatActor on {col.name}");
            return;
        }

        if (targetActor == owner) return;
        if (targetActor.faction == owner.faction) return;

        AttackData attack = new AttackData
        {
            attacker = owner.gameObject,
            attackerFaction = owner.faction,
            damage = attackDamage + weaponDamage,
            position = owner.transform.position,
            type = type,
            strength = strength
        };

        if (logValidHit)
            Debug.Log($"[WeaponHitbox] {name} deal {attack.damage} to {targetActor.name}");

        targetActor.TakeDamage(attack);
    }

    protected CombatActor FindCombatActor(Collider col)
    {
        CombatActor t = col.GetComponent<CombatActor>();
        if (t != null) return t;

        t = col.GetComponentInParent<CombatActor>();
        if (t != null) return t;

        if (col.attachedRigidbody != null)
        {
            t = col.attachedRigidbody.GetComponentInParent<CombatActor>();
            if (t != null) return t;
        }

        return null;
    }
}