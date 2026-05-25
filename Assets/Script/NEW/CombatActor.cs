using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public abstract class CombatActor : MonoBehaviour
{
    [Header("Combat")]
    public Faction faction = Faction.Neutral;
    public float power = 1f;
    public float maxHP = 200f;
    public float hurtInterval = 0.3f;

    [Header("Options")]
    public bool allowRotate = true;

    [Header("Events")]
    public UnityEvent dieEvent;

    protected Image hpBar;
    protected Animator anim;
    protected Rigidbody rb;

    protected float hp;
    protected bool dead;
    protected float nextHurtTime = 0f;

    protected Color frozenColor = Color.cyan;
    protected Renderer[] renderers;
    protected readonly List<Material> materials = new List<Material>();
    protected readonly List<Color> originalColors = new List<Color>();

    public bool IsDead => dead;

    protected virtual void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    protected virtual void Start()
    {
        hp = maxHP;
        dead = false;

        InitHPBar();
        InitializeColor();
        OnInitialized();
    }

    protected virtual void InitHPBar()
    {
        var transforms = GetComponentsInChildren<Transform>(true);
        foreach (var tran in transforms)
        {
            if (tran.name == "HPBar")
            {
                hpBar = tran.GetComponent<Image>();
                if (hpBar != null)
                    hpBar.fillAmount = 1f;
                break;
            }
        }
    }

    protected virtual void InitializeColor()
    {
        renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                originalColors.Add(mats[i].color);
                materials.Add(mats[i]);
                mats[i].EnableKeyword("_EMISSION");
            }
        }
    }

    public virtual void TakeDamage(AttackData attack)
    {
        Debug.Log($"[TakeDamage] {name} take {attack.damage} from {(attack.attacker ? attack.attacker.name : "NULL")}");

        if (dead || Time.time < nextHurtTime)
            return;

        hp -= attack.damage;
        if (hpBar)
            hpBar.fillAmount = hp / maxHP;

        FlashEmission();

        if (hp <= 0f)
        {
            Die();
            return;
        }

        PlayHurtAnimation();
        ApplyKnockback(attack);
        ApplyStatusEffect(attack);

        nextHurtTime = Time.time + hurtInterval;
        OnTakeDamage(attack);
    }

    protected virtual void PlayHurtAnimation()
    {
        anim.SetTrigger("hurt");
    }

    protected virtual void ApplyKnockback(AttackData attack)
    {
        if (rb == null || attack.strength <= 0)
            return;

        Vector3 pushBack = (transform.position - attack.position).normalized;
        pushBack *= attack.strength;
        rb.AddForce(pushBack * 10f, ForceMode.Impulse);
    }

    protected virtual void ApplyStatusEffect(AttackData attack)
    {
        if (attack.type == AttackType.frozen)
        {
            for (int i = 0; i < materials.Count; i++)
                materials[i].color = frozenColor;

            anim.speed = GetFrozenAnimSpeed();
            Invoke(nameof(RecoverStatus), 5f);
        }
    }

    protected virtual float GetFrozenAnimSpeed()
    {
        return 0.5f;
    }

    protected virtual void RecoverStatus()
    {
        for (int i = 0; i < materials.Count; i++)
            materials[i].color = originalColors[i];

        anim.speed = 1f;
    }

    protected virtual void FlashEmission()
    {
        for (int i = 0; i < materials.Count; i++)
            materials[i].SetColor("_EmissionColor", Color.white);

        Invoke(nameof(StopEmission), 0.05f);
    }

    protected virtual void StopEmission()
    {
        for (int i = 0; i < materials.Count; i++)
            materials[i].SetColor("_EmissionColor", Color.black);
    }

    protected virtual void Die()
    {
        if (dead) return;

        dead = true;
        anim.SetTrigger("die");

        if (hpBar)
            hpBar.transform.parent.gameObject.SetActive(false);

        dieEvent?.Invoke();
        OnDie();
    }

    protected virtual void OnInitialized() { }
    protected virtual void OnTakeDamage(AttackData attack) { }
    protected virtual void OnDie() { }

    protected virtual void OnDestroy()
    {
        foreach (var mat in materials)
        {
            if (mat != null)
                Destroy(mat);
        }
    }
}