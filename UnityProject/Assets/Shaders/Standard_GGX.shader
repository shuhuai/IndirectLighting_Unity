Shader "Standard_GGX" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "black" {}
		_GlossTex("GlossMap (RGB)", 2D) = "white" {}
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
			#pragma surface surf  SimpleSpecular noambient vertex:vert
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
				half3 Emission;
				half Specular;
				half Alpha;
				half fog;
			};
			float G_Smith(float roughness, float NoV, float NoL)
			{
				float  k = (roughness + 1) * (roughness + 1) / 8;
				return  (NoV / (NoV * (1 - k) + k)) *  (NoL / (NoL * (1 - k) + k));
			}


			float3 getParallaxCorrectDir(float3 BBmax, float3 BBmin, float3 envPos, float3 R, float3 worldPos)
			{
				float3 intersectMaxPointPlanes = (BBmax - worldPos) / R;
				float3 intersectMinPointPlanes = (BBmin - worldPos) / R;
				// Look only for intersections in the forward direction of the ray.
				float3 largestRayParams = max(intersectMaxPointPlanes, intersectMinPointPlanes);
				// Smallest value of the ray parameters gives us the intersection.
				float distToIntersect = min(min(largestRayParams.x, largestRayParams.y), largestRayParams.z);
				// Find the position of the intersection point.
				float3 intersectPositionWS = worldPos + R * distToIntersect;
				// Get local corrected reflection vector.
				R = intersectPositionWS - envPos;


				return R;

			}
			void vert(inout appdata_full v, out Input o) {
				UNITY_INITIALIZE_OUTPUT(Input, o);
				float3 worldN = mul((float3x3)_Object2World, SCALED_NORMAL);
				o.shLight = saturate(ShadeSH9(float4(worldN, 1.0)));
				o.normalW = worldN;
				float pos = length(mul(UNITY_MATRIX_MV, v.vertex).xyz);
				o.tangentW.xyz = normalize(mul(_Object2World, float4(v.tangent.xyz, 0.0)).xyz).xyz;
			}


			half4 LightingSimpleSpecular(SurfaceOutputCustom s, half3 lightDir, half3 viewDir, half atten) {
				const  float pi = 3.14159;
				float3 h = normalize(viewDir + lightDir);


				float NdotL = max(0, dot(s.Normal, lightDir));
				float NdotH = max(0, dot(s.Normal, h));
				float LdotH = max(0, dot(lightDir, h));
				float VdotH = max(0, dot(viewDir, h));
				float NdotV = max(0, dot(s.Normal, viewDir));
				float roughness = s.Specular;

				// D
				float alpha = roughness *  roughness;
				float alphaSqr = alpha*alpha;
				float denom = ((NdotH * NdotH) * (alphaSqr - 1.0) + 1.0);
				float D = alphaSqr / (pi * denom* denom);
				float FV;

				// Fersnel & V.
				float F_b = pow((1 - VdotH), 5);
				float F_a = 1;
				float k = alpha / 2;
				float	vis = G_Smith(roughness, NdotV, NdotL);
				float2 FV_helper = float2(F_a*vis, F_b*vis);
				float3 col = s.SpecularColor*FV_helper.x + (1 - s.SpecularColor)*FV_helper.y;
				
				col = (NdotL*D*col + NdotL*s.Albedo)* _LightColor0.rgb*atten;

				return float4(col, 1);
			}






			void surf(Input IN, inout SurfaceOutputCustom o) {
	
				half4 c = tex2D(_MainTex, IN.uv_MainTex);

				o.Normal = UnpackNormal(tex2Dlod(_BumpMap, float4(IN.uv_MainTex, 0, 0)));
				o.Normal.z *= _NormalStr;
				o.Normal = normalize(o.Normal);
				half4 emissivecolor = tex2D(_EmissiveTex, IN.uv_MainTex);
				o.SpecularColor = saturate(tex2D(_SpecularMap, IN.uv_MainTex)*_Shininess);

				o.Specular = saturate(max(1 - tex2D(_GlossTex, IN.uv_MainTex)*_Roughness, 0.1f));

				o.Albedo = c;
				o.Alpha = 1.0;


				float3 binormalWorld = cross(IN.normalW, IN.tangentW.xyz);
				float3x3 local2WorldTranspose = float3x3(
					IN.tangentW.xyz,
					binormalWorld,
					IN.normalW);
				float3 worldTangentNormal = normalize(mul(o.Normal, local2WorldTranspose));
				float3 N = worldTangentNormal;
				float3 V = normalize(_WorldSpaceCameraPos - IN.worldPos);
				float3 R = 2 * dot(V, N) * N - V;
				float NoV = saturate(dot(N, V));
				float invGloss = o.Specular;

				float3 PrefilteredColor = texCUBElod(_EnvCube, float4(R, (invGloss) / _CubeMipmapStep)).rgb;
				float2 BRDF_Map = tex2Dlod(_EnvBRDF, float4(NoV, invGloss, 0, 0));
				float3 EnvBRDF = float3(o.SpecularColor * BRDF_Map.x + BRDF_Map.y);
				float3 EnvColor = PrefilteredColor * EnvBRDF;
				float3 localR = getParallaxCorrectDir(_BBoxMax, _BBoxMin, _EnviCubeMapPos, R, IN.worldPos);
				float3 localPrefilteredColor = texCUBElod(_LocalEnv, float4(localR, (invGloss) / _CubeMipmapStep)).rgb;
				float3 LocalEnvColor = localPrefilteredColor* EnvBRDF;
				float d1 = distance(IN.worldPos, _EnviCubeMapPos);
				float ration = pow(min((max((d1 - _innerRange) / (_outerRange - _innerRange), 0.0f)), 1),4);//2 is an ad hoc variable to adjust gradient;
				float3 indirectSpecular = (1 - ration)*LocalEnvColor + (ration)*EnvColor;
				o.Emission += indirectSpecular + emissivecolor + IN.shLight*c;
			}
			ENDCG
		}
		Fallback "Diffuse"
		CustomEditor "CharacterShaderInspector"
}
