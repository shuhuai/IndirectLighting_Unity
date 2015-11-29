using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Runtime.InteropServices;

///<summary>
/// Prefilter a cubemap for the image-based lighting equation.
///</summary>
// Use importance sampling to filter a cubemap.
// For GGX normal distribution, the lighting data is different if roughness is different,
// so the function filters the cube map many times for different roughness.
static public class PrefilterEnvmap
{
	// Face direction data for accessing cubemaps.
	static float[,] sgFaceInput = new float[18, 3]{
		// XPOS face
		{ 0,  0, -1},   //u towards negative Z
		{ 0, -1,  0},   //v towards negative Y
		{1,  0,  0},  //pos X axis  
		// XNEG face
		{0,  0,  1},   //u towards positive Z
		{0, -1,  0},   //v towards negative Y
		{-1,  0,  0},  //neg X axis       
		// YPOS face
		{1, 0, 0},     //u towards positive X
		{0, 0, 1},     //v towards positive Z
		{0, 1 , 0},   //pos Y axis  
		// YNEG face
		{1, 0, 0},     //u towards positive X
		{0, 0 , -1},   //v towards negative Z
		{0, -1 , 0},  //neg Y axis  
		// ZPOS face
		{1, 0, 0},     //u towards positive X
		{0, -1, 0},    //v towards negative Y
		{0, 0,  1},   //pos Z axis  
		// ZNEG face
		{-1, 0, 0},    //u towards negative X
		{0, -1, 0},    //v towards negative Y
		{0, 0, -1},   //neg Z axis  
	};
	
	
	///<summary>
	/// Input a cubemap, and then prefilter this cubemap for image-based lighting equation.
	///</summary>
	static public void PreFilterEnviromentMap(Cubemap cubemap)
	{
		if (cubemap)
		{
			int cube_width = cubemap.width;
			
			Vector3 vec3 = new Vector3();
			// Create a read buffer to store cubemap direction data.
			ComputeBuffer cubeMatrix = new ComputeBuffer(sgFaceInput.Length, Marshal.SizeOf(vec3));
			cubeMatrix.SetData(sgFaceInput);
			
			Vector4 vec4 = new Vector4();
			// Create a output buffer.
			ComputeBuffer dstData = new ComputeBuffer(cube_width * cube_width * 6, Marshal.SizeOf(vec4));
			
			ComputeShader CSEnvFilter;
			CSEnvFilter = (ComputeShader)AssetDatabase.LoadAssetAtPath("Assets/EnvironmentMapTool/ComputeShader/FilterCubeMap.compute", typeof(ComputeShader));
			// Set cubemap to shader.
			CSEnvFilter.SetTexture(0, "gCubemap", cubemap);
			// Set read write buffer for data output.
			CSEnvFilter.SetBuffer(0, "gOutput", dstData);
			// Set cubemap direction data.
			CSEnvFilter.SetBuffer(0, "sgFace2DMapping", cubeMatrix);
			
			Color[] outputData = new Color[cube_width * cube_width * 6];
			
			// How many mipmap level?
			float mipLevelNum = Mathf.Log(cube_width, 2);
			
			// Loop each mipmap level with different roughness.
			for (int i = 0; i < mipLevelNum + 1; i++)
			{
				// The texel number of a face.
				int image_size = cube_width * cube_width;
				// The texel number of a cubemap.
				int num_threads = image_size * 6;
				// Set roughness value (between 0~1).
				CSEnvFilter.SetFloat("gRoughness", (i / mipLevelNum));
				// The width of a mipmap level of a cube map.
				CSEnvFilter.SetInt("gWidth", cube_width);
				// The total number of thread groups (the number of my thread group : 64).
				num_threads = (int)Mathf.Ceil((float)num_threads / 64.0f);
				// Run compute shader.
				CSEnvFilter.Dispatch(0, num_threads, 1, 1);
				// Get data from the read & write buffer.
				dstData.GetData(outputData);
				// Copy data to the original cubemap.
				SetCubeMipMap(cubemap, outputData, image_size, i);
				// Half the size for the next mipmap level.
				cube_width = cube_width / 2;
			}
			
			// Set false to disable auto-generating mipmap.
			cubemap.Apply(false);
			// Use trilinear mode to interpolate different mipmap levels.
			cubemap.filterMode = FilterMode.Trilinear;
			cubemap.wrapMode = TextureWrapMode.Clamp;
			cubemap.name = cubemap.name + "(PreFilter)";
			
			// Release data.
			dstData.Release();
			cubeMatrix.Release();
		}
		
	}
	
	///<summary>
	/// Copy a mipmap level data to the array.
	///</summary>
	static void SetCubeMipMap(Cubemap cubemap, Color[] data, int imageSize, int mipLevel)
	{
		// Copy all texels of the cubemap to an array.
		Color[] temp = new Color[imageSize];
		System.Array.Copy(data, 0, temp, 0, imageSize);
		cubemap.SetPixels(temp, CubemapFace.PositiveX, mipLevel);
		
		System.Array.Copy(data, imageSize, temp, 0, imageSize);
		cubemap.SetPixels(temp, CubemapFace.NegativeX, mipLevel);
		
		System.Array.Copy(data, imageSize * 2, temp, 0, imageSize);
		cubemap.SetPixels(temp, CubemapFace.PositiveY, mipLevel);
		
		System.Array.Copy(data, imageSize * 3, temp, 0, imageSize);
		cubemap.SetPixels(temp, CubemapFace.NegativeY, mipLevel);
		
		System.Array.Copy(data, imageSize * 4, temp, 0, imageSize);
		cubemap.SetPixels(temp, CubemapFace.PositiveZ, mipLevel);
		
		System.Array.Copy(data, imageSize * 5, temp, 0, imageSize);
		cubemap.SetPixels(temp, CubemapFace.NegativeZ, mipLevel);
		
	}
	
}
