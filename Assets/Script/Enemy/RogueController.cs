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

    private int currentArrows;
    private bool isReloading = false;

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
        playerDist = Vector3.Distance(Player.position, transform.position);

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
                // 有箭 → 播放射箭動畫（動畫事件會呼叫 ShootArrow）
                anim.SetTrigger("attack");
                nextAttackTime = Time.time + attackInterval;
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
    public void ShootArrow()
    {
        if (currentArrows <= 0) return;
        if (arrowPrefab == null)
        {
            Debug.LogWarning("[RogueController] arrowPrefab 未設定！");
            return;
        }

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up;
        Vector3 dir = (Player.position - spawnPos).normalized;

        GameObject arrowObj = Instantiate(arrowPrefab, spawnPos, Quaternion.LookRotation(dir));
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
    void StartReload()
    {
        isReloading = true;
        anim.SetTrigger("reload");          // 播放上膛/裝填動畫（若有）
        Invoke(nameof(FinishReload), reloadTime);
    }

    void FinishReload()
    {
        currentArrows = maxArrows;
        isReloading = false;
    }
}
