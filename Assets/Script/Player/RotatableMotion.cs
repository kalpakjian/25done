using UnityEngine;

public class RotatableMotion : StateMachineBehaviour
{
    public bool allow = true;
    public bool changeDirection = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerController player = animator.GetComponent<PlayerController>();
        if (player == null) return;

        player.AllowRotate = allow;

        if (changeDirection)
        {
            player.RotateChar();
        }
    }
}