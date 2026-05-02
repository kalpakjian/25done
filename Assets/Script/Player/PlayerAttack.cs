using UnityEngine;

public class PlayerAttack : StateMachineBehaviour {

	public float damage;
	public AttackType type;
	public int strength = 1;
	public float start = 0;
	public float end = 1;

	PlayerController player;
	PlayerWeapon weapon;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		player = animator.GetComponent<PlayerController>();
		weapon = animator.GetComponentInChildren<PlayerWeapon>();
		weapon.type = type;
		weapon.strength = strength;
	}

	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (stateInfo.normalizedTime > start 
							&& stateInfo.normalizedTime < end)
			weapon.attackDamage = damage * player.power;
		else
			weapon.attackDamage = 0;
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		weapon.attackDamage = 0;
	}
}

