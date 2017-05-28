using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SocketIO;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

public class GameState : IOGameBehaviour {

	public LoginController LoginUI;
	//public ChatUIController ChatUI;
	public GameUIController GameUI;
	public DialogueUIController DialogueUI;

	public Text ControlLeftRight;
	public Text ControlUpDown;

	public ParticleSystem GameOverParticle;
	public GameObject GameOverUI;

	public GameObject ClearUI;
	public GameObject ClearClearUI;

	public GameObject CamObj;

	int CurrentLevelNumber;

	public Text ResponseText;
	public Text ChannelText;

	public GameObject PlayerPrefab;
	public GameObject[] ScenePrefabs;

	string myname;

	[HideInInspector]
	public List<Player> Players = new List<Player>();
	[HideInInspector]
	public GameObject CurrentScene;

	bool ServerConnected = false;

	float pingtime = 0f;
	float pongtime = 0f;
	float lastpongtime = 0f;
	bool pingdone = false;

	public void Exit(){
		Application.Quit ();
	}

	// Use this for initialization
	void Start () {

		//ChatUI.Hide ();
		LoginUI.Hide ();
		DialogueUI.Hide ();
		GameUI.Hide ();


		//"ws://pure-sea-67758.herokuapp.com:80/socket.io/?EIO=4&transport=websocket"
		//"ws://127.0.0.1:3000/socket.io/?EIO=4&transport=websocket";
		InitCallbacks ();

		StartCoroutine (Connection ());

		//ping
		StartCoroutine (PingUpdate ());
	}

	IEnumerator PingUpdate()
	{
		SocketIOComp.Emit ("SERVER:PING");
		pingtime = Time.timeSinceLevelLoad;

		yield return new WaitForSeconds (1f);

		// check server is connected
		float lastpongduration = Mathf.Abs (Time.timeSinceLevelLoad - lastpongtime);
		if (lastpongduration > 4f) {
			// if server is disconnected
			PlayerControllerComp.State = PlayerController.PlayerState.Lobby;
			ServerConnected = false;
			//ChatUI.Hide ();
			LoginUI.Hide ();
			GameUI.Hide ();
			ClearScene ();
		}

		if (pingdone)
		{
			pongtime = pongtime * 1000f;
			ResponseText.text = (int)pongtime + "ms";
			pingdone = false;
		}
		StartCoroutine (PingUpdate ());
	}


	IEnumerator Connection(){
		
		yield return new WaitForSeconds(1f);

		if (!ServerConnected) {
			DialogueUI.Show (DialogueUIController.DialogueTypes.ConnectingServer);
			SocketIOComp.Emit ("SERVER:CONNECT");
		}
		StartCoroutine (Connection ());
	}

	private void InitCallbacks(){
		SocketIOComp.On ("CLIENT:PING", OnPing);

		SocketIOComp.On ("CLIENT:CONNECTED", OnServerConnected);

		SocketIOComp.On ("CLIENT:JOINED", OnUserJoined);

		SocketIOComp.On ("CLIENT:CREATE_OTHER", OnOtherUserJoin);

		SocketIOComp.On ("CLIENT:MOVE", OnUserMove);

		SocketIOComp.On ("CLIENT:DISCONNECTED", OnUserDisconnect);

		//SocketIOComp.On ("CLIENT:CHATSEND", OnChatSend);

		SocketIOComp.On ("CLIENT:ROOM_FULL", OnRoomFull);

		SocketIOComp.On ("CLIENT:SERVER_FULL", OnServerFull);
	}

	void OnPing(SocketIOEvent evt){ 
		pongtime = Mathf.Abs(Time.timeSinceLevelLoad - pingtime);
		lastpongtime = Time.timeSinceLevelLoad;
		pingdone = true;
	}

	// Update is called once per frame
	void Update () {
		
	}

	/*
	----------------------------------------------------------------------------------------------------------------
	Callbacks
	----------------------------------------------------------------------------------------------------------------
	*/

	private void OnServerConnected(SocketIOEvent evt){

		ServerConnected = true;

		DialogueUI.Hide ();
		LoginUI.Show ();
	}

