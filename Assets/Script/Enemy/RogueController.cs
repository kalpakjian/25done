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

    private int currentArrows;
    private bool isAiming = false;
    private bool isReloading = false;
    private bool isAttacking = false;
    private bool hasShotThisAttack = false;
    private Vector3 lockedAttackDirection = Vector3.forward;

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
        transform.rotation = Quaternion.LookRotation(lockedAttackDirection);

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

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up;
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
            lockedAttackDirection = Player.position - transform.position;
            lockedAttackDirection.y = 0f;
        }

        if (lockedAttackDirection.sqrMagnitude <= 0.001f)
            lockedAttackDirection = transform.forward;

        lockedAttackDirection.Normalize();
        transform.rotation = Quaternion.LookRotation(lockedAttackDirection);
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
