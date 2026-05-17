using UnityEngine;
using UnityEngine.AI;

public class RogueController : Enemy
{
    [Header("Rogue Specific Settings")]
    [Tooltip("The distance the Rogue tries to maintain from the player when attacking.")]
    public float maintainDistance = 4f;

    void Start()
    {
        start();
    }

    void Update()
    {
        update();
    }

    void LateUpdate()
    {
        lateUpdate();
    }

    protected override void update()
    {
        playerDist = Vector3.Distance(Player.position, transform.position);

        float stopThreshold = 0.5f;

        // Movement logic
        if (playerDist < maintainDistance)
        {
            // Player is too close — retreat away from player
            Vector3 directionAway = (transform.position - Player.position).normalized;
            Vector3 retreatTarget = Player.position + directionAway * (maintainDistance + 1f);
            NM.SetDestination(retreatTarget);
            anim.SetBool("walk", true);
        }
        else if (playerDist < maintainDistance + stopThreshold)
        {
            // At preferred distance — hold position
            anim.SetBool("walk", false);
            NM.SetDestination(transform.position);
        }
        else if (playerDist < traceRange)
        {
            // Within trace range but too far — approach to maintainDistance
            Vector3 dirToPlayer = (Player.position - transform.position).normalized;
            Vector3 approachTarget = Player.position - dirToPlayer * maintainDistance;
            NM.SetDestination(approachTarget);
            anim.SetBool("walk", true);
        }
        else
        {
            // Player is too far — idle
            anim.SetBool("walk", false);
            NM.SetDestination(transform.position);
        }

        // Attack from maintainDistance (ranged attacker)
        if (playerDist < maintainDistance + stopThreshold && Time.time >= nextAttackTime)
        {
            anim.SetTrigger("attack");
            nextAttackTime = Time.time + attackInterval;
        }
    }
}
