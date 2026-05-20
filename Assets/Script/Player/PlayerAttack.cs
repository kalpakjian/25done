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
		player.CurrentAttackStep = attackStep;
		player.CurrentAttackHit = false;

		if (weapon == null) return;

		weapon.type = type;
		weapon.strength = strength;
		weapon.canPushEnemy = canPushEnemy;
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
