using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComeAndDie : IOGameBehaviour {


	void OnTriggerEnter(Collider other) {
		if (other.tag == "Player")
			GlobalGameState.GameOver ();
	}
}
