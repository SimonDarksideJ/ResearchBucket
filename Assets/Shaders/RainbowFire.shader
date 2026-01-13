Shader "URP/Particles/RainbowFire"
{
    Properties
    {
        [Header(Particle Texture)]
        _MainTex ("Particle Texture", 2D) = "white" {}
        
        [Header(Color Settings)]
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _ColorOffset ("Color Cycle Offset", Range(0, 1)) = 0
        _ColorIntensity ("Color Intensity", Range(0, 5)) = 1.5
        
        [Header(Fire Properties)]
        _EmissionStrength ("Emission Strength", Range(0, 10)) = 2
        _Softness ("Soft Particles Factor", Range(0.01, 3)) = 1
        _DistortionStrength ("Distortion Strength", Range(0, 1)) = 0.1
        _DistortionSpeed ("Distortion Speed", Range(0, 5)) = 1
        
        [Header(Heat Shimmer)]
        _ShimmerIntensity ("Shimmer Intensity", Range(0, 1)) = 0.5
        _ShimmerScale ("Shimmer Scale", Range(0.1, 10)) = 2
        _ShimmerSpeed ("Shimmer Speed", Range(0, 5)) = 1
        
        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1 // One (additive)
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4 // LEqual
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }
        
        LOD 100
        
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        Cull Off
        Lighting Off
        
        Pass
        {
            Name "RainbowFirePass"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma target 3.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 projPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _TintColor;
                half _ColorOffset;
                half _ColorIntensity;
                half _EmissionStrength;
                half _Softness;
                half _DistortionStrength;
                half _DistortionSpeed;
                half _ShimmerIntensity;
                half _ShimmerScale;
                half _ShimmerSpeed;
            CBUFFER_END
            
            // Rainbow color function
            half3 RainbowColor(half t)
            {
                // Create smooth rainbow transition
                t = frac(t); // Ensure 0-1 range
                
                half3 color;
                half segment = t * 6.0;
                
                // Red to Orange
                if (segment < 1.0)
                {
                    color = lerp(half3(1, 0, 0), half3(1, 0.5, 0), segment);
                }
                // Orange to Yellow
                else if (segment < 2.0)
                {
                    color = lerp(half3(1, 0.5, 0), half3(1, 1, 0), segment - 1.0);
                }
                // Yellow to Green
                else if (segment < 3.0)
                {
                    color = lerp(half3(1, 1, 0), half3(0, 1, 0), segment - 2.0);
                }
                // Green to Blue
                else if (segment < 4.0)
                {
                    color = lerp(half3(0, 1, 0), half3(0, 0, 1), segment - 3.0);
                }
                // Blue to Indigo
                else if (segment < 5.0)
                {
                    color = lerp(half3(0, 0, 1), half3(0.5, 0, 1), segment - 4.0);
                }
                // Indigo to Violet
                else
                {
                    color = lerp(half3(0.5, 0, 1), half3(1, 0, 1), segment - 5.0);
                }
                
                return color;
            }
            
            // Noise function for distortion
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Fractal noise for organic movement
            float fractalNoise(float2 uv, float time)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * smoothNoise(uv * frequency + time);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                output.positionHCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
                output.projPos = vertexInput.positionNDC;
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Time-based animation
                float time = _Time.y * _DistortionSpeed;
                
                // Apply distortion to UV coordinates for fire movement
                float2 distortion = float2(
                    fractalNoise(input.uv * 2.0 + float2(0, time * 0.5), time),
                    fractalNoise(input.uv * 2.0 + float2(time * 0.3, 0), time * 1.2)
                );
                float2 distortedUV = input.uv + (distortion - 0.5) * _DistortionStrength;
                
                // Sample main texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);
                
                // Calculate rainbow color based on particle color and offset
                half colorTime = input.color.r + _ColorOffset; // Use red channel as gradient position
                half3 rainbowColor = RainbowColor(colorTime);
                
                // Enhance color intensity
                rainbowColor = pow(rainbowColor, half3(1.0 / _ColorIntensity, 1.0 / _ColorIntensity, 1.0 / _ColorIntensity));
                
                // Apply tint and particle color
                half3 finalColor = texColor.rgb * rainbowColor * _TintColor.rgb * input.color.rgb;
                
                // Calculate alpha with soft particles
                half alpha = texColor.a * input.color.a * _TintColor.a;
                
                // Soft particles using depth buffer
                #if defined(_SOFTPARTICLES_ON)
                    float sceneZ = LinearEyeDepth(SampleSceneDepth(input.projPos.xy / input.projPos.w), _ZBufferParams);
                    float particleZ = input.projPos.z;
                    float fade = saturate(_Softness * (sceneZ - particleZ));
                    alpha *= fade;
                #endif
                
                // Add heat shimmer effect
                float shimmerNoise = fractalNoise(input.worldPos.xz * _ShimmerScale, _Time.y * _ShimmerSpeed);
                half3 shimmerEffect = half3(shimmerNoise, shimmerNoise, shimmerNoise) * _ShimmerIntensity * 0.2;
                finalColor += shimmerEffect;
                
                // Apply emission
                finalColor *= _EmissionStrength;
                
                // Output final color
                half4 finalOutput = half4(finalColor, alpha);
                
                // Apply fog
                finalOutput.rgb = MixFog(finalOutput.rgb, input.fogFactor);
                
                return finalOutput;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Particles/Unlit"
}
