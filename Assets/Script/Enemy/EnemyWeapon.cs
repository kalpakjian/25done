using UnityEngine;

public class EnemyWeapon : MonoBehaviour {

	[HideInInspector]
	public float attackDamage;
	public float weaponDamage;

	void OnTriggerEnter (Collider col) {
		if (col.CompareTag("Player") && attackDamage > 0)
			col.SendMessage("Hurt", attackDamage + weaponDamage);
	}
}


