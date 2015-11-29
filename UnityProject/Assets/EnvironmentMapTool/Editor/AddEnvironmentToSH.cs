using UnityEngine;
using UnityEditor;
using System.Collections;



///<summary>
/// A class to add lighting from an environment map to Light Probes.
///</summary>
// In Unity, Light Probes are convenient to store indirect diffuse lighting data (SH coefficients).
// However, indirect diffuse lighting data may be quite different from the environment map.
// This function is to convert a cubemap into a set of SH coefficients, and then it saves these SH coefficients into Light Probes.
// I use some code from AMD cubemapgen and the document "Stupid Spherical Harmonics (SH) Tricks".
static public class AddEnvironmentToSH
{
	
	// Constants and const arrays for this conversion.
	
	// Use 3-order SH .
	const int MAX_SH_ORDER = 3;
	const int NUM_SH_COEFFICIENT = MAX_SH_ORDER * MAX_SH_ORDER;
	
	static double[] SHBandFactor = new double[NUM_SH_COEFFICIENT]{ 1.0,
		2.0 / 3.0, 2.0 / 3.0, 2.0 / 3.0,
		1.0 / 4.0, 1.0 / 4.0, 1.0 / 4.0, 1.0 / 4.0, 1.0 / 4.0};
	static double SqrtPi = System.Math.Sqrt(System.Math.PI);
	