	private void OnUserJoined(SocketIOEvent evt){

		// --------- GAME BEGIN ----------
		string roomName = JsonToString (evt.data.GetField ("room").ToString (), "\"");
		string[] strArry = roomName.Split(new char[]{'_'});
		ChannelText.text = "Channel Name: " + strArry[1];

		Debug.Log ("Connected server as " + evt.data);
		GameUI.Show ();
		DialogueUI.Hide ();
		LoginUI.Hide ();
		//ChatUI.Show ();

		string is2nd = JsonToString (evt.data.GetField ("is2nd").ToString (), "\"");
		string name = JsonToString( evt.data.GetField("name").ToString(), "\"");
		myname = name;
		string sceneNumber = JsonToString (evt.data.GetField ("scenenumber").ToString (), "\"");

		// create currentUser here
		if (is2nd == "true") {
			PlayerControllerComp.PlayerObject = CreateUser (evt, false);
			PlayerControllerComp.ControlRole = PlayerController.Role.UpDown;

			ControlUpDown.text = "Control Up/Down:" + name;
			ControlUpDown.color = Color.green;
			// SET UI for UpDown user name

		} else {
			PlayerControllerComp.PlayerObject = CreateUser (evt, false);
			PlayerControllerComp.ControlRole = PlayerController.Role.LeftRight;

			ControlLeftRight.text = "Control Left/Right:" + name;
			ControlLeftRight.color = Color.green;

			ControlUpDown.text = "Control Up/Down:" + "wating...";
			ControlUpDown.color = Color.red;
			// SET UI for LeftRight user name
		}
		
		PlayerControllerComp.State = PlayerController.PlayerState.Joined;

		// create temp scene
		JoinCreateLevel(int.Parse(sceneNumber), is2nd);
	}

	private void OnOtherUserCreated(SocketIOEvent evt){
		Debug.Log ("Creating other user " + evt.data);

		// create otherUser here
		//CreateUser(evt, true);
	}

	private void OnUserMove(SocketIOEvent evt){
		//Debug.Log ("Moved data " + evt.data);

		Vector3 pos = StringToVecter3( JsonToString(evt.data.GetField("position").ToString(), "\"") );
		string id = JsonToString(evt.data.GetField("id").ToString(), "\"");

		MoveUser (id, pos);
	}

	private void OnUserDisconnect(SocketIOEvent evt){
		
		Debug.Log ("disconnected user " + evt.data);

		string name = JsonToString(evt.data.GetField("name").ToString(), "\"");

		PlayerControllerComp.ControlRole = PlayerController.Role.LeftRight;

		//Debug.Log ("disconnected user id:" + id);

		//Player disconnectedPlayer = FindUserByID (id);

		//Debug.Log ("disconnected user found:" + disconnectedPlayer);

		//Players.Remove (disconnectedPlayer);

		//Destroy (disconnectedPlayer.gameObject);

		ControlUpDown.text = "Control Up/Down:" + "waiting...";
		ControlUpDown.color = Color.red;

		ControlLeftRight.text = "Control Left/Right:" + myname;
		ControlLeftRight.color = Color.green;

	}

	private void OnOtherUserJoin(SocketIOEvent evt){
		string name = JsonToString(evt.data.GetField("name").ToString(), "\"");

		Debug.Log (name + " has join as controller to " + myname + "'s room");

		if (PlayerControllerComp.ControlRole == PlayerController.Role.UpDown) {
			ControlLeftRight.text = "Control Left/Right:" + name;
			ControlLeftRight.color = Color.green;
			ControlUpDown.text = "Control Up/Down:" + myname;
			ControlUpDown.color = Color.green;
		} 
		else if (PlayerControllerComp.ControlRole == PlayerController.Role.LeftRight) {
			ControlLeftRight.text = "Control Left/Right:" + myname;
			ControlLeftRight.color = Color.green;
			ControlUpDown.text = "Control Up/Down:" + name;
			ControlUpDown.color = Color.green;
		}

		// SET UI for LeftRight user name

	}

	/*
	private void OnChatSend(SocketIOEvent evt){

		string id = JsonToString(evt.data.GetField("id").ToString(), "\"");

		Player player = FindUserByID (id);

		//player.txtChatMsg.text = JsonToString(evt.data.GetField("chatmsg").ToString(), "\"");
	}
	*/
	private void OnRoomFull(SocketIOEvent evt){
		DialogueUI.Hide ();
		DialogueUI.ShowWithOKBtn (DialogueUIController.DialogueTypes.RoomFull);
	}

	private void OnServerFull(SocketIOEvent evt){
		DialogueUI.Hide ();
		DialogueUI.ShowWithOKBtn (DialogueUIController.DialogueTypes.ServerFull);
	}

	/*
	----------------------------------------------------------------------------------------------------------------
	GENERAL
	----------------------------------------------------------------------------------------------------------------
	*/

	private void MoveUser(string id, Vector3 position){
		Player playerComp = FindUserByID (id);
		playerComp.simulationTimer = 0f;
		playerComp.simulatedStartPos = playerComp.gameObject.transform.position;
		playerComp.simulatedEndPos = position;
	}

