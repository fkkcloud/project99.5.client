using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIO;

public class PlayerController : IOGameBehaviour {

	[HideInInspector]
	public Player PlayerObject;

	[HideInInspector]
	public bool isOnChat = false;

	public bool flipX = false;
	public bool flipZ = false;

	float signX;
	float signZ;

	public enum Role
	{
		UpDown,
		LeftRight
	}

	public Role ControlRole;

	public enum PlayerState
	{
		Lobby,
		Joined,
		Playing
	}

	public PlayerState State = PlayerState.Lobby;

	// Use this for initialization
	void Start () {
		signX = (flipX) ? -1f : 1f;
		signZ = (flipZ) ? -1f : 1f;
	}
	
	// Update is called once per frame
	void Update () {

		if (State != PlayerState.Lobby && !isOnChat) {
			
			float x = Input.GetAxis ("Vertical");
			float z = Input.GetAxis ("Horizontal");

			if (x == 0f && z == 0f)
				return;

			if (ControlRole == Role.LeftRight)
				x = 0f;
			else if (ControlRole == Role.UpDown)
				z = 0f;

			Vector3 newPosition = PlayerObject.gameObject.transform.position + new Vector3(x * Time.deltaTime * 4f * signX, 0f, z * Time.deltaTime * 4f * signZ);
			PlayerObject.gameObject.transform.position = newPosition;

			Dictionary<string, string> data = new Dictionary<string, string> ();
			data ["position"] = newPosition.x + "," + newPosition.y + "," + newPosition.z;

			if (ControlRole == Role.LeftRight)
				SocketIOComp.Emit("SERVER:MOVELR", new JSONObject(data)); // z
			else if (ControlRole == Role.UpDown)
				SocketIOComp.Emit("SERVER:MOVEUD", new JSONObject(data)); // x
			
			//Debug.Log ("Attempting move:" + data["position"]);
			
		}

	}
}