	// Cubemap face indexes.
	const int CP_UDIR = 0;
	const int CP_VDIR = 1;
	const int CP_FACEAXIS = 2;
	// Cubemap face directions.
	static float[, ,] sgFace2DMapping = new float[6, 3, 3]{
		//XPOS face
		{{ 0,  0, -1},   //u towards negative Z
			{ 0, -1,  0},   //v towards negative Y
			{1,  0,  0}},  //pos X axis  
		//XNEG face
		{{0,  0,  1},   //u towards positive Z
			{0, -1,  0},   //v towards negative Y
			{-1,  0,  0}},  //neg X axis       
		//YPOS face
		{{1, 0, 0},     //u towards positive X
			{0, 0, 1},     //v towards positive Z
			{0, 1 , 0}},   //pos Y axis  
		//YNEG face
		{{1, 0, 0},     //u towards positive X
			{0, 0 , -1},   //v towards negative Z
			{0, -1 , 0}},  //neg Y axis  
		//ZPOS face
		{{1, 0, 0},     //u towards positive X
			{0, -1, 0},    //v towards negative Y
			{0, 0,  1}},   //pos Z axis  
		//ZNEG face
		{{-1, 0, 0},    //u towards negative X
			{0, -1, 0},    //v towards negative Y
			{0, 0, -1}},   //neg Z axis  
	};
	
	
	///<summary>
	/// Convert an environment map into an array of SH coefficients.
	///</summary>
	static float[] Convert(Cubemap cubemap)
	{
		
		// Initialize arrays.
		double[] SHr = new double[NUM_SH_COEFFICIENT];
		double[] SHg = new double[NUM_SH_COEFFICIENT];
		double[] SHb = new double[NUM_SH_COEFFICIENT];
		double[] SHdir = new double[NUM_SH_COEFFICIENT];
		Vector4[, ,] CubeInfo = new Vector4[6, cubemap.width, cubemap.height];
		
		// Loop all texels of a cubemap to calculate every texel's direction and area.
		// Loop 6 faces.
		for (int CubeFace = 0; CubeFace < 6; CubeFace++)
		{
			// Loop every texel of a face.
			for (int v = 0; v < cubemap.height; v++)
				for (int u = 0; u < cubemap.width; u++)
			{
				// Compute direction.
				Vector3 vect = TexelCoordToVect(CubeFace, u, v, cubemap.width);
				// Compute area.
				float area = TexelCoordSolidAngle(CubeFace, u, v, cubemap.width);
				
				CubeInfo[CubeFace, u, v] = new Vector4(vect.x, vect.y, vect.z, area);
				
			}
		}
		
		// Loop all texels of a cubemap to compute SH coefficients.
		
		double weightAccum = 0.0;
		double weight = 0.0;
		Texture2D temp = new Texture2D(cubemap.width, cubemap.height, cubemap.format, false);
		
		for (int CubeFace = 0; CubeFace < 6; CubeFace++)
		{
			// Get color data from  a cubemap.
			switch (CubeFace)
			{
			case 0:
				temp.SetPixels(cubemap.GetPixels(CubemapFace.PositiveX));
				break;
			case 1:
				temp.SetPixels(cubemap.GetPixels(CubemapFace.NegativeX));
				break;
			case 2:
				temp.SetPixels(cubemap.GetPixels(CubemapFace.PositiveY));
				break;
			case 3:
				temp.SetPixels(cubemap.GetPixels(CubemapFace.NegativeY));
				break;
			case 4:
				temp.SetPixels(cubemap.GetPixels(CubemapFace.PositiveZ));
				break;
			case 5:
				temp.SetPixels(cubemap.GetPixels(CubemapFace.NegativeZ));
				break;
			}
			
			// Loop every texel of a face.
			for (int u = 0; u < cubemap.width; u++)
				for (int v = 0; v < cubemap.height; v++)
			{
				
				// Evaluate the SH basis functions.
				weight = (double)(CubeInfo[CubeFace, u, v].w);
				Vector4 texelVect = CubeInfo[CubeFace, u, v];
				EvalSHBasis(texelVect, SHdir);
				Color rgb = temp.GetPixel(u, v);
				// Accumulate SH coefficients.
				for (int i = 0; i < NUM_SH_COEFFICIENT; i++)
				{
					SHr[i] += rgb.r * SHdir[i] * weight;
					SHg[i] += rgb.g * SHdir[i] * weight;
					SHb[i] += rgb.b * SHdir[i] * weight;
				}
				weightAccum += weight;
			}
		}
		
		// Normalization - The sum of solid angle should be equal to the solid angle of the sphere (4 PI).
		for (int i = 0; i < NUM_SH_COEFFICIENT; ++i)
		{
			
			SHr[i] *= 4.0 / (weightAccum);
			SHg[i] *= 4.0 / (weightAccum);
			SHb[i] *= 4.0 / (weightAccum);
		}
		
		for (int i = 0; i < NUM_SH_COEFFICIENT; ++i)
		{
			SHr[i] = (SHr[i] * SHBandFactor[i]);
			SHg[i] = (SHg[i] * SHBandFactor[i]);
			SHb[i] = (SHb[i] * SHBandFactor[i]);
		}
		

		float[] output = new float[NUM_SH_COEFFICIENT * 3];
		
		for (int i = 0; i < NUM_SH_COEFFICIENT; i++)
		{
			output[i * 3] = (float)SHr[i] * Mathf.PI;
			output[i * 3 + 1] = (float)SHg[i] * Mathf.PI;
			output[i * 3 + 2] = (float)SHb[i] * Mathf.PI;
		}
		
		return output;
		
	}
	///<summary>
	/// This function adjusts SH coefficients to be shader-ready format before inputting SH coefficients to a shader.
	///</summary>
	static void preSHParemater(double[] input, double[] output)
	{
		
		const float C1 = 0.429043f;
		const float C2 = 0.511664f;
		const float C3 = 0.743125f;
		const float C4 = 0.886227f;
		const float C5 = 0.247708f;
		
		output[0] = input[3] * 2 * C2;
		output[1] = input[1] * 2 * C2;
		output[2] = input[2] * 2 * C2;
		output[3] = C4 * input[0] - C5 * input[6];
		output[4] = 2 * C1 * input[4];
		output[5] = 2 * C1 * input[5];
		output[6] = C3 * input[6];
		output[7] = 2 * C1 * input[7];
		output[8] = C1 * input[7];
	}
	
	///<summary>
	/// Compute spherical harmonics basis functions by a direction.
	///</summary>
	static void EvalSHBasis(Vector4 dir, double[] res)
	{
		
		double xx = dir.x;
		double yy = dir.y;
		double zz = dir.z;
		
		double[] x = new double[MAX_SH_ORDER + 1];
		double[] y = new double[MAX_SH_ORDER + 1];
		double[] z = new double[MAX_SH_ORDER + 1];
		x[0] = y[0] = z[0] = 1;
		for (int i = 1; i < MAX_SH_ORDER + 1; ++i)
		{
			x[i] = xx * x[i - 1];
			y[i] = yy * y[i - 1];
			z[i] = zz * z[i - 1];
		}
		
		res[0] = (1 / (2 * SqrtPi));
		
		res[1] = -(System.Math.Sqrt(3 / System.Math.PI) * yy) / 2;
		res[2] = (System.Math.Sqrt(3 / System.Math.PI) * zz) / 2;
		res[3] = -(System.Math.Sqrt(3 / System.Math.PI) * xx) / 2;
		
		res[4] = (System.Math.Sqrt(15 / System.Math.PI) * xx * yy) / 2;
		res[5] = -(System.Math.Sqrt(15 / System.Math.PI) * yy * zz) / 2;
		res[6] = (System.Math.Sqrt(5 / System.Math.PI) * (-1 + 3 * z[2])) / 4;
		res[7] = -(System.Math.Sqrt(15 / System.Math.PI) * xx * zz) / 2;
		res[8] = System.Math.Sqrt(15 / System.Math.PI) * (x[2] - y[2]) / 4;
	}
	
