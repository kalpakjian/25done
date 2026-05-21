using UnityEngine;

public class EnemyAttack : StateMachineBehaviour {

	public float damage;
	[Tooltip("動畫 normalizedTime 進入此值後，武器開始造成傷害。")]
	public float start = 0;
	[Tooltip("動畫 normalizedTime 超過此值後，武器傷害關閉。")]
	public float end = 1;

	Enemy enemy;
	// 支援雙持武器：取得所有 EnemyWeapon（含雙手）
	EnemyWeapon[] weapons;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		// Animator 可能掛在子物件上，需同時往上找 Enemy
		enemy = animator.GetComponent<Enemy>();
		if (enemy == null)
			enemy = animator.GetComponentInParent<Enemy>();

		// 從 Enemy 根物件往下找全部武器（雙持不漏網）
		Transform root = enemy != null ? enemy.transform : animator.transform;
		weapons = root.GetComponentsInChildren<EnemyWeapon>();

		// 通知所有武器攻擊開始，重置單次命中記錄
		foreach (EnemyWeapon w in weapons)
			w.OnAttackBegin();
	}

	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (enemy == null || enemy.IsDead)
		{
			SetAllWeaponDamage(0);
			return;
		}

		bool isDamageActive = stateInfo.normalizedTime > start && stateInfo.normalizedTime < end;
		SetAllWeaponDamage(isDamageActive ? damage * enemy.power : 0f);
	}

	void SetAllWeaponDamage(float value)
	{
		foreach (EnemyWeapon w in weapons)
			if (w != null) w.attackDamage = value;
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		SetAllWeaponDamage(0);
	}
}
