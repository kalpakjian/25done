using UnityEngine;

public class RotatableMotion : StateMachineBehaviour {

	public bool allow = true;
	public bool changeDirection = false;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		PlayerController Player = animator.GetComponent<PlayerController>();
		Player.AllowRotate = allow;
		if (changeDirection)
			animator.GetComponent<PlayerController>().RotateChar();
	}
}

