Shader "CoralVivoVR/Video360"
{
    Properties
    {
        _MainTex ("Video Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }
        
        LOD 100
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Cull Front // Renderizar faces internas da esfera
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Alpha;
                float _Brightness;
                float _Contrast;
                float _Saturation;
            CBUFFER_END
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Posição do vértice
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                
                // UV coordinates
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                // Normal em world space
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Direção da view
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.viewDirWS = GetCameraPositionWS() - positionWS;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Sample da textura de vídeo
                float4 videoColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Aplicar cor de tint
                videoColor *= _Color;
                
                // Aplicar brilho
                videoColor.rgb *= _Brightness;
                
                // Aplicar contraste
                videoColor.rgb = (videoColor.rgb - 0.5) * _Contrast + 0.5;
                
                // Aplicar saturação
                float luminance = dot(videoColor.rgb, float3(0.299, 0.587, 0.114));
                videoColor.rgb = lerp(float3(luminance, luminance, luminance), videoColor.rgb, _Saturation);
                
                // Aplicar alpha
                videoColor.a *= _Alpha;
                
                return videoColor;
            }
            ENDHLSL
        }
        
        // Pass para sombras (opcional)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            
            Cull Front
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
    
    // Fallback para plataformas não suportadas
    Fallback "Universal Render Pipeline/Unlit"
}
