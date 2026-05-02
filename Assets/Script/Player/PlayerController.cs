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
		}
	}

	void LateUpdate()
	{
		anim.ResetTrigger("attack");
	}
}
