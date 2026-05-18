using UnityEngine;

public class AttackEffect : StateMachineBehaviour {

	public GameObject FX;
	GameObject currentFX;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (FX == null) return;
		Transform tran = animator.transform;
		currentFX = Instantiate (FX, tran.position, tran.rotation);
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (currentFX != null)
			Destroy(currentFX);
	}

}
