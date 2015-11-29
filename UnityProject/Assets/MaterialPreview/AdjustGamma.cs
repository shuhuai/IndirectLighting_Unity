using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class AdjustGamma : MonoBehaviour {

	Material _mat;
	void Awake () {
		_mat=new Material(Shader.Find("Custom/AdjustGamma"));
	}

	void OnRenderImage(RenderTexture source,RenderTexture destination)
	{
		if(_mat==null)
		{
			_mat=new Material(Shader.Find("Custom/AdjustGamma"));
		}
		Graphics.Blit (source, destination,_mat);
	}
}
