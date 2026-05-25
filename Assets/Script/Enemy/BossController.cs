using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BossController : EnemyBase
{
    AudioSource audioSource;
    public AudioClip hurtSound;

    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
    }

    protected override float GetFrozenAnimSpeed()
    {
        return 0.8f;
    }

    protected override void OnTakeDamage(AttackData attack)
    {
        base.OnTakeDamage(attack);

        if (audioSource && hurtSound)
            audioSource.PlayOneShot(hurtSound);
    }
}