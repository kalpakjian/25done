using UnityEngine;

public class EnemyRotate : StateMachineBehaviour
{

    public bool allow = true;
    public bool targetPlayer = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Animator 可能掛在子物件，Enemy 掛在根物件，需往上找
        Enemy enemy = animator.GetComponent<Enemy>();
        if (enemy == null)
            enemy = animator.GetComponentInParent<Enemy>();

        if (enemy == null)
            return;

        enemy.AllowRotate = allow;

        if (targetPlayer)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null) return;

            Vector3 dir = playerObj.transform.position - enemy.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                enemy.transform.rotation = Quaternion.LookRotation(dir.normalized);
        }
    }
}
