using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;

public class Enemy : MonoBehaviour
{
    protected Image HPBar;

    public float traceRange = 5;
    public float attackRange = 2f;
    public float attackInterval = 2f;
    public float power = 1;
    protected float nextAttackTime = 0;

    public float maxHP = 200;
    protected float HP;
    protected bool dead;
    public float hurtInterval = 0.3f;
    protected float nextHurtTime = 0;

    protected Transform Player;
    protected Animator anim;
    protected float playerDist;
    protected NavMeshAgent NM;
    protected Rigidbody rb;

    [HideInInspector]
    public bool AllowRotate = true;

    protected Color frozenColor = Color.cyan;
    protected Renderer[] rend;
    protected List<Material> material = new List<Material>();
    protected List<Color> oriColor = new List<Color>();

    public GameObject rewardItem;
    public UnityEvent dieEvent;

    [Header("Death Body")]
    public bool keepBodyAfterDeath = false;
    public float removeDelay = 5f;

    [Header("Knockback")]
    public float knockbackForceMultiplier = 10f;
    public float knockbackStopTime = 0.2f;
    protected bool isKnockback = false;
    protected Coroutine knockbackRoutine;

    protected void start()
    {
        var transforms = GetComponentsInChildren<Transform>();
        foreach (var tran in transforms)
        {
            if (tran.name == "HPBar")
            {
                HPBar = tran.GetComponent<Image>();
                HPBar.fillAmount = 1;
            }
        }

        Player = GameObject.FindWithTag("Player").transform;
        anim = GetComponent<Animator>();
        NM = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        if (NM != null)
            NM.updatePosition = false;

        HP = maxHP;
        dead = false;
        InitializeColor();
    }

    public void InitializeColor()
    {
        rend = GetComponentsInChildren<Renderer>();
        foreach (Renderer _rend in rend)
        {
            var mats = _rend.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                oriColor.Add(mats[i].color);
                material.Add(mats[i]);
                mats[i].EnableKeyword("_EMISSION");
            }
        }
    }

    protected void update()
    {
        if (dead) return;
        if (isKnockback) return;

        playerDist = Vector3.Distance(Player.position, transform.position);

        if (playerDist < attackRange)
        {
            anim.SetBool("walk", false);
            if (Time.time >= nextAttackTime)
            {
                anim.SetTrigger("attack");
                nextAttackTime = Time.time + attackInterval;
            }
        }
        else if (playerDist < traceRange)
        {
            if (NM != null && NM.enabled)
            {
                NM.isStopped = false;
                NM.SetDestination(Player.position);
            }
            anim.SetBool("walk", true);
        }
        else
        {
            anim.SetBool("walk", false);
            if (NM != null && NM.enabled)
            {
                NM.isStopped = true;
                NM.SetDestination(transform.position);
            }
        }
    }

    protected void lateUpdate()
    {
        if (dead) return;
        if (NM == null || !NM.enabled) return;

        if (!isKnockback)
        {
            NM.nextPosition = transform.position;
        }

        NM.updateRotation = AllowRotate && !isKnockback;
    }

    protected virtual void Hurt(Attack attack)
    {
        if (!dead && Time.time >= nextHurtTime)
        {
            SlowMotion.Slow(0.1f, 5);
            HP -= attack.damage;

            if (HPBar) HPBar.fillAmount = HP / maxHP;

            for (int i = 0; i < material.Count; i++)
                material[i].SetColor("_EmissionColor", Color.white);

            Invoke("StopEmission", 0.05f);

            if (HP <= 0)
            {
                Die();
                return;
            }

            anim.SetTrigger("hurt");

            ApplyHorizontalKnockback(attack);

            if (attack.type == AttackType.frozen)
            {
                for (int i = 0; i < material.Count; i++)
                    material[i].color = frozenColor;

                anim.speed = 0.5f;
                Invoke("Recover", 5);
            }

            nextHurtTime = Time.time + hurtInterval;
            nextAttackTime = Time.time + attackInterval;
        }
    }

    protected void ApplyHorizontalKnockback(Attack attack)
    {
        if (rb == null) return;

        Vector3 pushBack = transform.position - attack.position;
        pushBack.y = 0f;

        if (pushBack.sqrMagnitude <= 0.0001f)
            return;

        pushBack = pushBack.normalized * attack.strength;

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockbackRoutine(pushBack));
    }

    protected IEnumerator KnockbackRoutine(Vector3 pushDirection)
    {
        isKnockback = true;
        AllowRotate = false;

        if (NM != null && NM.enabled)
        {
            NM.isStopped = true;
            NM.updateRotation = false;
            NM.ResetPath();
        }

        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(pushDirection * knockbackForceMultiplier, ForceMode.Impulse);

        yield return new WaitForSeconds(knockbackStopTime);

        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        rb.angularVelocity = Vector3.zero;

        if (NM != null && NM.enabled)
        {
            NM.Warp(transform.position);
            NM.isStopped = false;
        }

        AllowRotate = true;
        isKnockback = false;
        knockbackRoutine = null;
    }

    protected void Recover()
    {
        for (int i = 0; i < material.Count; i++)
            material[i].color = oriColor[i];

        anim.speed = 1;
    }

    protected void StopEmission()
    {
        for (int i = 0; i < material.Count; i++)
            material[i].SetColor("_EmissionColor", Color.black);
    }

    protected virtual void Die()
    {
        if (rewardItem) Instantiate(rewardItem, transform.position, Quaternion.identity);

        dead = true;
        anim.SetTrigger("die");

        if (!keepBodyAfterDeath)
            Invoke("Remove", removeDelay);

        if (HPBar) HPBar.transform.parent.gameObject.SetActive(false);

        gameObject.layer = 0;
        AllowRotate = false;

        if (NM != null && NM.enabled)
        {
            NM.updateRotation = false;
            NM.velocity = Vector3.zero;
            NM.isStopped = true;
            NM.enabled = false;
        }

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        dieEvent.Invoke();
    }

    void Remove()
    {
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        foreach (var mat in material)
        {
            Destroy(mat);
        }
    }
}