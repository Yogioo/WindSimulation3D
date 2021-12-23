#ifndef WIND_SHADER_TOOLS
    #define WIND_SHADER_TOOLS
    float3 _WindCenter;
    float3 _DivisionSize;
    sampler3D _VelocityMap;
    
    float3 Wolrd2UV(float3 worldPos)
    {
        return((worldPos - _WindCenter) / _DivisionSize + 1) / 2;
    }
    
    float3 Sample3DWind(float3 worldPos){
        return abs(tex3D(_VelocityMap, Wolrd2UV(worldPos)).xyz);
    }

    float3 GetWindForce(float3 worldPos)
    {
        float3 c= Sample3DWind(worldPos);
        return c;
    }
#endif