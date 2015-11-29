using UnityEngine;
using UnityEditor;
using System.Collections;

///<summary>
/// The base class for previewing indirect lighting.
///</summary>
public class LightingPreview : EditorWindow
{

    Camera _previewCamera;   //Camera for rendering a preview object .
    RenderTexture _renderTexture;       //Texture to save the result.
    GameObject _previewObj;
    protected Shader _previewShader;  //Shader for rendering the preview object .
    protected Mesh _previewMesh = null;  //Mesh as the preview object.
    protected Material _demoMaterial;

    /// <summary>
    /// Unity callback.
    /// 
    /// This function runs, if the window is open.
    /// </summary>
    public void Awake()
    {
        // Initialize all objects and parameters for rendering a preview object.
        _renderTexture = new RenderTexture((int)position.width, (int)position.height, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        _previewObj = new GameObject("Preview Camera");
        _previewObj.transform.Translate(new Vector3(0, 0, -2.5f));
        _previewObj.hideFlags = HideFlags.DontSave;
        _previewCamera = _previewObj.AddComponent<Camera>();
        // When we use Linear color space in Unity, the custom editor window still is in gamma color space.
        // Therefore, I use a post-effect component to adjust color.
        if (PlayerSettings.colorSpace == ColorSpace.Linear)
        {
            _previewObj.AddComponent<AdjustGamma>();
        }
        _previewCamera.cullingMask = 1 << LayerMask.NameToLayer("demoLayer");
        _previewCamera.targetTexture = _renderTexture;
        _previewCamera.clearFlags = CameraClearFlags.Color;
        _previewCamera.enabled = false;
        // Depending on the preview shader, we should set different parameters.
		// The implementation of this function is in derived classes.
        SetDefaultParameters();

    }

    /// <summary>
    /// A virtual function. It should be implemented in derived classes.
    /// </summary>
    virtual protected void SetDefaultParameters()
    {

    }

    /// <summary>
    /// Unity callback.
    /// 
    /// This function updates parameters for different materials, and it also adjusts the size of render texture.
    /// </summary>
    public void Update()
    {
        if (Selection.gameObjects.Length > 0 && _previewShader != null)
        {
            GameObject obj = Selection.gameObjects[0];
            if (obj.renderer != null)
            {
                Material newMat = new Material(obj.renderer.sharedMaterial);
                _demoMaterial = newMat;
                _demoMaterial.shader = _previewShader;
                SetMaterial(obj);
                Graphics.DrawMesh(_previewMesh, Matrix4x4.identity, _demoMaterial, LayerMask.NameToLayer("demoLayer"), _previewCamera);
            }
        }
        if (_previewCamera != null)
        {
            _previewCamera.targetTexture = _renderTexture;
            _previewCamera.Render();
            _previewCamera.targetTexture = null;
        }
        if (_renderTexture != null)
        {
            if (_renderTexture.width != position.width ||
               _renderTexture.height != position.height)
                _renderTexture = new RenderTexture((int)position.width, (int)position.height, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        }
        else
        {
            _renderTexture = new RenderTexture((int)position.width, (int)position.height, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        }


    }
    /// <summary>
    /// A virtual function. It should be implemented in derived classes.
    /// </summary>
    virtual protected void SetMaterial(GameObject selectedObj)
    {
    }

    /// <summary>
    /// Unity callback.
    /// 
    /// This function destroys the preview object.
    /// </summary>
    void OnDestroy()
    {
        _renderTexture = null;
        DestroyImmediate(_previewObj);
    }

    /// <summary>
    /// Unity callback,
    /// 
    /// show the render texture in this editor window.
    /// </summary>
    void OnGUI()
    {
        _previewMesh = (Mesh)EditorGUI.ObjectField(new Rect(3, 3, 300, 20), "Preview Material", _previewMesh, typeof(Mesh), true);
        _previewShader = (Shader)EditorGUI.ObjectField(new Rect(3, 30, 300, 20), "Preview Shader", _previewShader, typeof(Shader), true);
        GUI.DrawTexture(new Rect(0.0f, 0.0f, position.width, position.height), _renderTexture);
    }
}