	private Player CreateUser(SocketIOEvent evt, bool IsSimulated){
		Debug.Log ("Creating player object: " + evt);

		Vector3 pos = StringToVecter3( JsonToString(evt.data.GetField("position").ToString(), "\"") );
		string id = JsonToString(evt.data.GetField("id").ToString(), "\"");

		GameObject go = Instantiate (PlayerPrefab, pos, Quaternion.identity);
		Player playerObject = go.GetComponent<Player> ();

		// set basics
		playerObject.IsSimulated = IsSimulated;
		playerObject.id = id;
		playerObject.simulatedEndPos = pos;

		//playerObject.txtUserName.text = name;
		//playerObject.txtChatMsg.text = "";
		//go.name = name;
		//go.transform.position = pos;

		Players.Add (playerObject);
		return playerObject;
	}

	public void JoinCreateLevel(int sceneNumber, string Is2nd){
		CurrentLevelNumber = sceneNumber;
		CurrentScene = Instantiate (ScenePrefabs[sceneNumber]);

		if (Is2nd == "true"){
			SceneOrganizer SO = CurrentScene.GetComponent<SceneOrganizer> ();
			CamObj.transform.position = SO.CameraPos.transform.position;

		}
	}

	public void GoToLevel(int sceneNumber){

		Destroy (CurrentScene);
		CurrentScene = Instantiate (ScenePrefabs[sceneNumber]);
		SceneOrganizer SO = CurrentScene.GetComponent<SceneOrganizer> ();
		CamObj.transform.position = SO.CameraPos.transform.position;
		PlayerControllerComp.PlayerObject.transform.position = SO.CharacterPos.transform.position;
		PlayerControllerComp.PlayerObject.simulatedEndPos = SO.CharacterPos.transform.position;

		Dictionary<string, string> data = new Dictionary<string, string> ();
		data ["position"] = PlayerControllerComp.PlayerObject.transform.position.x + "," + PlayerControllerComp.PlayerObject.transform.position.y + "," + PlayerControllerComp.PlayerObject.transform.position.z;

		//Debug.Log ("Attempting move:" + data["position"]);
		SocketIOComp.Emit("SERVER:MOVE", new JSONObject(data));
	}

	public void GameOver(){
		StartCoroutine (StartGameOver (3f)); 
	}

	public void Clear(){
		StartCoroutine (StartNextLevel (3f));
	}

	public void ClearAllPlayers(){
		// clear all the characters
		foreach (Player player in Players) {
			Destroy (player.gameObject);
		}
		Players.Clear ();
	}

	public void ClearScene(){

		if (CurrentScene)
			Destroy (CurrentScene);	

		ClearAllPlayers ();
	}

	/*
	----------------------------------------------------------------------------------------------------------------
	UTILITY
	----------------------------------------------------------------------------------------------------------------
	*/

	private Player FindUserByID(string id){
		foreach (Player playerComp in Players){
			if (playerComp.id == id)
				return playerComp;
		}
		return null;
	}

	string JsonToString( string target, string s){

		string[] newString = Regex.Split(target,s);

		return newString[1];

	}

	Vector3 StringToVecter3(string target ){

		Vector3 newVector;
		string[] newString = Regex.Split(target,",");
		newVector = new Vector3( float.Parse(newString[0]), float.Parse(newString[1]), float.Parse(newString[2]));

		return newVector;
	}

	IEnumerator StartNextLevel(float sec)
	{
		ClearUI.SetActive (true);

		CurrentLevelNumber = CurrentLevelNumber + 1;
		if (CurrentLevelNumber == ScenePrefabs.Length) {
			ClearUI.SetActive (false);
			ClearClearUI.SetActive (true);
		}
			
		PlayerControllerComp.State = PlayerController.PlayerState.Lobby;

		yield return new WaitForSeconds (sec);

		if (CurrentLevelNumber != ScenePrefabs.Length)
		{
			PlayerControllerComp.State = PlayerController.PlayerState.Playing;

			ClearUI.SetActive (false);


			Dictionary<string, string> data = new Dictionary<string, string> ();
			data ["scenenumber"] = CurrentLevelNumber.ToString();
			SocketIOComp.Emit("SERVER:UPDATELEVEL", new JSONObject(data));

			GoToLevel (CurrentLevelNumber);
		}


	}

	IEnumerator StartGameOver(float sec)
	{
		GameOverUI.SetActive (true);
		PlayerControllerComp.State = PlayerController.PlayerState.Lobby;

		GameOverParticle.gameObject.transform.position = PlayerControllerComp.PlayerObject.transform.position;
		GameOverParticle.Play ();

		PlayerControllerComp.PlayerObject.gameObject.SetActive (false);

		yield return new WaitForSeconds (sec);

		PlayerControllerComp.PlayerObject.gameObject.SetActive (true);

		PlayerControllerComp.State = PlayerController.PlayerState.Playing;
		GameOverUI.SetActive (false);
		GoToLevel (CurrentLevelNumber);
	}

}
