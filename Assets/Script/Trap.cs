using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour {

	public float startDelay = 0f;
	public float loopDuration = 1f;

	public Transform a;
	public Transform b;

	int id;

	// Use this for initialization
	void Start () {
		StartCoroutine (TrapStartAfter (startDelay));
	}

	IEnumerator TrapStartAfter(float sec)
	{
		yield return new WaitForSeconds (sec);

		gameObject.transform.position = a.position;
		id = LeanTween.move (gameObject, b.position, loopDuration).setLoopPingPong (0).id;
	}

	void OnDestroy() {
		//LeanTween.removeTween (id);
	}
}
