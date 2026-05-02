using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowMotion : MonoBehaviour
{
	static MonoBehaviour instance;
	
	void Awake()
    {
		instance = this;   
    }
    public static void Slow(float duration, float slowFactor)
    {
		instance.StartCoroutine(((SlowMotion)instance).SlowBegin(duration, slowFactor));
	}

	IEnumerator SlowBegin(float duration, float slowFactor)
	{

		Time.timeScale /= slowFactor;
		Time.fixedDeltaTime /= slowFactor;
		Time.maximumDeltaTime /= slowFactor;

		yield return new WaitForSecondsRealtime(duration);

		Time.timeScale = 1.0f;
		Time.fixedDeltaTime *= slowFactor;
		Time.maximumDeltaTime *= slowFactor;
	}
}
