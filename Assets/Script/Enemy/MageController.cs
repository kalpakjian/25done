using UnityEngine;
using UnityEngine.AI;

public class MageController : Enemy
{
    [Header("Mage Specific Settings")]
    [Tooltip("法師希望與玩家保持的施法距離")]
    public float maintainDistance = 6f;

    [Header("Magic Missile Settings")]
    [Tooltip("魔法飛彈 Prefab（需要掛 MagicMissile.cs 與 Collider(isTrigger)）")]
    public GameObject magicMissilePrefab;

    [Tooltip("法術生成位置（例如法杖頂端），若未指定則用自身前上方")]
    public Transform castPoint;

    [Tooltip("單次發射消耗 mana")]
    public int manaCostPerShot = 1;

    [Tooltip("法術傷害倍率（會乘以 power）")]
    public float magicDamageMultiplier = 1f;

    [Tooltip("施法後鎖定移動時間")]
    public float attackLockTime = 1.0f;

    [Tooltip("真正 attack 開始後延遲幾秒發射 magic missile")]
    public float missileReleaseDelay = 0.25f;

    [Header("Mana Settings")]
    [Tooltip("最大 mana")]
    public int maxMana = 5;

    [Tooltip("目前 mana")]
    public int currentMana = 5;

    [Tooltip("回魔長施法所需秒數")]
    public float channelManaTime = 3f;

    [Tooltip("每次長施法後回復多少 mana；若想直接回滿可設成 maxMana")]
    public int manaRestoreAmount = 5;

    [Header("Aiming Settings")]
    [Tooltip("施法前瞄準時間")]
    public float aimTime = 0.6f;

    [Tooltip("瞄準期間轉向玩家速度")]
    public float aimTurnSpeed = 10f;

    [Tooltip("進入瞄準狀態的 Animator Trigger")]
    public string aimingTriggerName = "attack";

    [Tooltip("瞄準完成後正式施法 Trigger；若由 Exit Time 自動切換可留空")]
    public string attackTriggerName = "";

    [Header("Channel Settings")]
    [Tooltip("回魔用的 Animator Trigger")]
    public string channelTriggerName = "reload";

    [Header("Hurt / Stagger Settings")]
    [Tooltip("被玩家打中後停止移動/瞄準/施法的時間")]
    public float hurtStunTime = 0.45f;

    [Tooltip("是否忽略玩家攻擊造成的擊退")]
    public bool ignorePlayerPushback = true;

    [Tooltip("若沒有忽略擊退，承受擊退倍率")]
    public float receivedPushbackMultiplier = 0.35f;

    private bool isAiming = false;
    private bool isChannelingMana = false;
    private bool isAttacking = false;
    private bool hasCastThisAttack = false;
    private Vector3 lockedCastDirection = Vector3.forward;
    private float hurtStunEndTime = 0f;
    private float channelStartTime = 0f;

    void Start()
    {
        start();
        NM.updatePosition = true;
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
    }

    void Update()
    {
        update();
    }

    void LateUpdate()
    {
        if (AllowRotate)
            NM.updateRotation = true;
        else
            NM.updateRotation = false;
    }

    protected new void update()
    {
        if (dead) return;

        playerDist = Vector3.Distance(Player.position, transform.position);

        if (Time.time < hurtStunEndTime)
        {
            StopMoving();
            anim.SetBool("walk", false);
            return;
        }

        if (isAiming || isAttacking || isChannelingMana)
        {
            StopMoving();
            anim.SetBool("walk", false);
            return;
        }

        if (NM != null && NM.isOnNavMesh)
            NM.isStopped = false;

        float stopThreshold = 0.6f;

        if (playerDist < maintainDistance)
        {
            Vector3 directionAway = (transform.position - Player.position).normalized;
            Vector3 retreatTarget = Player.position + directionAway * (maintainDistance + 1f);
            NM.SetDestination(retreatTarget);
            anim.SetBool("walk", true);
        }
        else if (playerDist < maintainDistance + stopThreshold)
        {
            anim.SetBool("walk", false);
            NM.SetDestination(transform.position);
        }
        else if (playerDist < traceRange)
        {
            Vector3 dirToPlayer = (Player.position - transform.position).normalized;
            Vector3 approachTarget = Player.position - dirToPlayer * maintainDistance;
            NM.SetDestination(approachTarget);
            anim.SetBool("walk", true);
        }
        else
        {
            anim.SetBool("walk", false);
            NM.SetDestination(transform.position);
        }

        if (playerDist < maintainDistance + stopThreshold && Time.time >= nextAttackTime)
        {
            if (currentMana >= manaCostPerShot)
            {
                StartAiming();
            }
            else if (!isChannelingMana)
            {
                StartChannelMana();
            }
        }
    }

