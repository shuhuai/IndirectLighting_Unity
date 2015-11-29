using UnityEngine;
using System.Collections;

///<summary>
/// A class to store reflection data.
///</summary>
// This class saves an environment map as a local reflection map or a world environment map.
// Every reflection probe has a range of influence.
[ExecuteInEditMode]
public class ReflectionProbe : MonoBehaviour
{
	ArrayList objectList;       //Save the objects with the GGX shader in the range.
	
	public Cubemap _environment;    //Environment map.
	public int _cubemapSize = 128;  //CubeMap size.
	
	// A bounding box to compute parallax-corrected cubemap.
	public BoxCollider _boundingBox;
	public Vector3 _AABBmax;
	public Vector3 _AABBmin;
	
	// A SphereCollider to define the range of influence for calculating weights to blend world and local environment maps.
	public SphereCollider _influenceRadius;
	public float _innerRange;
	public float _outerRange;
	
	
	
	public bool World = false;    //Is a world environment map?
	public bool Capture = true;   //Capture environment map?
	
	///<summary>
	/// Unity callback,
	/// 
	/// initialize all game components.
	///</summary>
	void Start()
	{
		if (objectList == null)
		{
			objectList = new ArrayList();
		}
		
		_boundingBox = ((BoxCollider)gameObject.GetComponent<BoxCollider>());
		
		if (_boundingBox == null)
		{
			_boundingBox = (BoxCollider)gameObject.AddComponent<BoxCollider>();
		}
		
		_influenceRadius = ((SphereCollider)gameObject.GetComponent<SphereCollider>());
		
		if (_influenceRadius == null)
		{
			_influenceRadius = (SphereCollider)gameObject.AddComponent<SphereCollider>();
		}
		_influenceRadius.isTrigger = true;
		if (gameObject.camera == null)
		{
			gameObject.AddComponent<Camera>();
		}
		_AABBmax = _boundingBox.bounds.max;
		_AABBmin = _boundingBox.bounds.min;
		camera.enabled = false;
		
		// Set the global environment map to the manager object.
		if (World)
		{
			EnvironmentmapManager manager = (EnvironmentmapManager)FindObjectOfType(typeof(EnvironmentmapManager));
			manager._environmentMap = _environment;
		}
	}
	
	///<summary>
	/// Unity callback,
	/// 
	/// validate inner radius less than outer range.
	/// Set SphereCollider equal to outer range.
	///</summary>
	void OnValidate()
	{
		if (World)
		{
			EnvironmentmapManager manager = (EnvironmentmapManager)FindObjectOfType(typeof(EnvironmentmapManager));
			manager._environmentMap = _environment;
		}

		if (_influenceRadius != null)
		{
			_influenceRadius.radius = _outerRange;
		}
		if (_innerRange > _outerRange)
		{
			_innerRange = _outerRange;
			
		}
		if (_innerRange < _outerRange / 2)
		{
			_innerRange = _outerRange / 2;
		}
	}
	
	///<summary>
	/// Unity callback,
	/// 
	/// update reflection parameters for local reflection.
	///</summary>
	void Update()
	{
		_AABBmax = _boundingBox.bounds.max;
		_AABBmin = _boundingBox.bounds.min;
		if (Application.isPlaying)
		{
			SetReflectionParameters();
		}
		else
		{
			SetPreviewReflectionParameters();
		}
	}
	
	///<summary>
	/// Clear all image-based lighting objects.
	///</summary>
	public void ClearList()
	{
		if (objectList != null)
		{
			objectList.Clear();
		}
	}
	
	///<summary>
	/// Add a new object to this reflection probe.
	///</summary>
	public void AddObject(GameObject obj)
	{
		if (objectList == null)
		{
			objectList = new ArrayList();
		}
		objectList.Add(obj);
		
	}
	
	///<summary>
	// Set reflection parameters.
	///</summary>
	void SetReflectionParameters()
	{
		for (int i = 0; i < objectList.Count; i++)
		{
			GameObject obj = (GameObject)objectList[i];
			
			obj.renderer.material.SetVector("_EnviCubeMapPos", (Vector4)(gameObject.transform.position));
			obj.renderer.material.SetVector("_BBoxMin", (Vector4)(_AABBmin));
			obj.renderer.material.SetVector("_BBoxMax", (Vector4)(_AABBmax));
			obj.renderer.material.SetTexture("_LocalEnv", _environment);
			
			float mipLevelNum = Mathf.Log(_environment.width, 2);
			obj.renderer.material.SetFloat("_CubeMipmapStep", 1 / mipLevelNum);
			obj.renderer.material.SetFloat("_innerRange", _innerRange);
			obj.renderer.material.SetFloat("_outerRange", _outerRange);
		}
	}
	
	///<summary>
	// Set reflection parameters. This function is for editor mode.
	///</summary>
	void SetPreviewReflectionParameters()
	{
		if (objectList == null)
		{
			objectList = new ArrayList();
		}
		for (int i = 0; i < objectList.Count; i++)
		{
			GameObject obj = (GameObject)objectList[i];
			
			obj.renderer.sharedMaterial.SetVector("_EnviCubeMapPos", (Vector4)(gameObject.transform.position));
			obj.renderer.sharedMaterial.SetVector("_BBoxMin", (Vector4)(_AABBmin));
			obj.renderer.sharedMaterial.SetVector("_BBoxMax", (Vector4)(_AABBmax));
			obj.renderer.sharedMaterial.SetTexture("_LocalEnv", _environment);
			
			float mipLevelNum = Mathf.Log(_environment.width, 2);
			obj.renderer.sharedMaterial.SetFloat("_CubeMipmapStep", 1 / mipLevelNum);
			obj.renderer.sharedMaterial.SetFloat("_innerRange", _innerRange);
			obj.renderer.sharedMaterial.SetFloat("_outerRange", _outerRange);
		}
	}
	
	
	///<summary>
	/// Unity callback,
	/// 
	/// draw the range of influence.
	/// Capture an environment map by camera.
	///</summary>
	void OnDrawGizmosSelected()
	{
		gameObject.transform.rotation = Quaternion.identity;
		if (Capture)
		{
			captureCamera();
		}
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, _innerRange);
	}
	
	///<summary>
	/// Use the camera component in this reflection probe to capture a new cubemap as an environment map.
	///</summary>
	public void captureCamera()
	{
		_environment = new Cubemap(_cubemapSize, TextureFormat.RGB24, true);
		gameObject.camera.RenderToCubemap(_environment);
		_environment.name = gameObject.name;
	}
}
