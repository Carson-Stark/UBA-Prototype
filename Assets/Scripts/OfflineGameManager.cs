using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OfflineGameManager : MonoBehaviour {

	public GameObject player_offline;

	public int scoreNeededToWin;
	public Text winText;
	public Text Scores;
	public Transform[] team1SpawnPoints;
	public Transform[] team2SpawnPoints;

	public List<GameObject> charactersInGame;
		
	Transform AssignSP (int team) {
		Transform[] spawnpoints = team1SpawnPoints;

		if (team == 1)
			spawnpoints = team1SpawnPoints;
		else if (team == 2)
			spawnpoints = team2SpawnPoints;
		else if (team == 0) { //team 0 is free-for-all
			spawnpoints = new Transform[team1SpawnPoints.Length + team2SpawnPoints.Length];
			Array.Copy (team1SpawnPoints, spawnpoints, team1SpawnPoints.Length); 
			Array.Copy (team2SpawnPoints, 0, spawnpoints, team1SpawnPoints.Length, team2SpawnPoints.Length);
			Debug.Log (spawnpoints.Length); 
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
}