    protected override void Hurt(Attack attack)
    {
        bool isComboFinisher = attack.attackStep >= 3;

        if (dead || (!isComboFinisher && Time.time < nextHurtTime))
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

        if ((attack.canPushEnemy || attack.attackStep >= 3) && !ignorePlayerPushback)
        {
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                Vector3 pushBack = (transform.position - attack.position).normalized;
                pushBack.y = 0f;

                if (pushBack.sqrMagnitude < 0.001f)
                    pushBack = Vector3.forward;

                pushBack.Normalize();

                float pushSpeed = attack.attackStep >= 3 ? 0.4f
                                : attack.attackStep == 2 ? 0.25f
                                : 0.15f;

                pushBack *= Mathf.Max(attack.strength, 1) * pushSpeed * receivedPushbackMultiplier;
                body.AddForce(pushBack, ForceMode.VelocityChange);
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

        isAiming = false;
        isAttacking = false;
        isChannelingMana = false;
        hasCastThisAttack = false;

        CancelInvoke(nameof(FinishAiming));
        CancelInvoke(nameof(CastMissileFromAttack));
        CancelInvoke(nameof(OnAttackEnd));
        CancelInvoke(nameof(FinishChannelMana));

        StopMoving();
        anim.SetBool("walk", false);
    }

    void StartAiming()
    {
        isAiming = true;
        StopMoving();
        LockCastDirection();

        if (!string.IsNullOrEmpty(attackTriggerName))
            anim.ResetTrigger(attackTriggerName);

        if (!string.IsNullOrEmpty(aimingTriggerName))
            anim.SetTrigger(aimingTriggerName);

        Invoke(nameof(FinishAiming), aimTime);
    }

    void FinishAiming()
    {
        if (dead) return;

        isAiming = false;

        if (currentMana < manaCostPerShot)
        {
            StartChannelMana();
            return;
        }

        StopMoving();
        FaceCastDirectionHorizontally();

        if (!string.IsNullOrEmpty(attackTriggerName))
            anim.SetTrigger(attackTriggerName);

        nextAttackTime = Time.time + attackInterval;
        isAttacking = true;
        hasCastThisAttack = false;

        Invoke(nameof(CastMissileFromAttack), missileReleaseDelay);
        Invoke(nameof(OnAttackEnd), attackLockTime);
    }

    void CastMissileFromAttack()
    {
        if (!isAttacking || hasCastThisAttack)
            return;

        CastMagicMissile();
    }

    public void CastMagicMissile()
    {
        if (hasCastThisAttack) return;
        if (currentMana < manaCostPerShot) return;
        if (magicMissilePrefab == null)
        {
            Debug.LogWarning("[MageController] magicMissilePrefab 未設定！");
            return;
        }

        Vector3 spawnPos = GetMissileSpawnPosition();
        Vector3 dir = lockedCastDirection;

        GameObject missileObj = Instantiate(magicMissilePrefab, spawnPos, Quaternion.LookRotation(dir));
        hasCastThisAttack = true;

        MagicMissile missile = missileObj.GetComponent<MagicMissile>();
        if (missile != null)
        {
            missile.damage = magicDamageMultiplier * power;
            missile.direction = dir;
        }

        currentMana -= manaCostPerShot;

        if (currentMana < manaCostPerShot && !isChannelingMana)
        {
            StartChannelMana();
        }
    }

    void OnAttackEnd()
    {
        isAttacking = false;
        CancelInvoke(nameof(CastMissileFromAttack));
    }

    void LockCastDirection()
    {
        if (Player != null)
            lockedCastDirection = GetAimTargetPosition() - GetMissileSpawnPosition();

        if (lockedCastDirection.sqrMagnitude <= 0.001f)
            lockedCastDirection = transform.forward;

        lockedCastDirection.Normalize();
        FaceCastDirectionHorizontally();
    }

    void FaceCastDirectionHorizontally()
    {
        Vector3 bodyDirection = lockedCastDirection;
        bodyDirection.y = 0f;

        if (bodyDirection.sqrMagnitude <= 0.001f)
            bodyDirection = transform.forward;

        transform.rotation = Quaternion.LookRotation(bodyDirection.normalized);
    }

    Vector3 GetMissileSpawnPosition()
    {
        return castPoint != null ? castPoint.position : transform.position + transform.forward + Vector3.up * 1.2f;
    }

    Vector3 GetAimTargetPosition()
    {
        if (Player == null)
            return transform.position + transform.forward;

        Collider playerCollider = Player.GetComponent<Collider>();
        if (playerCollider != null)
            return playerCollider.bounds.center;

        CharacterController cc = Player.GetComponent<CharacterController>();
        if (cc != null)
            return cc.bounds.center;

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

    void StartChannelMana()
    {
        isChannelingMana = true;
        channelStartTime = Time.time;
        StopMoving();

        if (!string.IsNullOrEmpty(channelTriggerName))
            anim.SetTrigger(channelTriggerName);

        Invoke(nameof(FinishChannelMana), channelManaTime);
    }

    void FinishChannelMana()
    {
        currentMana = Mathf.Min(currentMana + manaRestoreAmount, maxMana);
        isChannelingMana = false;
    }

    float GetChannelRemaining()
    {
        float remaining = channelManaTime - (Time.time - channelStartTime);
        return Mathf.Clamp(remaining, 0f, channelManaTime);
    }

    void OnGUI()
    {
        if (dead) return;
        if (Camera.main == null) return;

        Vector3 worldPos = transform.position + Vector3.up * 2.5f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0) return;

        float guiY = Screen.height - screenPos.y;
        float w = 240f, h = 28f;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        string manaLabel = $"✨ Mana：{currentMana} / {maxMana}";
        style.normal.textColor = currentMana >= manaCostPerShot ? Color.cyan : Color.red;
        GUI.Label(new Rect(screenPos.x - w / 2f, guiY - h - 2f, w, h), manaLabel, style);

        if (isChannelingMana)
        {
            float remaining = GetChannelRemaining();
            string channelLabel = $"🔮 回魔施法中... ({remaining:F1}s)";
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(screenPos.x - w / 2f, guiY + 2f, w, h), channelLabel, style);
        }
    }
}