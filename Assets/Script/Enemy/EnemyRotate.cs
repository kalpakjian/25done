using UnityEngine;

public class EnemyRotate : StateMachineBehaviour
{

    public bool allow = true;
    public bool targetPlayer = false;
    Transform Player;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<Enemy>().AllowRotate = allow;

        if (targetPlayer)
        {
            Player = GameObject.FindWithTag("Player").transform;
            Vector3 lookAtPos = Player.position;
            lookAtPos.y = animator.transform.position.y;
            animator.transform.LookAt(lookAtPos);
        }
    }
}
