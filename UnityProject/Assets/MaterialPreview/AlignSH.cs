using UnityEngine;
using System.Collections;

///<summary>
/// The class is for previewing indirect diffuse lighting in play mode.
///</summary>
public class AlignSH : AlignInfoBase {
	
	static float        s_fSqrtPI   = (float)Mathf.Sqrt(Mathf.PI);
	static float        fC0         = 1.0f / (2.0f*s_fSqrtPI);
	static float        fC1         = (float)Mathf.Sqrt(3.0f)  / (3.0f*s_fSqrtPI);
	static float        fC2         = (float)Mathf.Sqrt(15.0f) / (8.0f*s_fSqrtPI);
	static float        fC3         = (float)Mathf.Sqrt(5.0f)  / (16.0f*s_fSqrtPI);
	static float        fC4         = 0.5f * fC2;
	
	new void Update()
	{
		
		base.Update ();
	}

    /// <summary>
    /// Set parameters for an indirect diffuse lighting shader.
    /// </summary>
	protected override void SetMaterial(GameObject selectedObj)
	{
		float[]      aSample     = new float[27]; 
		Vector4[]    avCoeff     = new Vector4[7];   //SH coefficients in 'shader-ready' format.
		
		int x=0;
		x++;

        // Load the SH coefficients according to the position of the selected object.
		LightmapSettings.lightProbes.GetInterpolatedLightProbe(selectedObj.transform.position,selectedObj.renderer, aSample );

		// Convert coefficients into shader-ready format coefficients.
		for ( int iC=0; iC<3; iC++ )   
		{              
			avCoeff[iC].x =-fC1 * aSample[iC+9];
			avCoeff[iC].y =-fC1 * aSample[iC+3];
			avCoeff[iC].z = fC1 * aSample[iC+6];               
			avCoeff[iC].w = fC0 * aSample[iC+0] - fC3*aSample[iC+18];
		}
		
		for ( int iC=0; iC<3; iC++ )   
		{
			avCoeff[iC+3].x =        fC2 * aSample[iC+12];
			avCoeff[iC+3].y =       -fC2 * aSample[iC+15];
			avCoeff[iC+3].z = 3.0f * fC3 * aSample[iC+18];
			avCoeff[iC+3].w =       -fC2 * aSample[iC+21];
		}
		
		avCoeff[6].x = fC4 * aSample[24];
		avCoeff[6].y = fC4 * aSample[25];
		avCoeff[6].z = fC4 * aSample[26];
		avCoeff[6].w = 1.0f;

        // Set coefficients to the custom shader.
        // For order-3 spherical harmonics, it inputs 27 float variables.
		_demoMaterial.SetVector("cAr",avCoeff[0]);
		_demoMaterial.SetVector("cAg",avCoeff[1]);
		_demoMaterial.SetVector("cAb",avCoeff[2]);
		_demoMaterial.SetVector("cBr",avCoeff[3]);
		_demoMaterial.SetVector("cBg",avCoeff[4]);
		_demoMaterial.SetVector("cBb",avCoeff[5]);
		_demoMaterial.SetVector("cC",avCoeff[6]);
		
	}
}
