using UnityEngine;
using System.Collections;
using System.Collections.Generic;

///<summary>
/// The class is for previewing indirect lighting in play mode.
///</summary>
public class AlignInfoBase : MonoBehaviour
{

    public GameObject _selectedObject;
    public Camera _demoCamera;
    public Mesh _demoMesh;
    public RenderTexture _renderTexture;
    public Shader _alignShader;
    public Shader _previewShader;
    public float _offset;
    Material _alignMaterial;
    protected Material _demoMaterial;

    ///<summary>
    /// Create game objects for previewing.
    ///</summary>
    void GenerateRenderObjects()
    {
        GameObject obj = new GameObject();
        obj.transform.Translate(new Vector3(0, 0, -1.5f));
        _demoCamera = obj.AddComponent<Camera>();
        _demoCamera.aspect = 1;
        RenderTexture rt = new RenderTexture(256, 256, 24);
        _demoCamera.targetTexture = rt;
        _demoCamera.cullingMask = 1 << LayerMask.NameToLayer("demoLayer");
        _demoCamera.clearFlags = CameraClearFlags.Color;
        _renderTexture = rt;

    }

    ///<summary>
    /// Unity callback,
    /// 
	/// initialize game objects for previewing.
    ///</summary>
    void Start()
    {
        _alignMaterial = new Material(_alignShader);
        GenerateRenderObjects();

    }

    ///<summary>
    /// Unity callback,
    /// 
    /// render the preview object to a render texture.
    ///</summary>
    protected void Update()
    {
        if (_selectedObject != null && _previewShader != null)
        {
            GameObject obj = _selectedObject;
            if (obj.renderer != null)
            {
                Material newMat = new Material(obj.renderer.sharedMaterial);
                _demoMaterial = newMat;
                _demoMaterial.shader = _previewShader;
                SetMaterial(obj);
                Graphics.DrawMesh(_demoMesh, Matrix4x4.identity, _demoMaterial, LayerMask.NameToLayer("demoLayer"), _demoCamera);

            }
        }
    }

    ///<summary>
    /// Base function for setting a material.
    ///</summary>
    virtual protected void SetMaterial(GameObject selectedObj)
    {

		_demoMaterial.SetMatrix ("_OffsetMatrix",selectedObj.transform.localToWorldMatrix);
	
	
    }

    ///<summary>
    /// Unity callback.
    /// 
    /// To show a render texture on screen, we need to render a quad, and
	/// this quad is aligned with the corner of screen.
    ///</summary>
    void OnPostRender()
    {

		// Set the screen position for this quad.
        float min = -0.9f;
        float max = -0.6f;
        float ration = (float)(Screen.width / (float)Screen.height);
        float length = ration * (max - min);
        float maxy = min + length;


        GL.PushMatrix();
        _alignMaterial.SetPass(0);
        GL.LoadOrtho();
        _alignMaterial.SetTexture("_RenderTexture", _renderTexture);
        GL.Begin(GL.QUADS);

        GL.TexCoord2(0, 1); GL.Vertex3(min, min + _offset, 0);
        GL.TexCoord2(0, 0); GL.Vertex3(min, maxy + _offset, 0);
        GL.TexCoord2(1, 0); GL.Vertex3(max, maxy + _offset, 0);
        GL.TexCoord2(1, 1); GL.Vertex3(max, min + _offset, 0);
        GL.End();
        GL.PopMatrix();
    }

}
