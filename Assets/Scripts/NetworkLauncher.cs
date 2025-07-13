using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Com.Interstellar.CarsonStark {
	public class NetworkLauncher : Photon.PunBehaviour {

		public Text connecting;
		public InputField nameFeild;
		public InputField sizeFeild;

		int maxPlayersPerGame;
		bool CanBattle;
		string requestedGamemode;

		void Awake () {
			PhotonNetwork.autoJoinLobby = false;
			// #Critical
			// this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
			PhotonNetwork.automaticallySyncScene = true;

			maxPlayersPerGame = 2;

			GlobalVariables.online = false;
		}

		void Start () {
			Connect ();
		}

		void Connect () {
			PhotonNetwork.ConnectUsingSettings("1");
		}

		public void Multiplayer () {
			CanBattle = true;
			connecting.text = "Connecting...";
			Connect (); 
		}

		public void NameChanged () {
			PhotonNetwork.player.NickName = nameFeild.text;
		}

		public void RoomSizeChanged () {
			maxPlayersPerGame = int.Parse (sizeFeild.text);
		}

		#region Photon.PunBehaviour CallBacks


		public override void OnConnectedToMaster()
		{
			Debug.Log("DemoAnimator/Launcher: OnConnectedToMaster() was called by PUN");
			GlobalVariables.online = false;

			if (CanBattle) {
				connecting.text = "Joining...";
				PhotonNetwork.JoinRandomRoom ();

				CanBattle = false;
			} 
			else { 
				connecting.text = "Play";
			}
		}


		public override void OnDisconnectedFromPhoton()
		{
			Debug.LogWarning("DemoAnimator/Launcher: OnDisconnectedFromPhoton() was called by PUN");
			GlobalVariables.online = false;

			connecting.text = "No Connection";

			CanBattle = false;

			//FUTURE PLANS:
			//alert user that they are not connected to internet
			//guide to bots?
		}

		public override void OnPhotonRandomJoinFailed (object[] codeAndMsg)
		{
			string roomName = requestedGamemode + ": " + System.DateTime.UtcNow.ToString() + " : " + Time.deltaTime;

			Debug.Log("DemoAnimator/Launcher:OnPhotonRandomJoinFailed() was called by PUN. No random room available, so we create one: " + roomName);
			// #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room

			PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = (byte)maxPlayersPerGame}, null);
		}

		public override void OnJoinedRoom()
		{
			Debug.Log("DemoAnimator/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");
			SceneManager.LoadScene (1);
		}


		#endregion
	}
}

