using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class JoyStick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler { 

	[HideInInspector] public Vector2 inputDirection;
	[HideInInspector] public bool usingJoystick;

	public bool sticky;

	Image bgImg;
	Image joystickImg;

	void Start () {
		bgImg = GetComponent<Image> ();
		joystickImg = transform.GetChild (0).GetComponent<Image> (); 
	}

	public virtual void OnDrag (PointerEventData ped) { 
		usingJoystick = true;

		Vector2 pos;

		if (RectTransformUtility.ScreenPointToLocalPointInRectangle (bgImg.rectTransform, ped.position, ped.pressEventCamera, out pos)) {
			pos.x = pos.x / bgImg.rectTransform.sizeDelta.x;
			pos.y = pos.y / bgImg.rectTransform.sizeDelta.y;

			//keeps vector in joystick bounds
			inputDirection = new Vector2 (pos.x * 3, pos.y * 3).normalized; 

			//move joystick image
			joystickImg.rectTransform.anchoredPosition = new Vector2 (
				inputDirection.x * (bgImg.rectTransform.sizeDelta.x / 3),
				inputDirection.y * (bgImg.rectTransform.sizeDelta.y / 3)
			);
		}
	}

	public virtual void OnPointerDown (PointerEventData ped) { 
		OnDrag (ped); 
	}

	public virtual void OnPointerUp (PointerEventData ped) { 
		if (!sticky) {
			usingJoystick = false;
			joystickImg.rectTransform.anchoredPosition = Vector2.zero;
		}
	}
}
