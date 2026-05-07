using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float rotSpeed = 10f;
    public float moveSensitivity = 3f;
    public float power = 1f;

    [Header("Tap / Roll")]
    public float tapMaxTime = 0.3f;
    public float tapMoveThreshold = 0.03f;
    public float rollMoveThreshold = 0.08f;
    public float rollPower = 0.4f;
    public float rollSpeedMultiplier = 1.5f;

    [Header("Auto Face Enemy")]
    public bool autoFaceEnemy = true;
    public float autoFaceRange = 10f;

    [HideInInspector] public bool AllowRotate = true;
    [HideInInspector] public bool NextAttack = true;

    private Animator anim;

    private float moveSpeed;
    private float touchStartTime;
    private float screenDiagonal;

    private Vector2 touchStartPos;
    private Vector2 currentTouchPos;
    private Vector3 moveDirection;

    private bool isRolling = false;
    private bool isTouching = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
    }

    void Update()
    {
        HandleTouchInput();

        anim.SetFloat("speed", moveSpeed);

        if (isTouching && moveSpeed > 0.05f && AllowRotate)
        {
            RotateChar();
        }

        if (autoFaceEnemy && AllowRotate && moveSpeed <= 0.05f && !isTouching)
        {
            AutoFaceNearestEnemy();
        }
    }

    void HandleTouchInput()
    {
        if (Input.touchCount != 1)
        {
            ResetTouchState();
            return;
        }

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                HandleTouchBegan(touch);
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                HandleTouchMoved(touch);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                HandleTouchEnded(touch);
                break;
        }
    }

    void HandleTouchBegan(Touch touch)
    {
        isTouching = true;
        touchStartPos = touch.position;
        currentTouchPos = touch.position;
        touchStartTime = Time.time;
        moveSpeed = 0f;
    }

    void HandleTouchMoved(Touch touch)
    {
        currentTouchPos = touch.position;

        float dragDistance = GetNormalizedDragDistance();
        moveSpeed = Mathf.Min(dragDistance * moveSensitivity, 1f);
    }

    void HandleTouchEnded(Touch touch)
    {
        currentTouchPos = touch.position;

        float touchDuration = Time.time - touchStartTime;
        float dragDistance = GetNormalizedDragDistance();

        if (touchDuration <= tapMaxTime)
        {
            if (dragDistance <= tapMoveThreshold)
            {
                TryAttack();
            }
            else if (dragDistance >= rollMoveThreshold)
            {
                TryRoll();
            }
        }

        ResetTouchState();
    }

    float GetNormalizedDragDistance()
    {
        return Vector2.Distance(touchStartPos, currentTouchPos) / screenDiagonal;
    }

    void TryAttack()
    {
        if (!NextAttack) return;

        anim.ResetTrigger("attack");
        anim.SetTrigger("attack");
    }

    void TryRoll()
    {
        anim.ResetTrigger("roll");
        anim.SetTrigger("roll");
        anim.speed = rollSpeedMultiplier;
        isRolling = true;
    }

    void ResetTouchState()
    {
        isTouching = false;
        moveSpeed = 0f;
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
        Vector2 dragDirection = currentTouchPos - touchStartPos;
        moveDirection = new Vector3(dragDirection.x, 0, dragDirection.y);
        moveDirection = Camera.main.transform.TransformDirection(moveDirection);
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
    }

    void OnAnimatorMove()
    {
        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);

        if (state.IsName("roll") || state.IsName("roll2") || state.IsName("roll3"))
        {
            transform.position += anim.deltaPosition * rollPower;
        }
        else
        {
            transform.position += anim.deltaPosition * power;

            if (isRolling)
            {
                anim.speed = 1f;
                isRolling = false;
            }
        }
    }
}