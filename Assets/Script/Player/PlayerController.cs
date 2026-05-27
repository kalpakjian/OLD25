using UnityEngine;

public class PlayerController : CombatActor
{
    [Header("Input")]
    public float moveSensitivity = 5f;
    public float tapSensitivity = 3f;

    [Header("Combat")]
    public float attackRange = 3f;
    public float alertRange = 5f;

    float moveSpeed;
    float touchTime;
    float startTouchTime = 0f;
    float screenDiagonal;

    Vector2 touchStartPos;
    Vector2 touchMove;
    Vector3 moveDirection;
    Vector3 lastFaceDirection = Vector3.forward;

    bool lockFacing = false;
    Vector3 lockedFaceDirection = Vector3.forward;

    [HideInInspector]
    public bool NextAttack = true;

    public bool AllowRotate
    {
        get => allowRotate;
        set => allowRotate = value;
    }

    protected override void Awake()
    {
        base.Awake();
        faction = Faction.Player;
    }

    protected override void Start()
    {
        base.Start();
        screenDiagonal = Mathf.Sqrt(Mathf.Pow(Screen.width, 2) + Mathf.Pow(Screen.height, 2));

        if (alertRange < attackRange)
            alertRange = attackRange + 2f;
    }

    void Update()
    {
        if (dead)
            return;

        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        bool isHurt = state.IsName("hurt");
        bool isRolling = state.IsName("roll");

        if (!isRolling)
            lockFacing = false;

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
                touchMove = touch.position;
                startTouchTime = Time.time;
            }

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                touchMove = touch.position;
                moveSpeed = Vector2.Distance(touchStartPos, touchMove) / screenDiagonal;
                moveSpeed = Mathf.Min(moveSpeed * moveSensitivity, 1f);

                UpdateMoveDirection();
            }

            if (touch.phase == TouchPhase.Ended)
            {
                touchTime = Time.time - startTouchTime;
                touchMove = touch.position;

                float endMoveSpeed = Vector2.Distance(touchStartPos, touchMove) / screenDiagonal;

                UpdateMoveDirection();

                if (touchTime < 0.1f * tapSensitivity)
                {
                    if (endMoveSpeed < 0.1f)
                    {
                        if (NextAttack)
                        {
                            FaceNearestEnemyInRange(alertRange);
                            anim.ResetTrigger("attack");
                            anim.SetTrigger("attack");
                        }
                    }
                    else
                    {
                        LockRollDirection();
                        anim.ResetTrigger("roll");
                        anim.SetTrigger("roll");
                    }
                }

                moveSpeed = 0f;
            }
        }
        else
        {
            moveSpeed = 0f;
        }

        anim.SetFloat("speed", moveSpeed);

        if (!allowRotate || isHurt)
            return;

        if (lockFacing)
        {
            FaceLockedDirection();
            return;
        }

        if (moveSpeed > 0.05f)
        {
            FaceMoveDirection();
            return;
        }

        FaceNearestEnemyInRange(alertRange);
    }

    void UpdateMoveDirection()
    {
        Vector2 dragDirection = touchMove - touchStartPos;
        moveDirection = new Vector3(dragDirection.x, 0, dragDirection.y);
        moveDirection = Camera.main.transform.TransformDirection(moveDirection);
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            moveDirection.Normalize();
            lastFaceDirection = moveDirection;
        }
    }

    void FaceMoveDirection()
    {
        if (moveDirection.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(moveDirection);
        lastFaceDirection = moveDirection;
    }

    bool FaceNearestEnemyInRange(float range)
    {
        Transform target = FindNearestEnemy(range);
        if (target == null)
            return false;

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return false;

        dir.Normalize();
        transform.rotation = Quaternion.LookRotation(dir);
        lastFaceDirection = dir;
        return true;
    }

    void LockRollDirection()
    {
        Vector3 dir = moveDirection.sqrMagnitude > 0.0001f ? moveDirection : lastFaceDirection;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        dir.Normalize();
        lockedFaceDirection = dir;
        lastFaceDirection = dir;
        lockFacing = true;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    void FaceLockedDirection()
    {
        if (lockedFaceDirection.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(lockedFaceDirection);
    }

    Transform FindNearestEnemy(float range)
    {
#if UNITY_2023_1_OR_NEWER
        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
#else
        EnemyBase[] enemies = Object.FindObjectsOfType<EnemyBase>();
#endif
        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead)
                continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist > range)
                continue;

            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    public void RotateChar()
    {
        UpdateMoveDirection();
        FaceMoveDirection();
    }

    void LateUpdate()
    {
        anim.ResetTrigger("attack");
    }

    protected override float GetFrozenAnimSpeed()
    {
        return 0.7f;
    }

    protected override void OnDie()
    {
        allowRotate = false;
        NextAttack = false;
        lockFacing = false;
    }
}