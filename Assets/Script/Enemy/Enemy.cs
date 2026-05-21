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
	public bool lastAttackHitPlayer = false;
	[Header("Movement")]
	[Tooltip("使用 NavMeshAgent 算路，但由本腳本同步角色位置，避免 Agent 直接拖動造成無動畫漂移。")]
	public bool moveWithAgentDesiredVelocity = true;
	[Tooltip("角色步行動畫對應的實際移動速度（單位/秒）。設為 0 則沿用 NM.speed * movementSpeedMultiplier。")]
	public float walkAnimSpeed = 1.5f;
	[Tooltip("移動動畫沒有 root motion 位移時，用這個倍率把 NavMeshAgent desiredVelocity 套到角色。（walkAnimSpeed > 0 時忽略此值）")]
	public float movementSpeedMultiplier = 0.45f;
	[Tooltip("追擊時至少保留的距離，避免程式位移直接衝到玩家身上。0 代表使用 attackRange 的 90%。")]
	public float stoppingDistanceBuffer = 0f;
	[Tooltip("角色面向玩家的轉身速度（度/秒）。0 代表瞬間轉向。")]
	public float rotationSpeed = 120f;
	[Tooltip("若 Animator transition 沒有正常切換，追擊/攻擊時直接播放對應 state。")]
	public bool forceAnimatorStates = false;
	public string walkStateName = "walk";
	public string attackStateName = "attack";
	public string hurtStateName = "hurt";
	public string deathStateName = "death";
	public string spinAttackStateName = "Melee_2H_Attack_Spinning";
	public float animatorCrossFadeDuration = 0.08f;
	protected float nextAttackTime = 0;

	public float maxHP = 200;
	protected float HP;
	protected bool dead;
	public bool IsDead => dead;
	[Tooltip("死亡後屍體是否永久留在場上（不隨時間消失）。BOSS 建議勾選。")]
	public bool keepBodyOnDeath = false;
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

	protected virtual void start () {

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
		if (anim == null)
			anim = GetComponentInChildren<Animator>();
		NM = GetComponent<NavMeshAgent>();
		if (NM != null)
		{
			NM.updatePosition = false;
			NM.updateRotation = AllowRotate;
		}
		if (anim != null)
			anim.applyRootMotion = false;
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

	protected virtual void update () {
		if (dead)
		{
			StopEnemyActions();
			return;
		}

		playerDist = Vector3.Distance(Player.position, transform.position);

		if (NM == null || !NM.isOnNavMesh || anim == null)
			return;

		if (playerDist < attackRange)
		{
			anim.SetBool("walk", false);
			NM.isStopped = true;
			NM.velocity = Vector3.zero;

			if (Time.time >= nextAttackTime)
			{
				lastAttackHitPlayer = false;
				anim.SetTrigger("attack");
				PlayAnimatorState(attackStateName, true);
				nextAttackTime = Time.time + attackInterval;
			}
		}
		else if (playerDist < traceRange)
		{
			NM.isStopped = false;
			NM.SetDestination(Player.position);
			anim.SetBool("walk", true);
			PlayAnimatorState(walkStateName, false);
			MoveAlongAgentPath();
		}
		else
		{
			anim.SetBool("walk", false);
			NM.isStopped = true;
			NM.velocity = Vector3.zero;
			NM.SetDestination(transform.position);
		}
	}

	void PlayAnimatorState(string stateName, bool restartIfAlreadyInState)
	{
		if (!forceAnimatorStates || anim == null || string.IsNullOrEmpty(stateName))
			return;

		AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
		bool alreadyInTargetState = stateInfo.IsName(stateName);

		if (!restartIfAlreadyInState && alreadyInTargetState)
			return;

		if (!restartIfAlreadyInState && IsInBlockingState(stateInfo))
			return;

		anim.CrossFadeInFixedTime(stateName, animatorCrossFadeDuration);
	}

	bool IsInBlockingState(AnimatorStateInfo stateInfo)
	{
		return stateInfo.IsName(attackStateName)
			|| stateInfo.IsName(hurtStateName)
			|| stateInfo.IsName(deathStateName)
			|| stateInfo.IsName(spinAttackStateName);
	}

	protected virtual void lateUpdate()
	{
		if (NM == null)
			return;

		NM.nextPosition = transform.position;

		if (AllowRotate)
			NM.updateRotation = true;
		else
			NM.updateRotation = false;
	}

	void MoveAlongAgentPath()
	{
		if (!moveWithAgentDesiredVelocity || NM == null)
			return;

		Vector3 velocity = NM.desiredVelocity;
		velocity.y = 0f;

		if (velocity.sqrMagnitude <= 0.001f)
			return;

		// 停止距離：到達攻擊距離前就停下，不繼續貼近玩家
		float stopDistance = stoppingDistanceBuffer > 0f ? stoppingDistanceBuffer : attackRange * 0.9f;
		// 同步 NavMeshAgent 的 stoppingDistance，讓路徑計算一致
		NM.stoppingDistance = stopDistance;

		float distanceToPlayer = Player != null ? Vector3.Distance(transform.position, Player.position) : Mathf.Infinity;
		float allowedDistance = Mathf.Max(distanceToPlayer - stopDistance, 0f);
		if (allowedDistance <= 0f)
			return;

		// 優先使用步行動畫速度；若未設定則退回 NM.speed * multiplier
		float walkSpeed = walkAnimSpeed > 0f
			? walkAnimSpeed
			: NM.speed * Mathf.Max(movementSpeedMultiplier, 0f);

		// 依步行速度計算每幀位移，且不超過剩餘可走距離
		Vector3 step = velocity.normalized * Mathf.Min(walkSpeed * Time.deltaTime, allowedDistance);
		transform.position += step;

		// 平滑轉身：rotationSpeed <= 0 代表瞬間轉向
		if (AllowRotate)
		{
			Quaternion targetRot = Quaternion.LookRotation(velocity.normalized);
			if (rotationSpeed > 0f)
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
			else
				transform.rotation = targetRot;
		}
	}

	protected virtual void Hurt(Attack attack)
	{
		// 三連擊第三擊（收尾技）無視 hurtInterval，確保推力一定觸發
		bool isComboFinisher = attack.attackStep >= 3;

		if (!dead && (isComboFinisher || Time.time >= nextHurtTime))
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

			// 第三擊（收尾技）無論 canPushEnemy 是否勾選，一律強制推擊
			if (attack.canPushEnemy || attack.attackStep >= 3)
			{
			Vector3 pushBack = (transform.position - attack.position).normalized;
				pushBack.y = 0f;
				if (pushBack.sqrMagnitude < 0.001f)
					pushBack = Vector3.forward;
				pushBack.Normalize();

				// 使用 VelocityChange（不受質量影響），數值即實際速度 m/s
				float pushSpeed = attack.attackStep >= 3 ? 0.6f
				                : attack.attackStep == 2 ? 0.25f
				                : 0.15f;
				pushBack *= Mathf.Max(attack.strength, 1) * pushSpeed;
				GetComponent<Rigidbody>().AddForce(pushBack, ForceMode.VelocityChange);
			}

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

	protected void Die()
	{
		if (rewardItem) Instantiate(rewardItem, transform.position, Quaternion.identity);
		dead = true;
		StopEnemyActions();
		anim.SetTrigger("die");
		// keepBodyOnDeath = true 時屍體永久留在場上，不呼叫 Remove
		if (!keepBodyOnDeath)
			Invoke("Remove", 5);
		if (HPBar) HPBar.transform.parent.gameObject.SetActive(false);
		gameObject.layer = 0;
		dieEvent.Invoke();
	}

	void Remove()
	{
		Destroy(gameObject);
	}

	public void OnNotifyHit()
	{
		lastAttackHitPlayer = true;
	}

	/// <summary>
	/// 瞬間讓敵人面向 Player（用於 spinning 攻擊結束後轉身）。
	/// </summary>
	public void FacePlayer()
	{
		if (Player == null) return;
		Vector3 dir = (Player.position - transform.position);
		dir.y = 0;
		if (dir.sqrMagnitude > 0.001f)
			transform.rotation = Quaternion.LookRotation(dir.normalized);
	}

	/// <summary>
	/// 等待 Animator 離開 spinStateName 對應的 state 後，瞬間面向 Player。
	/// 由 EnemyAttack.cs 在觸發 spinning 時呼叫。
	/// </summary>
	public System.Collections.IEnumerator WaitSpinThenFace(Animator animator, string spinStateName)
	{
		// 先等一幀，讓 CrossFade 生效、Animator 進入過渡
		yield return null;
		yield return null;

		// 等待進入 spinning state（最多等 0.5 秒）
		float waitIn = 0f;
		while (waitIn < 0.5f && !animator.GetCurrentAnimatorStateInfo(0).IsName(spinStateName))
		{
			waitIn += Time.deltaTime;
			yield return null;
		}

		// 等待 spinning state 播完（離開該 state）
		float timeout = 10f;
		float elapsed = 0f;
		while (elapsed < timeout && animator.GetCurrentAnimatorStateInfo(0).IsName(spinStateName))
		{
			elapsed += Time.deltaTime;
			yield return null;
		}

		// spinning 結束，若 golem 仍存活則轉身面向 Player
		if (!IsDead)
			FacePlayer();
	}

	void RotateTowardsPlayer()
	{
		if (Player == null || anim == null) return;

		AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
		if (IsInBlockingState(stateInfo)) return;

		Vector3 direction = (Player.position - transform.position).normalized;
		direction.y = 0;
		if (direction.sqrMagnitude > 0.001f)
		{
			Quaternion targetRot = Quaternion.LookRotation(direction);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
		}
	}

	protected void StopEnemyActions()
	{
		if (anim != null)
		{
			anim.SetBool("walk", false);
			anim.ResetTrigger("attack");
		}

		if (NM != null && NM.isOnNavMesh)
		{
			NM.isStopped = true;
			NM.velocity = Vector3.zero;
			NM.ResetPath();
		}

		EnemyWeapon[] weapons = GetComponentsInChildren<EnemyWeapon>();
		foreach (EnemyWeapon weapon in weapons)
			weapon.attackDamage = 0;
	}

    void OnDestroy()
    {
        foreach(var mat in material)
        {
			Destroy(mat);
        }
    }

}
