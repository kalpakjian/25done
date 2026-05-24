using UnityEngine;
using System.Collections.Generic;

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
    public float enemyRefreshInterval = 0.25f;

    [HideInInspector] public bool AllowRotate = true;
    [HideInInspector] public bool NextAttack = true;
    [HideInInspector] public bool CurrentAttackHit = false;
    [HideInInspector] public int CurrentAttackStep = 0;

    [Header("2H Combo Miss States")]
    public string[] missAttackStateNames =
    {
        "Base Layer.2H1",
        "Base Layer.2H2",
        "Base Layer.2H3",
        "Base Layer.2H4",
        "Base Layer.2H5"
    };

    public string hitContinueStateName = "Base Layer.attack02";

    private Animator anim;
    private Transform camTf;

    private float moveSpeed;
    private float touchStartTime;
    private float screenDiagonal;
    private float enemyRefreshTimer;

    private Vector2 touchStartPos;
    private Vector2 currentTouchPos;
    private Vector3 moveDirection;

    private bool isRolling = false;
    private bool isTouching = false;
    private bool holdTriggered = false;

    private Enemy cachedClosestEnemy;
    private readonly List<Enemy> enemyBuffer = new List<Enemy>();

    private static readonly int SpeedHash = Animator.StringToHash("speed");
    private static readonly int AttackHash = Animator.StringToHash("attack");
    private static readonly int RollHash = Animator.StringToHash("roll");

    void Start()
    {
        anim = GetComponent<Animator>();
        camTf = Camera.main != null ? Camera.main.transform : null;
        screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
        RefreshEnemyCache();
    }

    void Update()
    {
        HandleTouchInput();
        anim.SetFloat(SpeedHash, moveSpeed);

        if (isTouching && moveSpeed > 0.05f && AllowRotate)
        {
            RotateChar();
        }

        enemyRefreshTimer -= Time.deltaTime;
        if (enemyRefreshTimer <= 0f)
        {
            enemyRefreshTimer = enemyRefreshInterval;
            RefreshEnemyCache();
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

        moveSpeed = Mathf.Clamp(dragDistance * moveSensitivity, 0f, 2f);
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
        bool wasHit = CurrentAttackHit;

        CurrentAttackHit = false;
        NextAttack = false;
        anim.ResetTrigger(AttackHash);

        if (inMissEligible)
        {
            if (wasHit)
            {
                anim.CrossFade(hitContinueStateName, 0.05f);
            }
            else
            {
                if (missAttackStateNames != null && missAttackStateNames.Length > 0)
                {
                    int idx = Random.Range(0, missAttackStateNames.Length);
                    anim.CrossFade(missAttackStateNames[idx], 0.05f);
                }
                else
                {
                    anim.SetTrigger(AttackHash);
                }
            }
        }
        else
        {
            anim.SetTrigger(AttackHash);
        }
    }

    bool IsInMissEligibleState(AnimatorStateInfo s)
    {
        if (s.IsName("Base Layer.attack01") || s.IsName("Base Layer.meleeattack01"))
            return true;

        if (missAttackStateNames == null) return false;

        foreach (string name in missAttackStateNames)
        {
            if (!string.IsNullOrEmpty(name) && s.IsName(name))
                return true;
        }

        return false;
    }

    public void RegisterAttackHit()
    {
        CurrentAttackHit = true;
    }

    public void OpenNextAttackWindow()
    {
        NextAttack = true;
    }

    public void CloseNextAttackWindow()
    {
        NextAttack = false;
    }

    void TryRoll()
    {
        anim.ResetTrigger(RollHash);
        anim.SetTrigger(RollHash);
        anim.speed = rollSpeedMultiplier;
        isRolling = true;
    }

    void ResetTouchState()
    {
        isTouching = false;
        moveSpeed = 0f;
        holdTriggered = false;
    }

    void RefreshEnemyCache()
    {
        enemyBuffer.Clear();
        enemyBuffer.AddRange(FindObjectsOfType<Enemy>());

        cachedClosestEnemy = null;
        float minDist = Mathf.Infinity;

        for (int i = 0; i < enemyBuffer.Count; i++)
        {
            Enemy enemy = enemyBuffer[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < autoFaceRange && dist < minDist)
            {
                minDist = dist;
                cachedClosestEnemy = enemy;
            }
        }
    }

    void AutoFaceNearestEnemy()
    {
        if (cachedClosestEnemy == null) return;

        Vector3 lookPos = cachedClosestEnemy.transform.position;
        lookPos.y = transform.position.y;

        Vector3 dir = lookPos - transform.position;
        if (dir.sqrMagnitude <= 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSpeed * Time.deltaTime);
    }

    public void RotateChar()
    {
        Vector2 dragDirection = currentTouchPos - touchStartPos;

        if (dragDirection.sqrMagnitude < 1f || camTf == null)
            return;

        moveDirection = new Vector3(dragDirection.x, 0f, dragDirection.y);
        moveDirection = camTf.TransformDirection(moveDirection);
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection.normalized);
        }
    }

    void OnAnimatorMove()
    {
        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);

        if (state.IsName("Base Layer.roll") || state.IsName("Base Layer.roll2") || state.IsName("Base Layer.roll3"))
        {
            transform.position += anim.deltaPosition * rollPower;
        }
        else
        {
            float currentPower = (moveSpeed >= 0.5f) ? power * runPowerMultiplier : power;
            transform.position += anim.deltaPosition * currentPower;
        }

        if (isRolling && !state.IsName("Base Layer.roll") && !state.IsName("Base Layer.roll2") && !state.IsName("Base Layer.roll3"))
        {
            anim.speed = 1f;
            isRolling = false;
        }
    }
}