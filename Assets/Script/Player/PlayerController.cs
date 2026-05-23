using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float rotSpeed = 10f;
    public float moveSensitivity = 6f;
    public float power = 1f;
    public float runPowerMultiplier = 1.5f;

    [Header("Tap / Roll")]
    public float tapMaxTime = 0.3f;
    public float tapMoveThreshold = 0.03f;
    public float rollMoveThreshold = 0.08f;
    public float rollPower = 0.4f;
    public float rollSpeedMultiplier = 1.5f;

    [Header("Hold Detection Debug")]
    public float holdThreshold = 0.35f;
    public float chargeThreshold = 0.9f;
    public float holdMoveThreshold = 0.025f;

    [Header("Auto Face Enemy")]
    public bool autoFaceEnemy = true;
    public float autoFaceRange = 10f;

    [HideInInspector] public bool AllowRotate = true;
    [HideInInspector] public bool NextAttack = true;
    [HideInInspector] public bool CurrentAttackHit = false;
    [HideInInspector] public int CurrentAttackStep = 0;

    [Header("2H Combo Miss States")]
    [Tooltip("揮空時可隨機跳到的 2H 狀態名稱（在 Animator 裡叫什麼就填什麼）")]
    public string[] missAttackStateNames = { "2H1", "2H2", "2H3", "2H4", "2H5" };

    [Tooltip("打中後要接的下一個攻擊狀態（通常是 attack02）")]
    public string hitContinueStateName = "attack02";

    private Animator anim;

    private float moveSpeed;
    private float touchStartTime;
    private float screenDiagonal;

    private Vector2 touchStartPos;
    private Vector2 currentTouchPos;
    private Vector3 moveDirection;

    private bool isRolling = false;
    private bool isTouching = false;

    private bool holdTriggered = false;

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

        holdTriggered = false;
    }

    void HandleTouchMoved(Touch touch)
    {
        currentTouchPos = touch.position;

        float dragDistance = GetNormalizedDragDistance();
        float holdTime = Time.time - touchStartTime;

        if (!holdTriggered && holdTime >= holdThreshold && dragDistance <= holdMoveThreshold)
        {
            holdTriggered = true;
            Debug.Log("Guard");
        }

        moveSpeed = Mathf.Min(dragDistance * moveSensitivity, 2f);
    }

    void HandleTouchEnded(Touch touch)
    {
        currentTouchPos = touch.position;

        float touchDuration = Time.time - touchStartTime;
        float dragDistance = GetNormalizedDragDistance();

        bool isTap = touchDuration <= tapMaxTime && dragDistance <= tapMoveThreshold;
        bool isRoll = touchDuration <= tapMaxTime && dragDistance >= rollMoveThreshold;
        bool isHoldStill = touchDuration >= holdThreshold && dragDistance <= holdMoveThreshold;
        bool isCharge = touchDuration >= chargeThreshold && dragDistance <= holdMoveThreshold;

        if (isCharge)
        {
            Debug.Log("Charge");
        }
        else if (isTap)
        {
            TryAttack();
        }
        else if (isRoll)
        {
            TryRoll();
        }
        else if (isHoldStill)
        {
            // 已經在 HandleTouchMoved 中輸出過 Guard，這裡不重複輸出
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

        AnimatorStateInfo s = anim.GetCurrentAnimatorStateInfo(0);
        bool inMissEligible = IsInMissEligibleState(s);
        bool wasHit = CurrentAttackHit;   // 先記錄，後面才清除

        CurrentAttackHit = false;
        NextAttack = false;
        anim.ResetTrigger("attack");

        if (inMissEligible)
        {
            if (wasHit)
            {
                // 打中 → 直接接 attack02（或 hitContinueStateName 設定的狀態）
                anim.CrossFade(hitContinueStateName, 0.05f);
            }
            else
            {
                // 揮空 → 隨機一個 2H 動作
                int idx = Random.Range(0, missAttackStateNames.Length);
                anim.CrossFade(missAttackStateNames[idx], 0.05f);
            }
        }
        else
        {
            // 正常起始攻擊（從 Idle 或 attack02/03 開始）
            anim.SetTrigger("attack");
        }
    }

    /// <summary>
    /// 判斷當前是否在「揮空可接 2H」的狀態（attack01 或任何 2H state）
    /// </summary>
    bool IsInMissEligibleState(AnimatorStateInfo s)
    {
        if (s.IsName("attack01") || s.IsName("meleeattack01"))
            return true;
        foreach (string name in missAttackStateNames)
            if (s.IsName(name))
                return true;
        return false;
    }

    public void RegisterAttackHit()
    {
        CurrentAttackHit = true;
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
        holdTriggered = false;
    }

    void AutoFaceNearestEnemy()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();

        Enemy closest = null;
        float minDist = Mathf.Infinity;

        foreach (var enemy in enemies)
        {
            if (!enemy.gameObject.activeInHierarchy) continue;

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

        if (dragDirection.sqrMagnitude < 1f)
            return;

        moveDirection = new Vector3(dragDirection.x, 0f, dragDirection.y);
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
            float currentPower = (moveSpeed >= 0.5f) ? power * runPowerMultiplier : power;
            transform.position += anim.deltaPosition * currentPower;

            if (isRolling)
            {
                anim.speed = 1f;
                isRolling = false;
            }
        }
    }
}