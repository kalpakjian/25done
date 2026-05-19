using UnityEngine;

public class PlayerWeapon : MonoBehaviour {

	[HideInInspector]
	public float attackDamage;
	public float weaponDamage;
	[HideInInspector]
	public AttackType type;
	[HideInInspector]
	public int strength;
	[HideInInspector]
	public bool canPushEnemy;

	Transform player;
	PlayerController controller;

	Attack attack = new Attack();

	void Start() {
		player = GameObject.FindWithTag("Player").transform;
		controller = player.GetComponent<PlayerController>();
	}

	void OnTriggerEnter (Collider col) {

		if (col.CompareTag("Treasure") && attackDamage > 0)
			col.SendMessage("Hit");

		if (col.CompareTag("Enemy") && attackDamage > 0)
		{

			attack.damage = attackDamage + weaponDamage;
			attack.position = player.position;
			attack.type = type;
			attack.strength = strength;
			attack.canPushEnemy = canPushEnemy;

			col.SendMessage("Hurt", attack);

			if (controller != null)
				controller.RegisterAttackHit();
		}


	}
}
