using UnityEngine;
using System.Collections.Generic;

public class PlayerWeapon : MonoBehaviour {

	[HideInInspector]
	public float attackDamage;
	public float weaponDamage;
	[HideInInspector]
	public AttackType type;
	[HideInInspector]
	public int strength;
	[HideInInspector]
	public bool canPushEnemy;

	[Header("Melee Hit Assist")]
	[Tooltip("補償近身時武器 Trigger 沒有重新進入敵人 Collider，導致貼身揮空的問題。")]
	public bool enableCloseRangeAssist = true;
	[Tooltip("近身輔助判定中心，位於玩家前方的距離。")]
	public float closeRangeForwardOffset = 0.85f;
	[Tooltip("近身輔助判定半徑。若貼身仍打不到，可稍微調大。")]
	public float closeRangeRadius = 0.9f;
	[Tooltip("玩家正前方可命中的角度。180 代表完整前半圈。")]
	[Range(1f, 180f)]
	public float closeRangeAngle = 140f;
	[Tooltip("輔助判定會檢查的 Layer。預設 Everything；可在 Inspector 改成 Enemy Layer 減少誤判。")]
	public LayerMask closeRangeHitMask = ~0;
	[Tooltip("每幀輔助判定最多處理的 Collider 數。")]
	public int maxCloseRangeHits = 16;

	Transform player;
	PlayerController controller;

	Attack attack = new Attack();
	Collider[] closeRangeHits;
	HashSet<GameObject> hitTargets = new HashSet<GameObject>();
	float previousAttackDamage;

	void Start() {
		GameObject playerObject = GameObject.FindWithTag("Player");
		if (playerObject != null)
		{
			player = playerObject.transform;
			controller = player.GetComponent<PlayerController>();
		}

		closeRangeHits = new Collider[Mathf.Max(1, maxCloseRangeHits)];
	}

	void LateUpdate() {
		if (attackDamage > 0 && previousAttackDamage <= 0)
			ResetHitTargets();

		if (attackDamage > 0 && enableCloseRangeAssist)
			CheckCloseRangeHits();

		previousAttackDamage = attackDamage;
	}

	void OnTriggerEnter (Collider col) {

		if (col.CompareTag("Treasure") && attackDamage > 0)
			col.SendMessage("Hit");

		if (attackDamage > 0)
			TryHitEnemy(col);


	}

	public void ResetHitTargets() {
		hitTargets.Clear();
	}

	void CheckCloseRangeHits() {
		if (player == null || closeRangeHits == null)
			return;

		Vector3 center = player.position + player.forward * closeRangeForwardOffset;
		int hitCount = Physics.OverlapSphereNonAlloc(
			center,
			closeRangeRadius,
			closeRangeHits,
			closeRangeHitMask,
			QueryTriggerInteraction.Collide
		);

		for (int i = 0; i < hitCount; i++)
		{
			Collider hit = closeRangeHits[i];
			if (hit == null)
				continue;

			Vector3 directionToTarget = hit.bounds.center - player.position;
			directionToTarget.y = 0f;

			if (directionToTarget.sqrMagnitude > 0.001f)
			{
				float angle = Vector3.Angle(player.forward, directionToTarget);
				if (angle > closeRangeAngle * 0.5f)
					continue;
			}

			TryHitEnemy(hit);
		}
	}

	void TryHitEnemy(Collider col) {
		if (attackDamage <= 0 || col == null)
			return;

		GameObject target = GetEnemyTarget(col);
		if (target == null || hitTargets.Contains(target))
			return;

		hitTargets.Add(target);

		attack.damage = attackDamage + weaponDamage;
		attack.position = player != null ? player.position : transform.position;
		attack.type = type;
		attack.strength = strength;
		attack.canPushEnemy = canPushEnemy;

		target.SendMessage("Hurt", attack);

		if (controller != null)
			controller.RegisterAttackHit();
	}

	GameObject GetEnemyTarget(Collider col) {
		Enemy enemy = col.GetComponentInParent<Enemy>();
		if (enemy != null)
			return enemy.gameObject;

		if (col.CompareTag("Enemy"))
			return col.gameObject;

		return null;
	}

	void OnDrawGizmosSelected() {
		if (!enableCloseRangeAssist)
			return;

		Transform origin = player != null ? player : transform.root;
		if (origin == null)
			return;

		Gizmos.color = new Color(1f, 0.75f, 0f, 0.35f);
		Gizmos.DrawSphere(origin.position + origin.forward * closeRangeForwardOffset, closeRangeRadius);
	}
}
