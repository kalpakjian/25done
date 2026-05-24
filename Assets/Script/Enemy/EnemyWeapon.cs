using UnityEngine;

public class EnemyWeapon : MonoBehaviour {

	[HideInInspector]
	public float attackDamage;
	public float weaponDamage;

	/// <summary>
	/// 同一次攻擊中，若已有任一武器命中玩家，此旗標設為 true。
	/// 由 EnemyAttack 在每次攻擊窗口開始前重置。
	/// </summary>
	[HideInInspector]
	public bool hitDisabled = false;

	void OnTriggerEnter (Collider col) {
		if (col.CompareTag("Player") && attackDamage > 0 && !hitDisabled) {
			// 造成傷害
			col.SendMessage("Hurt", attackDamage + weaponDamage);

			// 鎖住自己以及同一個 Boss 身上的所有其他武器，防止同一次攻擊重複命中
			// 找到最近的 Enemy 元件，從它底下抓所有武器
			Enemy owner = GetComponentInParent<Enemy>();
			if (owner != null) {
				EnemyWeapon[] allWeapons = owner.GetComponentsInChildren<EnemyWeapon>();
				foreach (EnemyWeapon w in allWeapons)
					if (w != null)
						w.hitDisabled = true;
			} else {
				// 找不到 Enemy 根節點，至少鎖住自己
				hitDisabled = true;
			}
		}
	}
}
