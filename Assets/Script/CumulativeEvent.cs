using UnityEngine;
using UnityEngine.Events;

public class CumulativeEvent : MonoBehaviour {

	public UnityEvent unlockEvent;

	public int targetPoint;
	int currentPoint;

	void Start () {
		currentPoint = 0;
	}
	
	public void AddPoint (int point) {
		currentPoint += point;
		if (currentPoint >= targetPoint)
			unlockEvent.Invoke();
	}
}

