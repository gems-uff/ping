#pragma strict
var size : int = 2;
function Start () {

}

function Update () {

}

function Awake() {
		Debug.Log("SCREENSHOT");
		Application.CaptureScreenshot("CarTutorialMap.png", size);
	}