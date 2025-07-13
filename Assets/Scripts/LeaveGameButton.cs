using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaveGameButton : MonoBehaviour {

	public void Click () {
		Camera.main.GetComponent<GameManager>().LeaveRoom (); 
	}
}
