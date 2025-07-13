using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class LobbyManager : MonoBehaviour {

	public GameObject[] playerReps;
	public Text playersLeft;

	List<PhotonPlayer> playersInLobby = new List<PhotonPlayer>();

	void Start () {
		Debug.Log (PhotonNetwork.masterClient.NickName + " is the master client");
		PhotonView photonView = PhotonView.Get(this);
		photonView.RPC ("playerJoinedLobby", PhotonTargets.AllBufferedViaServer, PhotonNetwork.player);
	}

	void Update () {
		playersLeft.text = "Waiting for more players...  " + playersInLobby.Count + "/" + PhotonNetwork.room.MaxPlayers;
	}

	[PunRPC]
	void playerJoinedLobby(PhotonPlayer player) {
		Debug.Log(player.NickName + "has joined the lobby");

		playersInLobby.Add (player);
		GameManager.playersInGame = playersInLobby;

		playerReps[playersInLobby.Count - 1].transform.GetComponentInChildren<Text>().text = player.NickName;

		RoomInfo room = PhotonNetwork.room;
		if (playersInLobby.Count >= room.MaxPlayers && PhotonNetwork.isMasterClient) {
			Debug.Log ("Reached max players: staring match");
			playersLeft.text = "Loading Game...";
			PhotonNetwork.LoadLevel (2);
		}
	}
}
