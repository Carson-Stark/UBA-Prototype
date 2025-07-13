using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class button : MonoBehaviour, IPointerDownHandler {

	public bool activated;

	public virtual void OnPointerDown (PointerEventData ped) {
		activated = !activated;
	}
}
