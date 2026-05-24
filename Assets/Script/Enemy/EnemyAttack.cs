using UnityEngine;

public class EnemyAttack : StateMachineBehaviour {

	public float damage;
	public float start = 0;
	public float end = 1;

	Enemy enemy;
	EnemyWeapon[] weapons;

	// 記錄上一幀的 attackDamage，用來偵測「攻擊窗口剛開始」
	bool wasActive = false;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		enemy = animator.GetComponent<Enemy>();
		weapons = animator.GetComponentsInChildren<EnemyWeapon>();
		wasActive = false;
		// 進入攻擊動畫時先清除所有武器傷害和命中鎖
		SetAllWeapons(0, false);
	}

	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		bool isActive = stateInfo.normalizedTime > start && stateInfo.normalizedTime < end;

		if (isActive) {
			// 剛進入攻擊窗口時，重置命中鎖（代表新一輪攻擊開始）
			if (!wasActive)
				SetAllWeapons(0, false); // 先歸零，再由下面設定傷害

			SetAllDamage(damage * enemy.power);
		} else {
			SetAllDamage(0);
		}

		wasActive = isActive;
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		SetAllWeapons(0, false);
		wasActive = false;
	}

	void SetAllDamage(float dmg) {
		if (weapons == null) return;
		foreach (EnemyWeapon weapon in weapons)
			if (weapon != null)
				weapon.attackDamage = dmg;
	}

	void SetAllWeapons(float dmg, bool hitDisabled) {
		if (weapons == null) return;
		foreach (EnemyWeapon weapon in weapons) {
			if (weapon != null) {
				weapon.attackDamage = dmg;
				weapon.hitDisabled = hitDisabled;
			}
		}
	}
}
