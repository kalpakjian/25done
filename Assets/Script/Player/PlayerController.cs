using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{

	public float rotSpeed = 10;
	public float moveSensitivity = 3;
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
	public float rollPower = 0.4f;
	public float rollSpeedMultiplier = 1.5f;

	private bool isRolling = false;

	[Header("Auto Face Enemy")]
	public bool autoFaceEnemy = true;
	public float autoFaceRange = 10f;

	void Start()
	{
		anim = GetComponent<Animator>();
		screenDiagonal = Mathf.Sqrt(Mathf.Pow(Screen.width, 2) + Mathf.Pow(Screen.height, 2));
	}

	void Update()
	{
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
				{
					anim.SetTrigger("roll");
					anim.speed = rollSpeedMultiplier;
					isRolling = true;
				}
				}
				moveSpeed = 0;
			}

			anim.SetFloat("speed", moveSpeed);

			if (moveSpeed > 0.05f && AllowRotate)
				RotateChar();
		}

		// 自動面向最近的存活 Enemy（在移動時不覆蓋手動轉向）
		if (autoFaceEnemy && AllowRotate && moveSpeed <= 0.05f)
		{
			AutoFaceNearestEnemy();
		}
	}

	void AutoFaceNearestEnemy()
	{
		Enemy[] enemies = FindObjectsOfType<Enemy>();

		Enemy closest = null;
		float minDist = Mathf.Infinity;

		foreach (var enemy in enemies)
		{
			if (enemy.IsDead) continue;

			float dist = Vector3.Distance(transform.position, enemy.transform.position);
			if (dist < autoFaceRange && dist < minDist)
			{
				minDist = dist;
				closest = enemy;
			}
		}

		if (closest != null)
		{
			Vector3 lookPos = closest.transform.position;
			lookPos.y = transform.position.y;
			Quaternion targetRot = Quaternion.LookRotation(lookPos - transform.position);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSpeed * Time.deltaTime);
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

	void OnAnimatorMove()
	{
		if (anim.GetCurrentAnimatorStateInfo(0).IsName("roll") ||
		    anim.GetCurrentAnimatorStateInfo(0).IsName("roll2") ||
		    anim.GetCurrentAnimatorStateInfo(0).IsName("roll3"))
		{
			// 滾動狀態：用 rollPower 縮小移動距離
			transform.position += anim.deltaPosition * rollPower;
		}
		else
		{
			// 其他狀態（走路等）：正常套用 root motion
			transform.position += anim.deltaPosition * power;

			// roll 結束後重置動畫速度
			if (isRolling)
			{
				anim.speed = 1.0f;
				isRolling = false;
			}
		}
	}

	void LateUpdate()
	{
		anim.ResetTrigger("attack");
	}
}
