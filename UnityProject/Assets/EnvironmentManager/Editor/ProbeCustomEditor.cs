using UnityEngine;
using UnityEditor;
using System.Collections;

///<summary>
/// Editing tool for reflection probes
///</summary>
// This class can call two functions for computing lighting data
// 1. Pre-filtered cubemap for image-based lighting
// 2. Use environment maps to add lighting to Light Probes
[CustomEditor(typeof(ReflectionProbe))]
public class ProbeCustomEditor : Editor
{
	float fStrength = 0.25f;
	
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		DrawDefaultInspector();
		ReflectionProbe myScript = (ReflectionProbe)target;
		
		// Pre-calculate the sum of incoming light of environment map with different roughness by importance sampling to integrate image-based lighting equation
		// This method is an approximation for physically-based image-based lighting.
		if (GUILayout.Button("Prefilter Environment map"))
		{
			PrefilterEnvmap.PreFilterEnviromentMap(myScript._environment);
			myScript.Capture = false;
		}
		
		// Convert environment maps to Light Probes while we want that indirect diffuse lighting is similar to indirect specular lighting
		// Light Probes are baked by Unity, and the difference between environment lighting and Light Probes may be big.
		// Therefore, the function converts an environment map to SH coefficients, and then it feeds the coefficients into Unity build-in SH data structure (Light Probes)
		if (GUILayout.Button("Add Environment light to Light probe"))
		{
			// Convert environment map to SH coefficients
			float[] SH;
			SH = AddEnvironmentToSH.ConvertToSH(myScript._environment, 1);
			
			// Blend the SH coefficients of original Light Probes and these SH coefficients from environment map by the variable "fStrength" 
			float[] SHcoeff = LightmapSettings.lightProbes.coefficients;
			int numberProbes = SHcoeff.Length / SH.Length;
			int index = 0;
			for (int i = 0; i < numberProbes; i++)
			{
				for (int j = 0; j < SH.Length; j++)
				{
					SHcoeff[index] = Mathf.Lerp(SHcoeff[index], SH[j], fStrength);
					index++;
				}
				
			}
			LightmapSettings.lightProbes.coefficients = SHcoeff;
		}
		GUILayout.Label("Additive Strength");
		fStrength = GUILayout.HorizontalSlider(fStrength, 0, 1);
		
		
		
	}
}