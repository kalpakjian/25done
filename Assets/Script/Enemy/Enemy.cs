using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;

public class Enemy : MonoBehaviour {

	protected Image HPBar;
	
	public 	float traceRange=5;

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

	protected void start () {

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
		NM.updatePosition=false;
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

	protected void update () {
		if (dead) return;   // 死亡後停止 AI 更新
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
			NM.SetDestination(Player.position);
			anim.SetBool("walk", true);
		}
		else
		{
			anim.SetBool("walk", false);
			NM.SetDestination(transform.position);
		}
	}

	protected void lateUpdate()
	{
		if (dead) return;   // 死亡後停止，避免 NM 被禁用後報錯

		NM.nextPosition = transform.position;

		if (AllowRotate)
			NM.updateRotation = true;
		else
			NM.updateRotation = false;
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

			Vector3 pushBack = (transform.position - attack.position).normalized;
			pushBack *= attack.strength;
			GetComponent<Rigidbody>().AddForce(pushBack * 10, ForceMode.Impulse);

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

		// 死亡後立即停止轉向與移動
		AllowRotate = false;
		NM.updateRotation = false;
		NM.velocity = Vector3.zero;
		NM.isStopped = true;
		NM.enabled = false;

		// 凍結 Rigidbody，讓屍體不再被物理推動
		Rigidbody rb = GetComponent<Rigidbody>();
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
        foreach(var mat in material)
        {
			Destroy(mat);
        }
    }

}
