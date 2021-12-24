﻿using System;
using UnityEngine;
using Wind.Core;

namespace Wind.Motor
{
    public class WindMotor : MonoBehaviour
    {
        public float Radius;
        public float Force;

        public WindMode WindSpawnMode
        {
            get
            {
                return _WindSpawnMode;
            }
            set
            {
                if (_WindSpawnMode != value)
                {
                    OnDisable();
                    _WindSpawnMode = value;
                    OnEnable();
                }
            }
        } 
        public WindMode _WindSpawnMode =WindMode.Directional;

        public enum WindMode
        {
            Directional,
            Vortex,
            Omni
        }

        void OnEnable()
        {
            switch (WindSpawnMode)
            {
                case WindMode.Directional:
                    StableWind.Instance.AddMotorDirectional(this);
                    break;
                case WindMode.Vortex:
                    StableWind.Instance.AddMotorVortexMotor(this);
                    break;
                case WindMode.Omni:
                    StableWind.Instance.AddMotorOmni(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void OnDisable()
        {
            switch (WindSpawnMode)
            {
                case WindMode.Directional:
                    StableWind.Instance.RemoveMotorDirectional(this);
                    break;
                case WindMode.Vortex:
                    StableWind.Instance.RemoveMotorVortexMotor(this);
                    break;
                case WindMode.Omni:
                    StableWind.Instance.RemoveMotorOmni(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(this.transform.position, Radius);

            if (this.WindSpawnMode == WindMode.Directional || this.WindSpawnMode == WindMode.Directional)
            {
                Gizmos.DrawRay(this.transform.position, this.transform.forward);
            }
        }
    }
}
