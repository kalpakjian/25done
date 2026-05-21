using UnityEngine;

public class Attack{
	public float damage;
	public Vector3 position;
	public AttackType type;
	public int strength;
	public bool canPushEnemy;
	/// <summary>連擊中的第幾擊（1/2/3）。由 PlayerWeapon 依 PlayerController.CurrentAttackStep 填入。</summary>
	public int attackStep;
}
