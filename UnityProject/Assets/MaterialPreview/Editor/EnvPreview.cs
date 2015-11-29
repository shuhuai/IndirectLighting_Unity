using UnityEngine;
using UnityEditor;
using System.Collections;

///<summary>
/// The derived class for previewing indirect specular lighting in editor mode.
///</summary> 
public class EnvPreview : LightingPreview
{

    /// <summary>
    /// Unity callback.
    /// 
    /// This function provides a menu item for opening a window.
    /// </summary>
    [MenuItem("Preview/Environment Lighting")]
    static void Init()
    {
        EditorWindow editorWindow = GetWindow(typeof(EnvPreview));
        editorWindow.autoRepaintOnSceneChange = true;
        editorWindow.Show();
    }

    /// <summary>
	/// Set a shader and a mesh for previewing indirect specular lighting.
    /// </summary>
    protected override void SetDefaultParameters()
    {
        _previewShader = (Shader)Resources.LoadAssetAtPath("Assets/MaterialPreview/Shader/RefMapPreviewer.shader", typeof(Shader));
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
        _previewMesh = mesh;
        DestroyImmediate(sphere);


    }

	protected override void SetMaterial(GameObject selectedObj)
	{
		_demoMaterial.SetMatrix ("_OffsetMatrix",selectedObj.transform.localToWorldMatrix);
	}
}
