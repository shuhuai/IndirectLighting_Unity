Shader "Custom/AlignTexture" {
	Properties{
		_RenderTexture("Height Texture", 2D) = "black" {}
		_Ration("Ration", float) = 1
	}
	SubShader{
			Pass{

				Blend SrcAlpha OneMinusSrcAlpha
				Cull off
				ZWrite off
				ztest off

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0
				#pragma glsl
				#include "UnityCG.cginc"

				float4 _MainTex_ST;
				float _Ration;
				sampler2D _RenderTexture;

				struct v2f {
					float4 vertex : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					float3 wvp : TEXCOORD1;
				};

				v2f vert(appdata_full v)
				{
					v2f o = (v2f)0;
					o.vertex = float4(v.vertex.xyz, 1);
					o.texcoord = v.texcoord;
					return o;
				}

				half4 frag(v2f i) : COLOR
				{
					half4 col = tex2D(_RenderTexture, i.texcoord);
					return col;
				}
					ENDCG
			}

		}
}
