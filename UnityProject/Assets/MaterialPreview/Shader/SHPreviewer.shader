Shader "Preview/SHPreviewer" {
	Properties{
		_TintColor("Tint Color", Color) = (0.0, 0.0, 0.0, 0.0)
		_MainTex("Base (RGB)", 2D) = "white" {}
		_GlossTex("GlossMap (RGB)", 2D) = "white" {} //r for gloss, g for refl}
		_EmissiveTex("Emissive", 2D) = "black" {}
		_BumpMap("Bumpmap", 2D) = "bump" {}
		_SpecularMap("Specular", 2D) = "white" {}
		_NormalStr("Normal Strenth", Range(0.1, 10)) = 1
		_RimColor("Rim Color", Color) = (0, 0, 0, 0.0)
		_RimPower("Rim Power", Float) = 0.0
		_ShininessColor("Specular Color", Color) = (1.0, 1.0, 1.0, 0.0)
		_Shininess1("Specular1(Specular magnitude)", Float) = 1.1
		_Shininess2("Specular2(Specular contrast)", Range(0.1, 5)) = 2.5
		_Roughness1("Roughness1(Gloss magnitude)", Float) = 0.4
		_Roughness2("Roughness2(Gloss contrast)", Range(0.1, 5)) = 0.9
		_AdjustDiffuseMap("Enhance Light", Float) = 1
		_GradientSmooth("Smooth Gradient", Range(0.0, 0.6)) = 0
		_AmbiantColor1("AmbiantColor1(Dark color)", Color) = (0.0, 0.0, 0.0, 0.0)
		_AmbiantColor2("AmbiantColor2(Bright color)", Color) = (1.0, 1.0, 1.0, 0.0)
	}

	SubShader{


			CGPROGRAM
			#pragma surface surf Lambert noambient
			#pragma target 3.0

			// SH coefficients.
			half4 cAr;
			half4 cBr;
			half4 cAg;
			half4 cBg;
			half4 cAb;
			half4 cBb;
			half4 cC;

			float3 ShadeIrad(float4 vNormal)
			{
				float3 x1, x2, x3;
				// Linear + constant polynomial terms.
				x1.r = dot(cAr, vNormal);
				x1.g = dot(cAg, vNormal);
				x1.b = dot(cAb, vNormal);
				// 4 of the quadratic polynomials.
				float4 vB = vNormal.xyzz * vNormal.yzzx;
				x2.r = dot(cBr, vB);
				x2.g = dot(cBg, vB);
				x2.b = dot(cBb, vB);
				// Final quadratic polynomial.
				float vC = vNormal.x*vNormal.x - vNormal.y*vNormal.y;
				x3 = cC.rgb * vC;
				float3 all = x1 + x2 + x3;
				return all;
			}

			sampler2D _MainTex;


			struct Input {
				float2 uv_MainTex;
				float3 worldPos;
				float3 worldNormal;
			};

			void surf(Input IN, inout SurfaceOutput o) {
				half4 c = tex2D(_MainTex, IN.uv_MainTex);
				o.Alpha = 1;
				float3 sh = ShadeIrad(float4(IN.worldNormal, 1));
				o.Emission = sh*c;


			}
			ENDCG
		}
		Fallback "Diffuse"
		CustomEditor "CharacterShaderInspector"
}
