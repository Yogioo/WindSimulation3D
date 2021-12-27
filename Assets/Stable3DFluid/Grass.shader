Shader "Unlit/Grass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
        _affectDist ("AffectDist", float) = 0.5
        _Target ("TargetPos", float) = (0, 0, 0, 0)
        _Color ("Color", Color) = (0, 0.8, 0, 0)
        _MultiMove ("MultiMove", float) = 4
    }
    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
        LOD 100
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite off
            CGPROGRAM
            
            // Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma exclude_renderers gles
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            
            #include "UnityCG.cginc"
            #include "HLSL/WindShaderTools.hlsl"
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
                float4 color: COLOR;
            };
            
            struct v2f
            {
                float2 uv: TEXCOORD0;
                float4 vertex: SV_POSITION;
                float4 vertexColor: COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _affectDist;
            
            float4 _Color;
            float _MultiMove;
            float4 _Target;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                float3 worldPos = mul((float3x4)unity_ObjectToWorld, v.vertex);
                float distLimit = _affectDist;
                
                float3 vertex = mul(unity_ObjectToWorld, v.vertex);
                
                float3 _obstacle = UNITY_ACCESS_INSTANCED_PROP(Props, _Target);
                // 方向
                // float3 bendDir = normalize(float3(worldPos.x, 0, worldPos.z) - float3(_obstacle.x, 0, _obstacle.z));//direction of obstacle bend
                float3 bendDir = Sample3DWindLod(worldPos);
                // 距离
                float distMulti = 1;//(distLimit - min(distLimit, distance(float3(worldPos.x, 0, worldPos.z), float3(_obstacle.x, 0, _obstacle.z)))) / distLimit; //distance falloff
                
                // 草越高 那就移动的越多, 反之越少
                float hight = saturate(v.vertex.y/_affectDist);// sin(pow(3.14 * vertex.y, 0.2)) - 0.9;
                // Color.a是在模型上面控制了顶点颜色的透明度
                float2 moveXZ = bendDir.xz * distMulti * hight * _MultiMove;
                float moveY = length(moveXZ) * hight;
                vertex.xz += moveXZ;
                vertex.y -= moveY;
                o.vertex = UnityWorldToClipPos(vertex);
                o.vertexColor = hight;
                

                return o;
            }
            
            fixed4 frag(v2f i): SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a);
                return  col * _Color;
                //return col*_Color;
            }
            ENDCG
            
        }
        Pass
        {
            ZWrite On
            ColorMask 0
        }
    }
}
