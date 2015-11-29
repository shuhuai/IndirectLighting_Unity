Shader "Preview/RefMapPreviewer" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_GlossTex("GlossMap (RGB)", 2D) = "white" {} //r for gloss, g for refl}
		_EmissiveTex("Emissive", 2D) = "black" {}
		_BumpMap("Bumpmap", 2D) = "bump" {}
		_SpecularMap("Specular", 2D) = "white" {}
		_NormalStr("Normal Strenth", Range(0.1, 10)) = 1
		_Shininess("Specular magnitude", Float) = 1.1
		_Roughness("Gloss magnitude", Float) = 0.4
		_EnviCubeMapPos("Enviorment Pos", Vector) = (0, 0, 0, 0)
		_BBoxMax("BBoxMax", Vector) = (0, 0, 0, 0)
		_BBoxMin("BBoxMin", Vector) = (0, 0, 0, 0)
		_innerRange("Inner1", float) = 1
		_outerRange("Outer1", float) = 1
		_LocalEnv("Local Environment", Cube) = ""
	}

	SubShader{

			CGPROGRAM
			#pragma surface surf Lambert noambient vertex:vert
			#pragma glsl
			#pragma target 3.0
			half _Shininess;
			half _Roughness;
			half  _NormalStr;

			half _CubeMipmapStep;
			sampler2D _EnvBRDF;
			samplerCUBE _EnvCube;
			samplerCUBE _LocalEnv;


			float3 _EnviCubeMapPos;
			float3 _BBoxMax;
			float3 _BBoxMin;
			float _innerRange;
			float _outerRange;

			sampler2D _MainTex;
			sampler2D _GlossTex;
			sampler2D _BumpMap;
			sampler2D _EmissiveTex;
			sampler2D _SpecularMap;
			
			float4x4 _OffsetMatrix;

			struct Input {
				float2 uv_MainTex;
				float3 worldPos;
				float3 normalW;
				half3 shLight;
				half4 tangentW;


			};
			struct SurfaceOutputCustom {

				half3 Albedo;
				half3 Normal;
				half3 SpecularColor;
				half3 shLight;
				half3 Emission;
				half Specular;
				half Alpha;
				half fog;
			};



			float3 getParallaxCorrectDir(float3 BBmax, float3 BBmin, float3 envPos, float3 R, float3 worldPos)
			{
				float3 intersectMaxPointPlanes = (BBmax - worldPos) / R;
				float3 intersectMinPointPlanes = (BBmin - worldPos) / R;
				float3 largestRayParams = max(intersectMaxPointPlanes, intersectMinPointPlanes);
				float distToIntersect = min(min(largestRayParams.x, largestRayParams.y), largestRayParams.z);
				float3 intersectPositionWS = worldPos + R * distToIntersect;
				R = intersectPositionWS - envPos;
				return R;

			}
			void vert(inout appdata_full v, out Input o) {

				// Evaluate SH light.
				UNITY_INITIALIZE_OUTPUT(Input, o);
				float3 worldN = mul((float3x3)_Object2World, SCALED_NORMAL);

				o.shLight = saturate(ShadeSH9(float4(worldN, 1.0)));
				o.normalW = worldN;

				float pos = length(mul(UNITY_MATRIX_MV, v.vertex).xyz);

				o.tangentW.xyz = normalize(mul(_Object2World, float4(v.tangent.xyz, 0.0)).xyz).xyz;
			}

			void surf(Input IN, inout SurfaceOutput o) {
				
				// Direct lighting.
				half4 c = tex2D(_MainTex, IN.uv_MainTex);
				o.Normal = UnpackNormal(tex2Dlod(_BumpMap, float4(IN.uv_MainTex, 0, 0)));
				o.Normal.z *= _NormalStr;
				o.Normal = normalize(o.Normal);
				half4 emissivecolor = tex2D(_EmissiveTex, IN.uv_MainTex);
				float3 SpecularColor = saturate(tex2D(_SpecularMap, IN.uv_MainTex)*_Shininess);
				float Specular = saturate(max(1 - tex2D(_GlossTex, IN.uv_MainTex)*_Roughness, 0.1f));
				// Clear to 0 for previewing.
				o.Albedo = 0;
				o.Alpha = 1.0;

				// Indirect specular lighting.
				float3 binormalWorld = cross(IN.normalW, IN.tangentW.xyz);
				float3x3 local2WorldTranspose = float3x3(
					IN.tangentW.xyz,
					binormalWorld,
					IN.normalW);
				float3 worldTangentNormal = normalize(mul(o.Normal, local2WorldTranspose));
				IN.worldPos=mul(_OffsetMatrix,float4(IN.worldPos,1));
				float3 N = worldTangentNormal;
				float3 V = normalize(float3(0,0,-1));
				float3 R = 2 * dot(V, N) * N - V;
				float NoV = saturate(dot(N, V));
				float invGloss = Specular;
				float3 PrefilteredColor = texCUBElod(_EnvCube, float4(R, (invGloss) / _CubeMipmapStep)).rgb;
				
				float2 BRDF_Map = tex2Dlod(_EnvBRDF, float4(NoV, invGloss, 0, 0));
				float3 EnvBRDF = float3(SpecularColor * BRDF_Map.x + BRDF_Map.y);
				float3 EnvColor = PrefilteredColor * EnvBRDF;
				float3 localR = R;
				float3 localPrefilteredColor = texCUBElod(_LocalEnv, float4(localR, (invGloss) / _CubeMipmapStep)).rgb;
				float3 LocalEnvColor = localPrefilteredColor* EnvBRDF;
				float d1 = distance(IN.worldPos, _EnviCubeMapPos);
				float ration = min((max((d1 - _innerRange) / (_outerRange - _innerRange), 0.0f)), 1);
				float3 indirectSpecular = (1 - ration)*LocalEnvColor + (ration)*EnvColor;
				o.Emission += indirectSpecular + emissivecolor + IN.shLight*c;
				

			}
			ENDCG
		}
		Fallback "Diffuse"
		CustomEditor "CharacterShaderInspector"
}
