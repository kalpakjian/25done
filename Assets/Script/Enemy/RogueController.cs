using UnityEngine;
using UnityEngine.AI;

public class RogueController : Enemy
{
    [Header("Rogue Specific Settings")]
    [Tooltip("The distance the Rogue tries to maintain from the player when attacking.")]
    public float maintainDistance = 4f;

    [Header("Arrow Settings")]
    [Tooltip("箭矢 Prefab（需要掛 Arrow.cs 與 Collider(isTrigger)）")]
    public GameObject arrowPrefab;

    [Tooltip("箭矢生成位置（例如弓的位置），若未指定則用自身位置")]
    public Transform firePoint;

    [Tooltip("最大箭矢數量")]
    public int maxArrows = 5;

    [Tooltip("上膛（裝填）所需秒數")]
    public float reloadTime = 2f;

    [Tooltip("箭矢傷害倍率（會乘以 power）")]
    public float arrowDamageMultiplier = 1f;

    [Tooltip("射箭動作鎖定移動的時間（秒），建議與射箭動畫長度相近")]
    public float attackLockTime = 1.0f;

    [Tooltip("真正 attack 開始後，延遲幾秒自動射箭。若新 attack 動畫沒有 ShootArrow Event，這個會負責射箭。")]
    public float arrowReleaseDelay = 0.25f;

    [Header("Aiming Settings")]
    [Tooltip("射箭前瞄準動作所需秒數，完成後才會真正播放攻擊動畫")]
    public float aimTime = 0.6f;

    [Tooltip("瞄準期間轉向玩家的速度")]
    public float aimTurnSpeed = 10f;

    [Tooltip("進入瞄準狀態使用的 Animator Trigger。若你把原本 attack 狀態改成 aiming，這裡保持 attack 即可。")]
    public string aimingTriggerName = "attack";

    [Tooltip("瞄準完成後要觸發的新攻擊 Animator Trigger。若 aiming 會用 Exit Time 自動轉到新 attack，這裡留空。")]
    public string attackTriggerName = "";

    [Header("Hurt / Stagger Settings")]
    [Tooltip("弓箭手被玩家打中後停止移動/瞄準/射箭的時間，避免一受擊就退開導致連段落空。")]
    public float hurtStunTime = 0.45f;

    [Tooltip("是否忽略玩家攻擊造成的擊退。開啟後弓箭手被打中會硬直但不會被推開。")]
    public bool ignorePlayerPushback = true;

    [Tooltip("若沒有忽略擊退，弓箭手承受的擊退倍率。")]
    public float receivedPushbackMultiplier = 0.35f;

    private int currentArrows;
    private bool isAiming = false;
    private bool isReloading = false;
    private bool isAttacking = false;
    private bool hasShotThisAttack = false;
    private Vector3 lockedAttackDirection = Vector3.forward;
    private float hurtStunEndTime = 0f;

    void Start()
    {
        start();
        // Let NavMeshAgent drive position for Rogue (no root motion)
        NM.updatePosition = true;
        currentArrows = maxArrows;
    }

    void Update()
    {
        update();
    }

    void LateUpdate()
    {
        // Override: skip NM.nextPosition sync so NavMeshAgent can move freely
        if (AllowRotate)
            NM.updateRotation = true;
        else
            NM.updateRotation = false;
    }

    protected override void update()
    {
        // 死亡後停止所有行為
        if (dead) return;

        playerDist = Vector3.Distance(Player.position, transform.position);

        if (Time.time < hurtStunEndTime)
        {
            StopMoving();
            anim.SetBool("walk", false);
            return;
        }

        // 瞄準、射箭或上膛時，原地鎖定不移動
        if (isAiming || isAttacking || isReloading)
        {
            StopMoving();
            anim.SetBool("walk", false);
            return;
        }

        if (NM != null && NM.isOnNavMesh)
            NM.isStopped = false;

        float stopThreshold = 0.5f;

        // Movement logic
        if (playerDist < maintainDistance)
        {
            // Player is too close — retreat away from player
            Vector3 directionAway = (transform.position - Player.position).normalized;
            Vector3 retreatTarget = Player.position + directionAway * (maintainDistance + 1f);
            NM.SetDestination(retreatTarget);
            anim.SetBool("walk", true);
        }
        else if (playerDist < maintainDistance + stopThreshold)
        {
            // At preferred distance — hold position
            anim.SetBool("walk", false);
            NM.SetDestination(transform.position);
        }
        else if (playerDist < traceRange)
        {
            // Within trace range but too far — approach to maintainDistance
            Vector3 dirToPlayer = (Player.position - transform.position).normalized;
            Vector3 approachTarget = Player.position - dirToPlayer * maintainDistance;
            NM.SetDestination(approachTarget);
            anim.SetBool("walk", true);
        }
        else
        {
            // Player is too far — idle
            anim.SetBool("walk", false);
            NM.SetDestination(transform.position);
        }

        // Attack from maintainDistance (ranged attacker)
        if (playerDist < maintainDistance + stopThreshold && Time.time >= nextAttackTime)
        {
            if (currentArrows > 0)
            {
                // 有箭 → 先播放瞄準動作，瞄準完成後才射箭
                StartAiming();
            }
            else if (!isReloading)
            {
                // 沒箭 → 開始上膛
                StartReload();
            }
        }
    }

