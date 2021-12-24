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
    struct MotorBall{
        float3 posWS;
        float force;
        float sqRadius;
        float3 direction;
    };

    float _Time;
    
    float lengthSq(float3 Vec)
    {
        return Vec.x * Vec.x + Vec.y * Vec.y + Vec.z * Vec.z;
    }


    float Random3DTo1D(float3 value,float a,float3 b)
    {			
        float3 smallValue = sin(value);
        float  random = dot(smallValue,b);
        random = frac(sin(random) * a);
        return random;
    }

    float3 Random3DTo3D(float3 value){
	    return float3(
		Random3DTo1D(value,14375.5964, float3(15.637,76.243,37.168)),
		Random3DTo1D(value,14684.6034,float3(45.366, 23.168,65.918)),
		Random3DTo1D(value,17635.1739,float3(62.654, 88.467,25.111))
	);
}

    float noiseForce(in float3 cellPos){
        return Random3DTo3D(cellPos + _Time );
    }
    
    // 平行风，out返回float3的velocity
    void ApplyMotorDirectional(in float3 cellPosWS, uniform MotorDirectional motorDirectional, in out float3 velocityWS)
    {
        // 计算cell到motor的距离
        float distanceSq = lengthSq((cellPosWS - motorDirectional.posWS) + 0.0001f);
        // 距离的平方小于motor的作用范围，加上速度
        // force = direction * strength * deltaTime

        if (distanceSq < motorDirectional.sqRadius)
            velocityWS += motorDirectional.force * motorDirectional.direction * noiseForce(cellPosWS);

        // velocityWS = noiseForce(cellPosWS);
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

        // velocityWS = noiseForce(cellPosWS);
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

        // velocityWS = noiseForce(cellPosWS);
    }

    // 球风
    void ApplyMotorBall(in float3 cellPosWS, uniform MotorBall motorBall, in out float3 velocityWS){
        // 计算cell到motor的距离
        float3 dir =  (cellPosWS - motorBall.posWS) + 0.0001f;
        float distanceSq = lengthSq(dir);
        // 距离的平方小于motor的作用范围，加上速度
        // force = direction * strength * deltaTime
        if (distanceSq < motorBall.sqRadius)
            velocityWS += motorBall.force * (motorBall.direction + dir) * noiseForce(cellPosWS);

        // velocityWS = noiseForce(cellPosWS);
    }
    
#endif