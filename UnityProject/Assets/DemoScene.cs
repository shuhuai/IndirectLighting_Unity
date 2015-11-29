using UnityEngine;
using System.Collections;

public class DemoScene : MonoBehaviour {

	public GameObject demoObject;
	public Material[] demoMaterials;
	public Camera[] demoCamera; 
	public float scaleUI=1.0f;
	public float minDistance=1.0f;
	public float maxDistance=10.0f;
	Vector3 orgDistance;
	// Use this for initialization
	void Start () {
		orgDistance=demoObject.transform.position-demoCamera[0].transform.position;
	}
	 int selectedMaterial = 0;
	 int selectedScene = 0;
	int lastSelected;

	void OnGUI() {
		string[] materialStrings=new string[demoMaterials.Length];
		for(int i=0;i<materialStrings.Length;i++)
		{
			materialStrings[i]=demoMaterials[i].name;
		}
		GUI.backgroundColor=Color.white;
		GUI.Label(new Rect(15, 5, 150*scaleUI, 40*scaleUI),"Material:");
		selectedMaterial = GUI.Toolbar(new Rect(25, 25, 350*scaleUI, 40*scaleUI), selectedMaterial, materialStrings);

		string[] scenesStrings=new string[demoCamera.Length];
		for(int i=0;i<scenesStrings.Length;i++)
		{
			scenesStrings[i]=demoCamera[i].name;
		}
		GUI.Label(new Rect(15, 65, 150*scaleUI, 40*scaleUI),"Scene:");
		selectedScene = GUI.Toolbar(new Rect(25, 85, 350*scaleUI, 40*scaleUI), selectedScene, scenesStrings);
	
	
		if (lastSelected != selectedScene) {
						demoObject.transform.position = demoCamera [selectedScene].transform.position + orgDistance;
						Camera.main.CopyFrom(demoCamera[selectedScene]);
			AlignInfoBase baseInfo=Camera.main.GetComponent<AlignInfoBase>();
			if(baseInfo!=null)
			{
				baseInfo.enabled=false;
				baseInfo.enabled=true;
			}
				}
		lastSelected = selectedScene;
		demoObject.renderer.sharedMaterial=demoMaterials[selectedMaterial];

	}
	// Update is called once per frame
	void Update () {
		if (Input.GetMouseButton (0)) {
			Camera.main.transform.RotateAround (demoObject.transform.position, Vector3.up, Time.deltaTime * Input.GetAxis ("Mouse X")*50);
						
				}

		Camera.main.transform.Translate (Vector3.forward * Input.GetAxis ("Mouse ScrollWheel"));
		float distance = Vector3.Distance (Camera.main.transform.position, demoObject.transform.position);
		if ( (distance < minDistance)) {
			Camera.main.transform.Translate((distance-minDistance)*Vector3.forward);
			//Camera.main.transform.Translate (-Vector3.forward * Input.GetAxis ("Mouse ScrollWheel"));
			}
		if ( (distance > maxDistance)) {
			Camera.main.transform.Translate((distance-maxDistance)*Vector3.forward);
			//Camera.main.transform.Translate (-Vector3.forward * Input.GetAxis ("Mouse ScrollWheel"));
		}

	
   	
	}
}
