using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
	public int gameSendRate;
	public int scoreNeededToWin;
	public Text winText;
	public Text Scores;
	public Transform[] team1SpawnPoints;
	public Transform[] team2SpawnPoints;

	public static List<PhotonPlayer> playersInGame;
	public List<GameObject> charactersInGame;

	public List<GameState> pastGameStates;

	public GameObject localPlayer;

	void Awake () {
		PhotonNetwork.sendRate = gameSendRate * 2;
		PhotonNetwork.sendRateOnSerialize = gameSendRate;

		PhotonNetwork.player.SetScore (0); 

		charactersInGame = new List<GameObject> ();
		pastGameStates = new List<GameState> ();
	}

	void Start () {
		GameObject player;

		if (GlobalVariables.online)
			player = (GameObject) PhotonNetwork.Instantiate ("Player", Vector2.zero, Quaternion.identity, 0);
	}

	[PunRPC]
	void EndGame (PhotonPlayer winner) {
		Debug.Log (winner.NickName + " wins"); 
		if (winner == PhotonNetwork.player)
			winText.text = "YOU WON!";
		else
			winText.text = winner.NickName + " wins!";

		Invoke ("LeaveRoom", 5); 
	}
		
	public Transform AssignSP (int team) {
		Transform[] spawnpoints = team1SpawnPoints;

		if (team == 1)
			spawnpoints = team1SpawnPoints;
		else if (team == 2)
			spawnpoints = team2SpawnPoints;
		else if (team == 0) { //team 0 is free-for-all
			spawnpoints = new Transform[team1SpawnPoints.Length + team2SpawnPoints.Length];
			Array.Copy (team1SpawnPoints, spawnpoints, team1SpawnPoints.Length); 
			Array.Copy (team2SpawnPoints, 0, spawnpoints, team1SpawnPoints.Length, team2SpawnPoints.Length);
		} 
		else {
			Debug.LogError ("Team does not match any spawn points!"); 
			return null;
		}
			
		if (charactersInGame.Count <= 0) {
			Debug.Log ("currently no players : choosing random spawn point");

			return spawnpoints [UnityEngine.Random.Range (0, spawnpoints.Length)]; 
		}

		//find the spawn point furthest away from all enemy players
		float farthestDistFromClosestEnemy = 0;
		Transform farthestSP = spawnpoints[0];
		foreach (Transform Sp in spawnpoints) {
			
			//find the closest enemy
			float closestDist = Mathf.Infinity;
			foreach (GameObject enemy in charactersInGame) {
				//if the distance is closer than the closest one we've found : that is now the closest one we've found
				if (Vector2.Distance (Sp.position, enemy.transform.position) < closestDist)
					closestDist = Vector2.Distance (Sp.position, enemy.transform.position);
			}
		
			//if our closest enemy is further away from the furthest enemy we've found : that is down the furthest one we've found
			if (closestDist > farthestDistFromClosestEnemy) {
				farthestDistFromClosestEnemy = closestDist;
				farthestSP = Sp;
			}
		}

		return farthestSP;
	}

	void Update () {
		//clear out gamestate after it's one second old
		GameState[] copyOfPastGameStates = pastGameStates.ToArray ();
		foreach (GameState gamestate in copyOfPastGameStates) {
			if (gamestate.time < PhotonNetwork.time - 1)
				pastGameStates.Remove (gamestate);
		}

		saveGameState (); 

		string scoreText = "";
	
		foreach (PhotonPlayer player in playersInGame) {
			if (player.GetScore () >= scoreNeededToWin && PhotonNetwork.isMasterClient) {
				PhotonView.Get (this).RPC ("EndGame", PhotonTargets.All, player); 
			}

			scoreText += player.NickName + "[" + player.GetScore () + "]  ";
		}
	
		Scores.text = scoreText;
	}
			
	// Called when the local player left the room. We need to load the launcher scene.
	public void OnLeftRoom() {
		SceneManager.LoadScene(0);
	}

	public void LeaveRoom() {
		PhotonNetwork.LeaveRoom();
	}

	public void saveGameState () {
		GameState currentGameState = new GameState ();
		currentGameState.time = PhotonNetwork.time;
		currentGameState.positions = new List<Vector2> ();
		currentGameState.rotations = new List<Quaternion> (); 

		foreach (GameObject player in charactersInGame) {
			currentGameState.positions.Add (player.transform.position);
			currentGameState.rotations.Add (player.transform.rotation);
		}

		pastGameStates.Add (currentGameState); 
	}

	public GameState FindGameState (double time) {
		GameState closestState = new GameState ();
		closestState.time = 0;

		//find the gamestate with a time closest to the requested time
		foreach (GameState gamestate in pastGameStates) {
			if (Mathf.Abs ((float)(time - closestState.time)) > Mathf.Abs ((float)(time - gamestate.time)))
				closestState = gamestate;
		}
			
		return closestState;
	}

	public void rewindGame (double time) {
		Debug.Log ("rewind from " + PhotonNetwork.time + " to " + time); 

		foreach (GameObject player in charactersInGame) {
			int index = charactersInGame.IndexOf (player);

			player.transform.position = FindGameState (time).positions[index];
			player.transform.rotation = FindGameState (time).rotations[index];
		}
	}

	public void MigrateHost () {
		Debug.Log ("Migrating Host"); 

		int rnd = 0;
		do {
			rnd = UnityEngine.Random.Range (0, PhotonNetwork.room.PlayerCount); 
		} while (rnd == playersInGame.IndexOf (PhotonNetwork.masterClient)); 

		Debug.Log (rnd); 
		Debug.Log (playersInGame.Count); 
	
		PhotonNetwork.SetMasterClient (playersInGame[rnd]); 
	}
}
