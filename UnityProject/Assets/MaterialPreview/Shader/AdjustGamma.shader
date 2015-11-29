Shader "Custom/AdjustGamma" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}
		SubShader{
		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma glsl
			#include "UnityCG.cginc"
			
			float4 _MainTex_ST;
			sampler2D _MainTex;

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float3 wvp : TEXCOORD1;
			};

			v2f vert(appdata_full v)
			{
				v2f o = (v2f)0;

				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord = v.texcoord;
				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				half4 col = tex2D(_MainTex, i.texcoord);
				return pow(col, 0.45f);
			}
				ENDCG
		}

	}
}
