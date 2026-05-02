using UnityEngine;

public class AutoTarget : StateMachineBehaviour {

	Transform Player;
	public float range=3;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

        Player = animator.transform;
		GameObject target = FindClosestTarget(Player.transform.position, range, "HitTarget");
		if (target!=null) {
			Vector3 lookPos = target.transform.position;
			lookPos.y = Player.position.y;
			Player.LookAt(lookPos);
		}
	}

	GameObject FindClosestTarget(Vector3 location, float radius, string targetTag)
	{
		GameObject closest = null;
		float distance = Mathf.Infinity;
		int layerMask = LayerMask.GetMask(targetTag);

		foreach (var thing in Physics.OverlapSphere(location, radius, layerMask))
		{
			float calcDistance = Vector3.Distance(Player.position, thing.transform.position);

			if (distance > calcDistance)
			{
				distance = calcDistance;
				closest = thing.gameObject;
			}
		}
		return closest;
	}
}