	///<summary>
	/// Compute the area for a texel of a cube map.
	///</summary
	static float TexelCoordSolidAngle(int a_FaceIdx, float a_U, float a_V, int a_Size)
	{
		Vector3[] cornerVect = new Vector3[4];
		float halfTexelStep = 0.5f;
		double texelArea = 0;
		// Compute 4 corner vectors of a texel.
		cornerVect[0] = TexelCoordToVect(a_FaceIdx, a_U - halfTexelStep, a_V - halfTexelStep, a_Size);
		cornerVect[1] = TexelCoordToVect(a_FaceIdx, a_U - halfTexelStep, a_V + halfTexelStep, a_Size);
		cornerVect[2] = TexelCoordToVect(a_FaceIdx, a_U + halfTexelStep, a_V - halfTexelStep, a_Size);
		cornerVect[3] = TexelCoordToVect(a_FaceIdx, a_U + halfTexelStep, a_V + halfTexelStep, a_Size);
		// Area of triangle defined by corners 0, 1, and 2.
		Vector3 edge0 = cornerVect[1] - cornerVect[0];
		Vector3 edge1 = cornerVect[2] - cornerVect[0];
		Vector3 xProdVect = Vector3.Cross(edge0, edge1);
		texelArea = 0.5f * System.Math.Sqrt(Vector3.Dot(xProdVect, xProdVect));
		// Area of triangle defined by corners 1, 2, and 3.
		edge0 = cornerVect[2] - cornerVect[1];
		edge1 = cornerVect[3] - cornerVect[1];
		xProdVect = Vector3.Cross(edge0, edge1);
		texelArea += 0.5f * System.Math.Sqrt(Vector3.Dot(xProdVect, xProdVect));
		
		return (float)texelArea;
	}
	
	///<summary>
	/// Compute a direction for a texel of a cube map.
	///</summary
	static Vector3 TexelCoordToVect(int a_FaceIdx, float a_U, float a_V, int a_Size)
	{
		float nvcU, nvcV;
		Vector3 output, tempVec;
		// Transform from [0..res - 1] to [- (1 - 1 / res) .. (1 - 1 / res)].
		// + 0.5f is for texel center addressing.
		nvcU = (2.0f * (a_U + 0.5f) / (float)a_Size) - 1.0f;
		nvcV = (2.0f * (a_V + 0.5f) / (float)a_Size) - 1.0f;
		
		float a = Mathf.Pow(a_Size, 2.0f) / Mathf.Pow(a_Size - 1, 3.0f);
		nvcU = a * Mathf.Pow(nvcU, 3) + nvcU;
		nvcV = a * Mathf.Pow(nvcV, 3) + nvcV;
		
		
		// U contribution.
		output = new Vector3(sgFace2DMapping[a_FaceIdx, CP_UDIR, 0], sgFace2DMapping[a_FaceIdx, CP_UDIR, 1], sgFace2DMapping[a_FaceIdx, CP_UDIR, 2]);
		output = output * nvcU;
		// V contribution.
		tempVec = new Vector3(sgFace2DMapping[a_FaceIdx, CP_VDIR, 0], sgFace2DMapping[a_FaceIdx, CP_VDIR, 1], sgFace2DMapping[a_FaceIdx, CP_VDIR, 2]);
		tempVec = tempVec * nvcV;
		output = output + tempVec;
		// Add face axis.
		tempVec = new Vector3(sgFace2DMapping[a_FaceIdx, CP_FACEAXIS, 0], sgFace2DMapping[a_FaceIdx, CP_FACEAXIS, 1], sgFace2DMapping[a_FaceIdx, CP_FACEAXIS, 2]);
		output = output + tempVec;
		
		// Normalize vector.
		return output.normalized;
	}
	static public float[] ConvertToSH(Cubemap cubemap, float scale)
	{
		return Convert(cubemap);
	}
}
