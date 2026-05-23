using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class MinionController : MonoBehaviour
{
    private enum State
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    [Header("References")]
    [SerializeField] private Animator anim;          // 子模型上的 Animator
    [SerializeField] private NavMeshAgent agent;     // 根物件上的 NavMeshAgent
    [SerializeField] private Transform player;       // 可手動指定，不指定就自動找 Tag=Player
    [SerializeField] private Image hpBar;            // 可手動指定，不指定就自動找名字 HPBar

    [Header("Animator Parameters")]
    [SerializeField] private string walkBoolName = "walk";
    [SerializeField] private string attackTriggerName = "attack";
    [SerializeField] private string hurtTriggerName = "hurt";
    [SerializeField] private string dieTriggerName = "die";

    [Header("Combat")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float attackRange = 1.25f;
    [SerializeField] private float traceRange = 7f;
    [SerializeField] private float attackInterval = 1.5f;
    [SerializeField] private float attackPrepareTime = 0.2f;

    [Header("Movement")]
    [SerializeField] private float faceRotateSpeed = 720f;
    [SerializeField] private float walkVelocityThreshold = 0.05f;
    [SerializeField] private bool stopMovingWhileAttack = true;

    [Header("Death")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 5f;
    [SerializeField] private GameObject rewardItem;

    private State state = State.Idle;
    private float hp;
    private float nextAttackTime;
    private float attackReadyTime = -1f;
    private bool dead;

    public bool IsDead => dead;

    private void Start()
    {
        hp = maxHP;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (anim == null)
            anim = GetComponentInChildren<Animator>(true);

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (hpBar == null)
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                if (t.name == "HPBar")
                {
                    hpBar = t.GetComponent<Image>();
                    break;
                }
            }
        }

        if (hpBar != null)
            hpBar.fillAmount = 1f;

        if (agent != null)
            agent.stoppingDistance = attackRange * 0.9f;

        if (anim != null)
            anim.applyRootMotion = false;

        SetWalk(false);
        ChangeState(State.Idle);
    }

    private void Update()
    {
        if (dead)
            return;

        if (player == null || agent == null || anim == null || !agent.isOnNavMesh)
            return;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Idle:
                UpdateIdle(dist);
                break;

            case State.Chase:
                UpdateChase(dist);
                break;

            case State.Attack:
                UpdateAttack(dist);
                break;
        }

        UpdateWalkAnimation();
    }

    void UpdateIdle(float dist)
    {
        SetWalk(false);
        agent.isStopped = true;
        agent.ResetPath();

        if (dist <= traceRange)
            ChangeState(State.Chase);
    }

    void UpdateChase(float dist)
    {
        if (dist > traceRange)
        {
            ChangeState(State.Idle);
            return;
        }

        agent.isStopped = false;
        agent.stoppingDistance = attackRange * 0.9f;
        agent.SetDestination(player.position);

        FacePlayerSmooth(player.position);

        if (dist <= attackRange)
        {
            attackReadyTime = Time.time + attackPrepareTime;
            ChangeState(State.Attack);
        }
    }

    void UpdateAttack(float dist)
    {
        FacePlayerSmooth(player.position);

        if (dist > attackRange + 0.35f)
        {
            attackReadyTime = -1f;
            ChangeState(State.Chase);
            return;
        }

        if (stopMovingWhileAttack)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        SetWalk(false);

        if (attackReadyTime < 0f)
            attackReadyTime = Time.time + attackPrepareTime;

        if (Time.time >= attackReadyTime && Time.time >= nextAttackTime)
        {
            anim.ResetTrigger(attackTriggerName);
            anim.SetTrigger(attackTriggerName);

            nextAttackTime = Time.time + attackInterval;
            attackReadyTime = Time.time + attackInterval;
        }
    }

    void UpdateWalkAnimation()
    {
        if (state == State.Dead || state == State.Attack)
        {
            SetWalk(false);
            return;
        }

        bool isMoving = false;

        if (agent != null && agent.isOnNavMesh && !agent.isStopped)
        {
            if (!agent.pathPending)
            {
                isMoving =
                    agent.remainingDistance > agent.stoppingDistance + 0.05f &&
                    agent.velocity.sqrMagnitude > walkVelocityThreshold * walkVelocityThreshold;
            }
        }

        SetWalk(isMoving);
    }

    void ChangeState(State newState)
    {
        if (state == newState)
            return;

        state = newState;

        switch (state)
        {
            case State.Idle:
                if (agent != null)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }
                SetWalk(false);
                break;

            case State.Chase:
                if (agent != null)
                    agent.isStopped = false;
                break;

            case State.Attack:
                SetWalk(false);
                break;

            case State.Dead:
                if (agent != null)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }
                SetWalk(false);
                break;
        }
    }

    void SetWalk(bool value)
    {
        if (anim != null && !string.IsNullOrEmpty(walkBoolName))
            anim.SetBool(walkBoolName, value);
    }

    void FacePlayerSmooth(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            faceRotateSpeed * Time.deltaTime
        );
    }

    public void Hurt(float damage)
    {
        if (dead)
            return;

        hp -= damage;

        if (hpBar != null)
            hpBar.fillAmount = Mathf.Clamp01(hp / maxHP);

        if (hp <= 0f)
        {
            Die();
            return;
        }

        if (anim != null && !string.IsNullOrEmpty(hurtTriggerName))
            anim.SetTrigger(hurtTriggerName);
    }

    public void Hurt(Attack attack)
    {
        if (attack == null)
            return;

        Hurt(attack.damage);
    }

    void Die()
    {
        if (dead)
            return;

        dead = true;
        ChangeState(State.Dead);

        if (anim != null && !string.IsNullOrEmpty(dieTriggerName))
            anim.SetTrigger(dieTriggerName);

        if (rewardItem != null)
            Instantiate(rewardItem, transform.position, Quaternion.identity);

        if (hpBar != null && hpBar.transform.parent != null)
            hpBar.transform.parent.gameObject.SetActive(false);

        gameObject.layer = 0;

        EnemyWeapon[] weapons = GetComponentsInChildren<EnemyWeapon>();
        foreach (EnemyWeapon weapon in weapons)
            weapon.attackDamage = 0;

        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }
}