// 风力驱动源
#ifndef MOTORS
    #define MOTORS
    
    struct MotorDirectional
    {
        float3 posWS;
        float force;
        float3 direction;
        float sqRadius;
    };
    struct MotorOmni
    {
        float3 posWS;
        float force;
        float radiusSq;
    };
    struct MotorVortex
    {
        float3 posWS;
        float force;
        float radiusSq;
        float3 axis;
    };
    
    float lengthSq(float3 Vec)
    {
        return Vec.x * Vec.x + Vec.y * Vec.y + Vec.z * Vec.z;
    }
    
    // 平行风，out返回float3的velocity
    void ApplyMotorDirectional(in float3 cellPosWS, uniform MotorDirectional motorDirectional, in out float3 velocityWS)
    {
        // 计算cell到motor的距离
        float distanceSq = lengthSq((cellPosWS - motorDirectional.posWS) + 0.0001f);
        // 距离的平方小于motor的作用范围，加上速度
        // force = direction * strength * deltaTime
        if (distanceSq < motorDirectional.sqRadius)
            velocityWS += motorDirectional.force * motorDirectional.direction;
    }
    
    // 全向风，作用朝四面八方，辐射出去，存在作用半径radius
    void ApplyMotorOmni(in float3 cellPosWS, uniform MotorOmni motorOmni, in out float3 velocityWS)
    {
        // force = strength * deltaTime
        float3 differenceWs = cellPosWS - motorOmni.posWS + 0.0001f;
        float distanceSq = lengthSq(differenceWs);
        float3 direction = normalize(differenceWs);
        // 速度受到作用半径和距离的影响
        if (distanceSq < motorOmni.radiusSq)
        {
            velocityWS += motorOmni.force * direction * (distanceSq/motorOmni.radiusSq);
        }
            // velocityWS += motorOmni.force * direction *(-rsqrt(distanceSq)-motorOmni.radiusSq) ;//* rsqrt(distanceSq);
    }
    
    // 螺旋风
    void ApplyMotorVortex(in float3 cellPosWS, uniform MotorVortex motorVortex, in out float3 velocityWS)
    {
        // force = strength * deltaTime
        float3 differenceWs = cellPosWS - motorVortex.posWS;
        float distanceSq = lengthSq(differenceWs + 0.0001f);
        // 速度受到作用半径和螺旋风轴向叉乘的影响
        if (distanceSq < motorVortex.radiusSq)
            velocityWS += motorVortex.force * cross(motorVortex.axis, rsqrt(distanceSq) * differenceWs);
    }
    
#endif