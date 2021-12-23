#ifndef WIND_COMPUTE_TOOLS
    #define WIND_COMPUTE_TOOLS
    
    float3 Index2UV(uint3 iduv)
    {
        return iduv.xyz / (float3(32.0f, 16.0f, 32.0f) - 1.0f);
    }
    
    uint3 World2Index(float3 worldPos)
    {
        return floor(worldPos * (float3(32.0f, 16.0f, 32.0f) - 1.0f));
    }
    
    
    bool IsBorder(float3 index)
    {
        if (index.x <= 0 || index.x >= 31 || index.y <= 0 || index.y >= 15 || index.z <= 0 || index.z >= 31)
        {
            return true;
        }
        return false;
    }
    
    float3 GetVelocity(RWTexture3D < float4 > tex, uint3 index)
    {
        return tex[index].xyz * 2 - 1;
    }
    
    void SetVelocity(RWTexture3D < float4 > tex, uint3 index, float3 velocity)
    {
        tex[index] = float4(velocity / 2 + 0.5f, 1.0f);
    }
    
    // 多线性插值图片 基于某个UV
    half4 Bilerp(RWTexture3D < float4 > tex, float3 p)
    {
        float3 st0, st1;
        st0 = floor(p);
        st1 = st0 + 1.0f;
        half3 z = p - st0;
        
        half4 a = tex[st0]; // 0,0,0
        half4 b = tex[float3(st1.x, st0.yz)]; // 1,0,0
        half4 c = tex[float3(st0.xy, st1.z)]; // 0,0,1
        half4 d = tex[float3(st1.x, st0.y, st1.z)]; // 1,0,1
        half4 panelA = lerp(lerp(a, b, z.x), lerp(c, d, z.x), z.z);
        
        half4 e = tex[float3(st0.x, st1.y, st0.z)]; // 0,1,0
        half4 f = tex[float3(st0.x, st1.y, st0.z)]; // 1,1,0
        half4 g = tex[float3(st0.x, st1.y, st0.z)]; // 0,1,1
        half4 h = tex[float3(st0.x, st1.y, st0.z)]; // 1,1,1
        half4 panelB = lerp(lerp(e, f, z.x), lerp(g, h, z.x), z.z);
        
        return lerp(panelA, panelB, z.y);
    }
    
    void SpinCompareExchange(uniform RWTexture3D < uint > rwTex, in uint3 coord, in float value)
    {
        uint curVal = 0;
        for (; ; )
        {
            uint oldVal;
            float newVal = asfloat(curVal) + value;
            InterlockedCompareExchange(rwTex[coord], curVal, asuint(newVal), oldVal);
            if (curVal == oldVal)
                break;
            curVal = oldVal;
        }
    }
    
#endif