Shader "Custom/URP/TranslucentLight"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 1
        _CoreGlowIntensity ("Core Glow Intensity", Range(0, 10)) = 2
        _CoreGlowRadius ("Core Glow Radius", Range(0.1, 5)) = 1
        _Transparency ("Transparency", Range(0, 1)) = 0.5
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3
        _FresnelIntensity ("Fresnel Intensity", Range(0, 5)) = 1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _EmissionIntensity;
                half _CoreGlowIntensity;
                half _CoreGlowRadius;
                half _Transparency;
                half _FresnelPower;
                half _FresnelIntensity;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                output.positionOS = input.positionOS.xyz;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Normalize vectors
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);
                
                // Calculate distance from center (core glow)
                half distFromCenter = length(input.positionOS);
                half coreGlow = saturate(1.0 - (distFromCenter / _CoreGlowRadius));
                coreGlow = pow(coreGlow, 2.0) * _CoreGlowIntensity;
                
                // Fresnel effect for edge lighting
                half fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                fresnel *= _FresnelIntensity;
                
                // Combine effects
                half3 emission = _EmissionColor.rgb * _EmissionIntensity;
                emission += _EmissionColor.rgb * coreGlow;
                emission += _BaseColor.rgb * fresnel;
                
                // Final color
                half3 finalColor = _BaseColor.rgb + emission;
                half alpha = _Transparency + fresnel * 0.3; // Edges more visible
                
                return half4(finalColor, saturate(alpha));
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
