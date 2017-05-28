using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComeAndClear : IOGameBehaviour {

	void OnTriggerEnter(Collider other) {
		if (other.tag == "Player")
			GlobalGameState.Clear ();
	}
}
