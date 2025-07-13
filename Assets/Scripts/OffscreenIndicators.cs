using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffscreenIndicators : MonoBehaviour {

	public GameObject[] indicators;
	public Canvas can;
	public float indicatorBuffer;

	GameManager manager;

	float XBuffer;
	float YBuffer;

	void Start () {
		manager = Camera.main.GetComponent<GameManager> (); 

		XBuffer = Screen.width / can.scaleFactor / indicatorBuffer;
		YBuffer = Screen.height / can.scaleFactor / indicatorBuffer;
	}

	void Update () {
		for (int i = 0; i < indicators.Length; i++) {
			if (manager.charactersInGame.Count <= i || i == manager.charactersInGame.IndexOf(manager.localPlayer))
				indicators [i].SetActive (false); 
			else {
				if (!indicators [i].activeSelf)
					indicators [i].SetActive (true); 

				Vector2 indicatorPosition = Camera.main.WorldToScreenPoint (manager.charactersInGame [i].transform.position) / can.scaleFactor;
				indicatorPosition.x = Mathf.Clamp (indicatorPosition.x, XBuffer, Screen.width / can.scaleFactor - XBuffer); 
				indicatorPosition.y =  Mathf.Clamp (indicatorPosition.y, YBuffer, Screen.height / can.scaleFactor - YBuffer); 

				indicators [i].GetComponent<RectTransform> ().anchoredPosition = indicatorPosition;
				indicators [i].GetComponent<RectTransform> ().rotation = Quaternion.FromToRotation (Vector2.up,manager.charactersInGame [i].transform.position -  manager.localPlayer.transform.position); 
			}
		}
	}
}
