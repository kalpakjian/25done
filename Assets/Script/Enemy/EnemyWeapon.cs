using UnityEngine;

public class EnemyWeapon : MonoBehaviour {

	[HideInInspector]
	public float attackDamage;
	public float weaponDamage;

	[Header("Debug")]
	[Tooltip("Play 模式時在 Scene 視窗顯示武器 Collider 範圍（需開啟 Gizmos）。有傷害時紅色，無傷害時青色。")]
	public bool showDebugCollider = true;

	Enemy owner;

	// 每次攻擊動畫開始時，由 EnemyAttack 呼叫，重置命中記錄
	bool hasHitThisSwing = false;

	void Awake() {
		owner = GetComponentInParent<Enemy>();
	}

	/// <summary>
	/// 由 EnemyAttack StateMachineBehaviour 在攻擊動畫開始時呼叫，重置單次命中記錄。
	/// </summary>
	public void OnAttackBegin()
	{
		hasHitThisSwing = false;
	}

	// 武器傷害歸零時同步重置命中記錄，避免跨攻擊殘留
	void LateUpdate()
	{
		if (attackDamage <= 0f)
			hasHitThisSwing = false;
	}

	void OnTriggerEnter(Collider col) {
		TryHitPlayer(col);
	}

	// 補上 OnTriggerStay：武器 collider 已在玩家內部時（攻擊前就靠太近），仍能命中
	void OnTriggerStay(Collider col) {
		TryHitPlayer(col);
	}

	void TryHitPlayer(Collider col)
	{
		if (owner != null && owner.IsDead)
		{
			attackDamage = 0;
			return;
		}

		if (col.CompareTag("Player") && attackDamage > 0 && !hasHitThisSwing)
		{
			hasHitThisSwing = true;   // 一次揮擊只命中一次，防連續扣血
			col.SendMessage("Hurt", attackDamage + weaponDamage);
		}
	}

	// ────────── Gizmos 除錯：Play 時在 Scene 視窗顯示武器 collider ──────────
#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		if (!showDebugCollider) return;

		// 有傷害時紅色（半透明），無傷害時青色
		Color col = attackDamage > 0f
			? new Color(1f, 0f, 0f, 0.45f)
			: new Color(0f, 1f, 1f, 0.2f);
		Gizmos.color = col;

		DrawColliderGizmo();
	}

	void DrawColliderGizmo()
	{
		// BoxCollider
		BoxCollider box = GetComponent<BoxCollider>();
		if (box != null)
		{
			Gizmos.matrix = Matrix4x4.TRS(
				transform.TransformPoint(box.center),
				transform.rotation,
				transform.lossyScale);
			Gizmos.DrawCube(Vector3.zero, box.size);
			Gizmos.DrawWireCube(Vector3.zero, box.size);
			Gizmos.matrix = Matrix4x4.identity;
			return;
		}

		// SphereCollider
		SphereCollider sphere = GetComponent<SphereCollider>();
		if (sphere != null)
		{
			float r = sphere.radius * Mathf.Max(
				transform.lossyScale.x,
				transform.lossyScale.y,
				transform.lossyScale.z);
			Gizmos.DrawSphere(transform.TransformPoint(sphere.center), r);
			Gizmos.DrawWireSphere(transform.TransformPoint(sphere.center), r);
			return;
		}

		// CapsuleCollider
		CapsuleCollider cap = GetComponent<CapsuleCollider>();
		if (cap != null)
		{
			// 畫兩顆球代表膠囊兩端
			float r = cap.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
			float halfH = Mathf.Max(0f, cap.height * 0.5f * transform.lossyScale.y - r);
			Vector3 center = transform.TransformPoint(cap.center);
			Vector3 up = transform.up * halfH;
			Gizmos.DrawWireSphere(center + up, r);
			Gizmos.DrawWireSphere(center - up, r);
			return;
		}
	}
#endif
}
