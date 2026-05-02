using UnityEngine;
using System.Collections.Generic;

public class EnemyController : Enemy {

	void Start () {
		start();
	}

	void Update () {
		update();
	}

	void LateUpdate() {
		lateUpdate();
	}
}

