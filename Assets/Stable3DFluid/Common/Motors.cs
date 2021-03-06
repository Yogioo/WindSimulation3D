using UnityEngine;

namespace Wind.Common
{
    [System.Serializable]
    public struct MotorDirectional
    {
        public Vector3 posWS;
        public float force;
        public Vector3 direction;
        public float sqRadius;

        public void Reset()
        {
            posWS = Vector3.zero;
            force = 0;
            sqRadius = 0;
            direction = Vector3.forward;
        }
    }; 
    
    [System.Serializable]
    public struct MotorCylinder
    {
        public Vector3 posWS;
        public float force;
        public float height;
        public float sqRadius;
        public Vector3 direction;
        
        public void Reset()
        {
            posWS = Vector3.zero;
            force = 0;
            height = 0;
            sqRadius = 0;
            direction = Vector3.forward;
        }
    }; 

    [System.Serializable]
    public struct MotorOmni
    {
        public Vector3 posWS;
        public float force;
        public float radiusSq;

        public void Reset()
        {
            posWS = Vector3.zero;
            force = 0;
            radiusSq = 0;
        }
    };
    [System.Serializable]
    public struct MotorVortex
    {
        public Vector3 posWS;
        public float force;
        public float radiusSq;
        public Vector3 axis;

        public void Reset()
        {
            posWS = Vector3.zero;
            force = 0;
            radiusSq = 0;
            axis = Vector3.up;
        }
    };

    [System.Serializable]
    public struct MotorBall
    {
        public Vector3 posWS;
        public float force;
        public float sqRadius;
        public Vector3 direction;

        public void Reset()
        {
            posWS = Vector3.zero;
            force = 0;
            sqRadius = 0;
            direction = Vector3.forward;
        }
    };

    public enum MotorType
    {
        Directional,
    }
}
