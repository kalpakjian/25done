using UnityEngine;
using UnityEngine.Events;

public class TriggerEvent : MonoBehaviour {

	public UnityEvent triggerEnter;
	public UnityEvent triggerExit;

	void OnTriggerEnter (Collider col) {
		if (col.CompareTag("Player"))
			triggerEnter.Invoke();
	}

	void OnTriggerExit (Collider col) {
		if (col.CompareTag("Player"))
			triggerExit.Invoke();
	}
}


