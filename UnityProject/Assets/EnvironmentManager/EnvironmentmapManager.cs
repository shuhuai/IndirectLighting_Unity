using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
///<summary>
/// A global object to manage environment maps (for image-based lighting).
///</summary>
// A manager class for setting the reflection parameters of image-based lighting materials.
// Two types of environment map:
// Global environment map: apply to all objects.
// Local environment map: apply to those objects in the influence sphere of a reflection probe.
[ExecuteInEditMode]
public class EnvironmentmapManager : MonoBehaviour
{
	
	
	
	ArrayList _objectList;	//A list to store all image-based lighting materials(using useShader) in the scene.
	ArrayList _probeList;	//A list to store all ReflectionProbe object in the scene.
	public Cubemap _environmentMap;
	RenderTexture _texBRDF;
	float _envStep;          //Mipmap step
	public Shader _useIBLShader;
	public bool _useInputBRDF;
	public Texture2D _inputBRDF;
	
	///<summary>
	/// Generate a BRDF look-up table for indirect specular lighting.
	///</summary>
	// In this project, we use a physically-based shading shader with GGX distribution,
	// and this shader uses the image-based lighting for specular indirect lighting.
	// To integrate physically-based image-based lighting, it requires a BRDF look-up table.
	// This look-up table is a part of the approximation of numerical integration of image-based lighting.
	// I use importance sampling to calculate the integration. This method is introduced in the note "Real Shading in Unreal Engine 4".
	void GenerateEnvBRDF()
	{
		// The size of BRDF look-up table texture. With a larger size, the value of BRDF become more accurate.
		int resBRDF = 512;
		_texBRDF = new RenderTexture(resBRDF, resBRDF, 0, RenderTextureFormat.RGHalf);  //32bit format, 16bit per channel.
		_texBRDF.enableRandomWrite = true;   //Set enable randomWrite to use unordered access view.
		_texBRDF.Create();
		
		// Load a compute shader to generate a BRDF look-up table by importance sampling.
		ComputeShader cs = (ComputeShader)Resources.Load("EnvironmentBRDF");
		// Set parameters of this compute shader.
		cs.SetInt("gWidth", resBRDF);
		cs.SetTexture(0, "gBRDFTex", _texBRDF);
		// Run this compute shader to generate a BRDF look-up table texture.
		cs.Dispatch(0, (resBRDF * resBRDF) / 64, 1, 1);    //number of threads:64.
		
		// Set this look-up table to all shaders.
		Shader.SetGlobalTexture("_EnvBRDF", _texBRDF);
		
	}
	/// <summary>
	/// Unity callback,
	/// 
	/// run this function after loading this component.
	/// </summary>
	void Start()
	{
		if (!_useInputBRDF)
		{
			GenerateEnvBRDF();
		}
		else
		{
			Shader.SetGlobalTexture("_EnvBRDF", _inputBRDF);
		}
		_objectList = new ArrayList();
		_probeList = new ArrayList();
		
		// Store all reflection probes and image-based lighting objects to two lists.
		BuildList();
		CheckIntersection();
	}
	
	
	/// <summary>
	/// Unity callback.
	/// 
	/// 1.Check the intersection between reflection probes and image-based lighting objects.
	/// 2.Update shader parameters.
	/// </summary>
	void Update()
	{
		
		CheckIntersection();
		_envStep = 1 / Mathf.Log(_environmentMap.width, 2);
		if (!_useInputBRDF)
		{
			Shader.SetGlobalTexture("_EnvBRDF", _texBRDF);
		}
		else
		{
			Shader.SetGlobalTexture("_EnvBRDF", _inputBRDF);
		}
		Shader.SetGlobalTexture("_EnvCube", _environmentMap);
		Shader.SetGlobalFloat("_CubeMipmapStep", _envStep);
	}
	
	/// <summary>
	/// Add image-based lighting objects to a list;
	/// Add reflection probes to another list.
	/// </summary>
	void BuildList()
	{
		MeshRenderer[] meshrenderers = GameObject.FindObjectsOfType<MeshRenderer>();
		
		for (int i = 0; i < meshrenderers.Length; i++)
		{
			if (meshrenderers[i].sharedMaterial.shader.name == _useIBLShader.name)
			{
				_objectList.Add(meshrenderers[i].gameObject);
			}
		}
		ReflectionProbe[] probes = GameObject.FindObjectsOfType<ReflectionProbe>();
		
		for (int i = 0; i < probes.Length; i++)
		{
			if (!probes[i].World)
			{
				_probeList.Add(probes[i]);
			}
		}
		
	}
	
	/// <summary>
	/// Check the intersections between reflection probes and image-based lighting objects.
	/// </summary>
	public void CheckIntersection()
	{
		if (_objectList != null)
		{
			object[] objects = _objectList.ToArray();
			
			for (int i = 0; i < _probeList.Count; i++)
			{
				ReflectionProbe probe = (ReflectionProbe)_probeList[i];
				probe.ClearList();     //Clear old list.
				for (int j = 0; j < objects.Length; j++)
				{
					GameObject obj = (GameObject)objects[j];
					// Use InfluenceRadius to determine the intersection.
					bool intersect = probe._influenceRadius.bounds.Intersects(obj.renderer.bounds);
					
					if (intersect)
					{
						// Add the object to a reflection probe, so the reflection probe can set reflection parameters to this object.
						probe.AddObject(obj);
					}
				}
				
			}
		}
		
	}
	
	
	
}
