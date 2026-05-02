using UnityEngine;

public class NextAttack : StateMachineBehaviour {

	public float nextAttackTime = 0.5f;
	PlayerController controller;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		controller = animator.GetComponent<PlayerController>();
	}

	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (stateInfo.normalizedTime > nextAttackTime)
			controller.NextAttack = true;
		else
			controller.NextAttack = false;
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		controller.NextAttack = true;
	}
}
