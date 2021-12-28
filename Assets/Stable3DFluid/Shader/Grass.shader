Shader "Unlit/Grass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
        _affectDist ("AffectDist", float) = 0.5
        _Target ("TargetPos", float) = (0, 0, 0, 0)
        _Color ("Color", Color) = (0, 0.8, 0, 0)
        _MultiMove ("MultiMove", range(-500,500)) = 1
        _ClampRotate("Clamp Rotate",range(0,3.1415926)) = 1
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
            #include "./HLSL/WindShaderTools.hlsl"
            
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
            float _ClampRotate;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // 取得世界空间旋转中心
                float3 pivotWorldPos = mul(unity_ObjectToWorld ,float4(0,0,0,1));
                float4 worldPos = mul(unity_ObjectToWorld ,v.vertex);

                float3 velocity = Sample3DWindLod(worldPos);
                float velocityStrengthZY =  velocity.y*velocity.y + velocity.z*velocity.z;
                float velocityStrengthXY =  velocity.y*velocity.y + velocity.x*velocity.x;
                velocityStrengthZY = min(velocityStrengthZY,1.0f);
                velocityStrengthXY = min(velocityStrengthXY,1.0f);
                // // 世界空间绕模型原点旋转
                float3 localPos = worldPos.xyz - pivotWorldPos;
                // Rotate By X
                float3 tmpPos = worldPos;
                float3 tmpVel = velocity;
                tmpPos.x = 0;
                tmpVel.x = 0;
                tmpPos = normalize(tmpPos);
                tmpVel = normalize(tmpVel);
                float theta = acos(dot(tmpPos,tmpVel)) * 360.0f / (3.1415926535f * 2.0f);
                float u = cross(tmpPos,tmpVel).x; // 叉乘判断正负 左手螺旋定则 叉乘结果为正时 逆时针旋转 也就是负数旋转度数
                if(u < 0){
                    theta *= -1;
                }
                theta *= _MultiMove * velocityStrengthZY;///180.0f * 3.1415926535f;
                theta = min(theta,_ClampRotate);
                theta = max(theta,-_ClampRotate);

                matrix rotateX;
                float cosTheta = cos(theta);
                float sinTheta = sin(theta);
                rotateX[0] = float4(1,0,0,0);
                rotateX[1] = float4(0,cosTheta,-sinTheta,0);
                rotateX[2] = float4(0,sinTheta,cosTheta,0);
                rotateX[3] = float4(0,0,0,1);
                localPos = mul(rotateX,localPos);

                // Rotate By Z
                tmpPos = worldPos;
                tmpVel = velocity;
                tmpPos.z = 0;
                tmpVel.z = 0;
                tmpPos = normalize(tmpPos);
                tmpVel = normalize(tmpVel);
                theta = acos(dot(tmpPos,tmpVel)) * 360.0f / (3.1415926535f * 2.0f);
                u = cross(tmpPos,tmpVel).z; // 叉乘判断正负 左手螺旋定则 叉乘结果为正时 逆时针旋转 也就是负数旋转度数
                if(u < 0){
                    theta *= -1;
                }
                theta *= _MultiMove * velocityStrengthXY;///180.0f * 3.1415926535f;
                theta = min(theta,_ClampRotate);
                theta = max(theta,-_ClampRotate);
                matrix rotateZ;
                cosTheta = cos(theta);
                sinTheta = sin(theta);
                rotateZ[0] = float4(cosTheta,-sinTheta,0,0);
                rotateZ[1] = float4(sinTheta,cosTheta,0,0);
                rotateZ[2] = float4(0,0,1,0);
                rotateZ[3] = float4(0,0,0,1);
                localPos = mul(rotateZ,localPos);


                worldPos = float4(localPos + pivotWorldPos,1.0f);

                o.vertex = mul(UNITY_MATRIX_VP,worldPos);

                // float3 worldPos = mul((float3x4)unity_ObjectToWorld, v.vertex);
                // float distLimit = _affectDist;
                
                // float3 vertex = mul(unity_ObjectToWorld, v.vertex);
                
                // float3 _obstacle = UNITY_ACCESS_INSTANCED_PROP(Props, _Target);
                // // 方向
                // // float3 bendDir = normalize(float3(worldPos.x, 0, worldPos.z) - float3(_obstacle.x, 0, _obstacle.z));//direction of obstacle bend
                // float3 bendDir = Sample3DWindLod(worldPos);
                
                // // 草越高 那就移动的越多, 反之越少
                // float hight = saturate(v.vertex.y/_affectDist);// sin(pow(3.14 * vertex.y, 0.2)) - 0.9;
                // // Color.a是在模型上面控制了顶点颜色的透明度
                // float2 moveXZ = bendDir.xz * _MultiMove * hight;
                // moveXZ = min(moveXZ,float2(.5,.5));
                // float moveY = length(moveXZ);
                // vertex.xz += moveXZ;
                // vertex.y -= moveY;
                // o.vertex = UnityWorldToClipPos(vertex);
                // o.vertexColor = hight;

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
