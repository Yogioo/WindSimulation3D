Shader "Masaka/WindSimulation/WindSlideDebugger"
{
    Properties { 
        _Alpha("Alpha",Range(0,1)) = 0.1
        
        [Toggle]_DisplayGrayMap("Display Gray Map",Int) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass{
            ColorMask 0
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "HLSL/WindShaderTools.hlsl"
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };
            
            struct v2f
            {
                float3 worlduv: TEXCOORD0;
                float4 vertex: SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worlduv = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            float _Alpha;
            int _DisplayGrayMap;
            
            fixed4 frag(v2f i): SV_Target
            {
                // 计算与风场中间的距离, 基于距离采样, 然后基于贴图大小缩放
                // fixed4 col = tex3D(_WindTex, Wolrd2UV(i.worlduv));
                float4 col = float4(1, 1, 1, 1);
                if(_DisplayGrayMap){
                    col.xyz = abs(GetWindForce(i.worlduv.xyz)); 
                    col.xyz = max(max(col.x,col.y),col.z);
                }else{
                    col.xyz = GetWindForce(i.worlduv.xyz) + 0.5f; 
                }
                col.a = _Alpha;
                return col;
            }
            ENDCG
            
        }
    }
}
