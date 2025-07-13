using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class PlayerInputOffline : MonoBehaviour {

	public JoyStick moveJoystick;
	public JoyStick shootJoystick;
	public button granadeButton;
	public button diveButton;

	public input currentInput;

	void Update () {
		currentInput = new input ();
		currentInput.moveDirection = moveJoystick.inputDirection;
		currentInput.moveing = moveJoystick.usingJoystick;

		currentInput.shootDirection = shootJoystick.inputDirection;
		currentInput.shooting = shootJoystick.usingJoystick;

		currentInput.aimingGranade = granadeButton.activated;
		currentInput.readyToDive = diveButton.activated;
	}
}
