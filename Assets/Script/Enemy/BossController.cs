using UnityEngine;

public class BossController : EnemyBase
{
    private AudioSource audioSource;
    public AudioClip hurtSound;

    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
    }

    protected override void OnTakeDamage(AttackData attack)
    {
        base.OnTakeDamage(attack);

        if (audioSource != null && hurtSound != null)
            audioSource.PlayOneShot(hurtSound);
    }

    protected override float GetFrozenAnimSpeed()
    {
        return 0.8f;
    }
}