using UnityEngine;

public class PlayerController : CombatActor
{
    public float rotSpeed = 10f;
    public float moveSensitivity = 5f;
    public float tapSensitivity = 3f;

    float moveSpeed;
    float touchTime;
    float startTouchTime = 0f;
    float screenDiagonal;
    Vector2 touchStartPos;
    Vector2 touchMove;
    Vector3 moveDirection;

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
    }

    void Update()
    {
        if (dead) return;

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
                startTouchTime = Time.time;
            }

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                touchMove = touch.position;
                moveSpeed = Vector2.Distance(touchStartPos, touchMove) / screenDiagonal;
                moveSpeed = Mathf.Min(moveSpeed * moveSensitivity, 1f);
            }

            if (touch.phase == TouchPhase.Ended)
            {
                touchTime = Time.time - startTouchTime;
                moveSpeed = Vector2.Distance(touchStartPos, touchMove) / screenDiagonal;
                touchMove = touch.position;

                if (touchTime < 0.1f * tapSensitivity)
                {
                    if (moveSpeed < 0.1f)
                    {
                        if (NextAttack)
                            anim.SetTrigger("attack");
                    }
                    else
                    {
                        anim.SetTrigger("roll");
                    }
                }

                moveSpeed = 0;
            }

            anim.SetFloat("speed", moveSpeed);

            if (moveSpeed > 0.05f && allowRotate)
                RotateChar();
        }
    }

    public void RotateChar()
    {
        Vector2 dragDirection = touchMove - touchStartPos;
        moveDirection = new Vector3(dragDirection.x, 0, dragDirection.y);
        moveDirection = Camera.main.transform.TransformDirection(moveDirection);
        moveDirection.y = 0;

        if (moveDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveDirection);
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
    }
}