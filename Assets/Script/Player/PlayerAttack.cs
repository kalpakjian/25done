using UnityEngine;

public class PlayerAttack : StateMachineBehaviour {

	public float damage;
	public AttackType type;
	public int strength = 1;
	public int attackStep = 1;
	public bool canPushEnemy = false;
	public float start = 0;
	public float end = 1;
	public bool stepForwardOnSecondAndThird = true;
	public float forwardStepDistance = 0.55f;
	public float forwardStepStart = 0.05f;
	public float forwardStepEnd = 0.35f;

	PlayerController player;
	PlayerWeapon weapon;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		player = animator.GetComponent<PlayerController>();
		weapon = animator.GetComponentInChildren<PlayerWeapon>();

		// 若 Inspector 未設定 attackStep（預設 1），自動依狀態名稱偵測第幾擊
		bool nameIs3rd = stateInfo.IsName("attack03") || stateInfo.IsName("meleeattack03");
		bool nameIs2nd = stateInfo.IsName("attack02") || stateInfo.IsName("meleeattack02");
		int resolvedStep = attackStep;
		if (resolvedStep < 2 && nameIs2nd) resolvedStep = 2;
		if (resolvedStep < 3 && nameIs3rd) resolvedStep = 3;

		player.CurrentAttackStep = resolvedStep;
		player.CurrentAttackHit = false;

		if (weapon == null) return;

		weapon.type = type;
		weapon.strength = strength;
		// 第三擊強制開啟 canPushEnemy，即使 Inspector 沒有勾選
		weapon.canPushEnemy = canPushEnemy || resolvedStep >= 3;
		weapon.ResetHitTargets();
	}

	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (weapon == null) return;

		MoveForwardDuringComboStep(animator, stateInfo);

		if (stateInfo.normalizedTime > start 
							&& stateInfo.normalizedTime < end)
			weapon.attackDamage = damage * player.power;
		else
			weapon.attackDamage = 0;
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (weapon == null) return;

		weapon.attackDamage = 0;
	}

	void MoveForwardDuringComboStep(Animator animator, AnimatorStateInfo stateInfo) {
		if (!stepForwardOnSecondAndThird || !ShouldStepForward(stateInfo) || forwardStepDistance <= 0)
			return;

		if (stateInfo.normalizedTime < forwardStepStart || stateInfo.normalizedTime > forwardStepEnd)
			return;

		float stepDuration = Mathf.Max((forwardStepEnd - forwardStepStart) * stateInfo.length, 0.01f);
		float stepSpeed = forwardStepDistance / stepDuration;
		animator.transform.position += animator.transform.forward * stepSpeed * Time.deltaTime;
	}

	bool ShouldStepForward(AnimatorStateInfo stateInfo) {
		if (attackStep >= 2)
			return true;

		return stateInfo.IsName("attack02")
			|| stateInfo.IsName("attack03")
			|| stateInfo.IsName("meleeattack02")
			|| stateInfo.IsName("meleeattack03");
	}
}
