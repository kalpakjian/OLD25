using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{

	public float rotSpeed = 10;
	public float moveSensitivity = 5;
	public float tapSensitivity = 3;

	Animator anim;
	float moveSpeed;
	float touchTime;
	float startTouchTime = 0;
	float screenDiagonal;
	Vector2 touchStartPos;
	Vector2 touchMove;
	Vector3 moveDirection;

	[HideInInspector]
	public bool AllowRotate = true;
	[HideInInspector]
	public bool NextAttack = true;

	public float power = 1;

	public Faction faction = Faction.Player;
	public float maxHP = 200;
	public float hurtInterval = 0.3f;

	float HP;
	bool dead;
	float nextHurtTime = 0;

	public float frozenAnimSpeed = 0.7f;

	void Start()
	{
		anim = GetComponent<Animator>();
		screenDiagonal = Mathf.Sqrt(Mathf.Pow(Screen.width, 2) + Mathf.Pow(Screen.height, 2));
		HP = maxHP;
		dead = false;
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
				moveSpeed = Mathf.Min(moveSpeed * moveSensitivity, 1);
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
						anim.SetTrigger("roll");
				}
				moveSpeed = 0;
			}

			anim.SetFloat("speed", moveSpeed);

			if (moveSpeed > 0.05f && AllowRotate)
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

	public void Hurt(float damage)
	{
		AttackData attack = new AttackData();
		attack.attacker = null;
		attack.attackerFaction = Faction.Enemy;
		attack.damage = damage;
		attack.position = transform.position;
		attack.type = AttackType.normal;
		attack.strength = 0;

		TakeDamage(attack);
	}

	public virtual void TakeDamage(AttackData attack)
	{
		if (dead || Time.time < nextHurtTime)
			return;

		HP -= attack.damage;

		if (HP <= 0)
		{
			Die();
			return;
		}

		anim.SetTrigger("hurt");

		if (attack.strength > 0)
		{
			Rigidbody rb = GetComponent<Rigidbody>();
			if (rb)
			{
				Vector3 pushBack = (transform.position - attack.position).normalized;
				pushBack *= attack.strength;
				rb.AddForce(pushBack * 10, ForceMode.Impulse);
			}
		}

		if (attack.type == AttackType.frozen)
		{
			anim.speed = frozenAnimSpeed;
			Invoke("RecoverStatus", 5f);
		}

		nextHurtTime = Time.time + hurtInterval;
	}

	void RecoverStatus()
	{
		anim.speed = 1f;
	}

	void Die()
	{
		dead = true;
		AllowRotate = false;
		NextAttack = false;
		anim.SetTrigger("die");
	}
}
