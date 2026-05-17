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

        // 1. Attack Logic: If player is within attackRange, stay at maintainDistance
        if (playerDist < attackRange)
        {
            anim.SetBool("walk", false);
            
            // Implement "won't approach player" by moving to a point at 'maintainDistance'
            Vector3 directionToPlayer = (Player.position - transform.position).normalized;
            Vector3 targetPosition = transform.position + (directionToPlayer * maintainDistance);

            // Ensure we don't overshoot the player
            if (playerDist < maintainDistance)
            {
                // If too close, move back to maintain distance
                Vector3 backPosition = transform.position - (directionToPlayer * 0.5f);
                NM.SetDestination(backPosition);
            }
            else
            {
                // Move to the perimeter of the attack range
                NM.SetDestination(targetPosition);
            }

            if (Time.time >= nextAttackTime)
            {
                anim.SetTrigger("attack");
                nextAttackTime = Time.time + attackInterval;
            }
        }
        // 2. Trace Logic: If player is within traceRange but outside attackRange
        else if (playerDist < traceRange)
        {
            NM.SetDestination(Player.position);
            anim.SetBool("walk", true);
        }
        // 3. Idle Logic: Player is too far
        else
        {
            anim.SetBool("walk", false);
            NM.SetDestination(transform.position);
        }
    }
}
