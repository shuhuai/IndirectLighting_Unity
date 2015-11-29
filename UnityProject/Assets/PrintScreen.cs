using UnityEngine;
using System.Collections;

public class PrintScreen : MonoBehaviour {

	void Update () {
		if (Input.GetKeyDown (KeyCode.Space)) {
			Application.CaptureScreenshot("Screenshot.png");
		}
	
	}
}
