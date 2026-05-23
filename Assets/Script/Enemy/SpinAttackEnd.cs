using UnityEngine;

/// <summary>
/// 掛在 Animator 的 Melee_2H_Attack_Spinning state 上。
/// 動畫播完（OnStateExit）時，讓 golem 立刻面向 Player。
/// 
/// 使用方式：
///   1. 開啟 golem 的 Animator Controller。
///   2. 選取 Melee_2H_Attack_Spinning state。
///   3. 在 Inspector > Add Behaviour > SpinAttackEnd。
/// </summary>
public class SpinAttackEnd : StateMachineBehaviour
{
	// 若想用平滑轉身而非瞬間，可設定此值（度/秒）。0 = 瞬間。
	[Tooltip("spinning 結束後轉向 Player 的速度（度/秒）。0 代表瞬間到位。")]
	public float snapRotationSpeed = 0f;

	Enemy enemy;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		enemy = animator.GetComponent<Enemy>();
		if (enemy == null)
			enemy = animator.GetComponentInParent<Enemy>();
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (enemy == null || !enemy.gameObject.activeInHierarchy) return;

		Transform player = GameObject.FindWithTag("Player")?.transform;
		if (player == null) return;

		Vector3 dir = player.position - enemy.transform.position;
		dir.y = 0f;
		if (dir.sqrMagnitude < 0.001f) return;

		if (snapRotationSpeed <= 0f)
		{
			// 瞬間轉向
			enemy.transform.rotation = Quaternion.LookRotation(dir.normalized);
		}
		else
		{
			// 平滑轉向：啟動協程
			enemy.StartCoroutine(SmoothFace(enemy, snapRotationSpeed));
		}
	}

	static System.Collections.IEnumerator SmoothFace(Enemy e, float speed)
	{
		if (e == null) yield break;

		Transform player = GameObject.FindWithTag("Player")?.transform;
		if (player == null) yield break;

		float timeout = 1.5f;  // 最多轉 1.5 秒，防止卡住
		float elapsed = 0f;

		while (elapsed < timeout)
		{
		if (e == null || !e.gameObject.activeInHierarchy) yield break;

			Vector3 dir = (player.position - e.transform.position);
			dir.y = 0;
			if (dir.sqrMagnitude < 0.001f) yield break;

			Quaternion target = Quaternion.LookRotation(dir.normalized);
			e.transform.rotation = Quaternion.RotateTowards(e.transform.rotation, target, speed * Time.deltaTime);

			// 轉完了就停
			if (Quaternion.Angle(e.transform.rotation, target) < 0.5f) yield break;

			elapsed += Time.deltaTime;
			yield return null;
		}
	}
}