    // ─────────────────────────────────────────────
    //  射箭 – 由 Animation Event 在動畫適當幀呼叫
    // ─────────────────────────────────────────────
    protected override void Hurt(Attack attack)
    {
        if (dead || Time.time < nextHurtTime)
            return;

        SlowMotion.Slow(0.1f, 5);
        HP -= attack.damage;
        if (HPBar) HPBar.fillAmount = HP / maxHP;

        for (int i = 0; i < material.Count; i++)
            material[i].SetColor("_EmissionColor", Color.white);

        Invoke(nameof(StopEmission), 0.05f);

        if (HP <= 0)
        {
            Die();
            return;
        }

        EnterHurtStun();
        anim.SetTrigger("hurt");

        if (attack.canPushEnemy && !ignorePlayerPushback)
        {
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                Vector3 pushBack = (transform.position - attack.position).normalized;
                pushBack *= attack.strength * receivedPushbackMultiplier;
                body.AddForce(pushBack * 10, ForceMode.Impulse);
            }
        }

        if (attack.type == AttackType.frozen)
        {
            for (int i = 0; i < material.Count; i++)
                material[i].color = frozenColor;
            anim.speed = 0.5f;
            Invoke(nameof(Recover), 5);
        }

        nextHurtTime = Time.time + hurtInterval;
        nextAttackTime = Time.time + Mathf.Max(attackInterval, hurtStunTime);
    }

    void EnterHurtStun()
    {
        hurtStunEndTime = Time.time + hurtStunTime;

        // 被打中時中斷正在進行的瞄準/射擊，避免硬直中還把箭射出去。
        isAiming = false;
        isAttacking = false;
        hasShotThisAttack = false;
        CancelInvoke(nameof(FinishAiming));
        CancelInvoke(nameof(ShootArrowFromAttack));
        CancelInvoke(nameof(OnAttackEnd));

        StopMoving();
        anim.SetBool("walk", false);
    }

    void StartAiming()
    {
        isAiming = true;
        StopMoving();
        LockAttackDirection();

        if (!string.IsNullOrEmpty(attackTriggerName))
            anim.ResetTrigger(attackTriggerName);

        if (!string.IsNullOrEmpty(aimingTriggerName))
            anim.SetTrigger(aimingTriggerName);

        Invoke(nameof(FinishAiming), aimTime);
    }

    void FinishAiming()
    {
        if (dead)
            return;

        isAiming = false;

        if (currentArrows <= 0)
        {
            StartReload();
            return;
        }

        StopMoving();
        FaceAttackDirectionHorizontally();

        // 瞄準完成 → 進入真正射箭階段。
        // 若 attackTriggerName 留空，代表 Animator 會由 aiming 的 Exit Time 自動轉到新 attack。
        if (!string.IsNullOrEmpty(attackTriggerName))
            anim.SetTrigger(attackTriggerName);

        nextAttackTime = Time.time + attackInterval;
        isAttacking = true;
        hasShotThisAttack = false;
        Invoke(nameof(ShootArrowFromAttack), arrowReleaseDelay);
        Invoke(nameof(OnAttackEnd), attackLockTime);
    }

    void ShootArrowFromAttack()
    {
        if (!isAttacking || hasShotThisAttack)
            return;

        ShootArrow();
    }

    public void ShootArrow()
    {
        if (hasShotThisAttack) return;
        if (currentArrows <= 0) return;
        if (arrowPrefab == null)
        {
            Debug.LogWarning("[RogueController] arrowPrefab 未設定！");
            return;
        }

        Vector3 spawnPos = GetArrowSpawnPosition();
        Vector3 dir = lockedAttackDirection;

        GameObject arrowObj = Instantiate(arrowPrefab, spawnPos, Quaternion.LookRotation(dir));
        hasShotThisAttack = true;

        Arrow arrow = arrowObj.GetComponent<Arrow>();
        if (arrow != null)
        {
            arrow.damage = arrowDamageMultiplier * power;
            arrow.direction = dir;
        }

        currentArrows--;

        // 箭用完自動開始上膛
        if (currentArrows <= 0 && !isReloading)
        {
            StartReload();
        }
    }

    // ─────────────────────────────────────────────
    //  上膛（裝填箭矢）
    // ─────────────────────────────────────────────
    void OnAttackEnd()
    {
        isAttacking = false;
        CancelInvoke(nameof(ShootArrowFromAttack));
    }

    void LockAttackDirection()
    {
        if (Player != null)
        {
            // 從真正出箭位置瞄準玩家 Collider 中心，而不是只用腳底座標與水平線。
            // 這可避免玩家站著不動時，箭因高度差從身體上/下方擦過。
            lockedAttackDirection = GetAimTargetPosition() - GetArrowSpawnPosition();
        }

        if (lockedAttackDirection.sqrMagnitude <= 0.001f)
            lockedAttackDirection = transform.forward;

        lockedAttackDirection.Normalize();

        FaceAttackDirectionHorizontally();
    }

    void FaceAttackDirectionHorizontally()
    {
        Vector3 bodyDirection = lockedAttackDirection;
        bodyDirection.y = 0f;
        if (bodyDirection.sqrMagnitude <= 0.001f)
            bodyDirection = transform.forward;

        transform.rotation = Quaternion.LookRotation(bodyDirection.normalized);
    }

    Vector3 GetArrowSpawnPosition()
    {
        return firePoint != null ? firePoint.position : transform.position + Vector3.up;
    }

    Vector3 GetAimTargetPosition()
    {
        if (Player == null)
            return transform.position + transform.forward;

        Collider playerCollider = Player.GetComponent<Collider>();
        if (playerCollider != null)
            return playerCollider.bounds.center;

        CharacterController characterController = Player.GetComponent<CharacterController>();
        if (characterController != null)
            return characterController.bounds.center;

        // 沒有 Collider 時，至少瞄準身體中段而非腳底。
        return Player.position + Vector3.up;
    }

    void StopMoving()
    {
        if (NM == null || !NM.isOnNavMesh)
            return;

        NM.isStopped = true;
        NM.velocity = Vector3.zero;
        NM.SetDestination(transform.position);
    }

    void StartReload()
    {
        isReloading = true;
        reloadStartTime = Time.time;        // 記錄開始時間，用於顯示倒數
        anim.SetTrigger("reload");          // 播放上膛/裝填動畫（若有）
        Invoke(nameof(FinishReload), reloadTime);
    }

    void FinishReload()
    {
        currentArrows = maxArrows;
        isReloading = false;
    }

    // ─────────────────────────────────────────────
    //  在怪物頭頂顯示上膛狀態文字（僅 Debug 用途）
    // ─────────────────────────────────────────────
    void OnGUI()
    {
        if (dead) return;
        if (Camera.main == null) return;

        Vector3 worldPos = transform.position + Vector3.up * 2.5f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // 物體在攝影機後面則不顯示
        if (screenPos.z < 0) return;

        // Unity GUI 的 Y 軸是反的
        float guiY = Screen.height - screenPos.y;
        float w = 220f, h = 28f;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        // ── 第一行：箭矢數量（常態顯示）──
        string arrowLabel = $"🏹 箭矢：{currentArrows} / {maxArrows}";
        style.normal.textColor = currentArrows > 0 ? Color.white : Color.red;
        GUI.Label(new Rect(screenPos.x - w / 2f, guiY - h - 2f, w, h), arrowLabel, style);

        // ── 第二行：上膛倒數（僅上膛中顯示）──
        if (isReloading)
        {
            float remaining = CancelInvoke_GetRemaining();
            string reloadLabel = $"🔄 上膛中... ({remaining:F1}s)";
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(screenPos.x - w / 2f, guiY + 2f, w, h), reloadLabel, style);
        }
    }

    // 計算 FinishReload Invoke 剩餘秒數（用 reloadStartTime 追蹤）
    private float reloadStartTime = 0f;
    float CancelInvoke_GetRemaining()
    {
        float remaining = reloadTime - (Time.time - reloadStartTime);
        return Mathf.Clamp(remaining, 0f, reloadTime);
    }
}
