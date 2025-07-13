using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Granade : MonoBehaviour {

	public bool online;
	
	public GameObject aimingInd;
	public GameObject granade;

	public float throwDistance;

	PlayerInput player_input;
	PlayerInputOffline player_input_off;
	bool aiming;
	GameObject aim;

	void Start () {
		aiming = false;
		if (online)
			player_input = GetComponent<PlayerMovement> ().player_input;
		else 
			player_input_off = GetComponent<PlayerMovementOffline> ().player_input;
	}	

	void Update () {
		if (online) {
			if (player_input.mine || PhotonNetwork.isMasterClient)
				CheckForThrow (); //prediction
		} 
		else
			CheckForThrow (); 
	}

	void CheckForThrow () {
		input currentInput;
		if (online)
			currentInput = player_input.currentInput;
		else
			currentInput = player_input_off.currentInput;
			
		if (currentInput.aimingGranade) {
			if (!aiming && currentInput.shooting) {
				if (player_input.mine)
					aimingInd.SetActive (true); 
				
				aiming = true;
			} 
			else if (aiming) {  
				Vector2 shootDir = player_input.currentInput.shootDirection;
				float throwDist = Mathf.Clamp (shootDir.magnitude, 0.25f, Mathf.Infinity) * throwDistance;

				if (!online || player_input.mine) {
					aimingInd.transform.position = (Vector2)transform.position + shootDir.normalized * throwDist;
					aimingInd.transform.rotation = Quaternion.FromToRotation (Vector3.up, shootDir.normalized);
					aimingInd.transform.localScale = new Vector2 (Mathf.Pow (0.5f, throwDist / 2), throwDist / 2); 
				}

				if (!currentInput.shooting) {
					if (!online || player_input.mine) {
						aimingInd.SetActive (false); 					
						player_input.granadeButton.activated = false;
					}

					aiming = false;

					if (online) {
						if (PhotonNetwork.isMasterClient)
							PhotonView.Get (this).RPC ("ThrowGranade", PhotonTargets.All, (Vector2)transform.position + shootDir.normalized * throwDist * 2, false);
						else if (player_input.mine)
							ThrowGranade ((Vector2)transform.position + shootDir.normalized * throwDist * 2, true); //prediciton
					} 
					else
						ThrowGranade ((Vector2)transform.position + shootDir.normalized * throwDist * 2, true); //prediciton
				}
			}
		} 
	}

	[PunRPC]
	void ThrowGranade (Vector2 targetPos, bool prediciton) {
		if (!online || !player_input.mine || PhotonNetwork.isMasterClient || prediciton) {
			GameObject _granade = (GameObject)Instantiate (granade, transform.position, transform.rotation); 
			_granade.GetComponent<granadeObj> ().target = targetPos; 
			_granade.GetComponent<granadeObj> ().owner = transform.parent.GetComponent<PhotonView>().owner;  
			_granade.GetComponent<granadeObj> ().mine = player_input.mine;
		}
	}
}
