using UnityEngine;

/// <summary>
/// 掛在每個攻擊動畫狀態上的 StateMachineBehaviour，控制玩家何時可以輸入下一段攻擊。
///
/// requireHitToContinue 設定指引：
///   false（預設）— attack01、2H1~2H5：揮空也能接下一段
///   true          — attack02、attack03：只有打中才能繼續連擊
/// </summary>
public class NextAttack : StateMachineBehaviour {

	[Tooltip("normalizedTime 超過此值後，才開放下一段輸入。")]
	public float nextAttackTime = 0.5f;

	[Tooltip("true = 打中才允許下一段；false = 揮空也允許下一段。")]
	public bool requireHitToContinue = false;

	PlayerController controller;

	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		controller = animator.GetComponent<PlayerController>();
	}

	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (controller == null) return;

		bool timeReached = stateInfo.normalizedTime > nextAttackTime;
		bool hitOk       = !requireHitToContinue || controller.CurrentAttackHit;
		controller.NextAttack = timeReached && hitOk;
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (controller == null) return;
		controller.NextAttack = true;
	}
}
