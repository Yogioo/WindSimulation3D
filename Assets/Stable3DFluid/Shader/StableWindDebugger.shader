// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/StableWindDebugger"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 velocity: TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float3 _WindCenter;
            float3 _DivisionSize;
            sampler3D _VelocityMap;

             float3 Wolrd2UV(float3 worldPos)
            {
                return((worldPos - _WindCenter) / _DivisionSize + 1) / 2;
            }
    
            float3 Sample3DWindLod(float3 worldPos,float lod){
                return tex3Dlod(_VelocityMap, float4(Wolrd2UV(worldPos),lod)).xyz;
            }

            v2f vert (appdata v)
            {
                v2f o;
                // float4 pos  = UnityObjectToClipPos(v.vertex);//UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // 采样速度贴图
                float3 centerWorldPos = mul(unity_ObjectToWorld,float3(0,0,0));
                float3 velocity = Sample3DWindLod(centerWorldPos,0);
                //把速度转换到Object空间
                velocity = mul(unity_WorldToObject,velocity);
                // 已知速度 求当前旋转矩阵 使得当前0,0,1 转向速度方向
                float velocityLength = length(velocity);
                float3 normalizeVeclocity = normalize(velocity);
                o.velocity = velocity;

                float4x4 m = UNITY_MATRIX_MVP;
                float4x4 identity = m;
                float angleX, angleY, angleZ;
                matrix rotateX,rotateY,rotateZ;
                float xTheta,yTheta,zTheta;

                float3 xV = normalizeVeclocity;
                xV.x = 0;
                float3 yV = normalizeVeclocity;
                yV.y = 0;
                // float3 zV = normalizeVeclocity;
                // zV.z = 0;

                float pi = 3.1415926535f;
                
                angleX = acos(dot(xV,float3(0,0,1)))/pi * 180.0f;
                if(xV.y < 0 ){ //逆时针
                    angleX *= -1;
                }
                angleY = acos(dot(yV,float3(0,0,1)))/pi * 180.0f;
                if(angleY.x > 0 ){ //逆时针
                    angleY *= -1;
                }
                angleZ = 0;//acos(dot(zV,float3(0,0,0)))/pi * 180.0f;

                xTheta=angleX / 180.0f  * pi;
                yTheta=angleY / 180.0f * pi;
                zTheta=angleZ / 180.0f * pi;

                rotateX[0] = float4(1,0,0,0);
                rotateX[1] = float4(0,cos(xTheta),-sin(xTheta),0);
                rotateX[2] = float4(0,sin(xTheta),cos(xTheta),0);
                rotateX[3] = float4(0,0,0,1);

                rotateY[0] = float4(cos(yTheta),0,sin(yTheta),0);
                rotateY[1] = float4(0,1,0,0);
                rotateY[2] = float4(-sin(yTheta),0,cos(yTheta),0);
                rotateY[3] = float4(0,0,0,1);

                rotateZ[0] = float4(cos(zTheta),-sin(zTheta),0,0);
                rotateZ[1] = float4(sin(zTheta),cos(zTheta),0,0);
                rotateZ[2] = float4(0,0,1,0);
                rotateZ[3] = float4(0,0,0,1);

                matrix rotate = mul(rotateX , rotateY);
                rotate = mul(rotateZ,rotate);
                // v.vertex = mul(rotate,v.vertex);
                o.vertex =  UnityObjectToClipPos(mul(rotate,v.vertex));
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                UNITY_APPLY_FOG(i.fogCoord, col);

                // return float4(i.velocity,1.0f);

                return col;
            }
            ENDCG
        }
    }
}
