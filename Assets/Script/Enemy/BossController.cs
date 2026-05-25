using UnityEngine;

public class BossController : Enemy
{
    AudioSource AS;
    public AudioClip hurtSound;

    void Start ()
    {
        start();
        AS = GetComponent<AudioSource>();
    }

    protected override float GetFrozenAnimSpeed()
    {
        return 0.8f;
    }

    protected override void OnTakeDamage(AttackData attack)
    {
        if (AS && hurtSound)
            AS.PlayOneShot(hurtSound);
    }

    void Update ()
    {
        update();
    }

    void LateUpdate()
    {
        lateUpdate();
    }
}
