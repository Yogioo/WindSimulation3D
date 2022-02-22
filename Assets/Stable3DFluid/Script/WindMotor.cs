using System;
using Sirenix.OdinInspector;
using Sunset;
using UnityEngine;
using Wind.Core;

namespace Wind.Motor
{
    public class WindMotor : MonoBehaviour
    {
        public WindMode WindSpawnMode
        {
            get { return _WindSpawnMode; }
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

        public WindMode _WindSpawnMode = WindMode.Directional;

        public float Radius { get; private set; }
        public float Force { get; private set; }
        public float Height { get; private set; }

        public AnimationCurve RadiusCurve = AnimationCurve.Linear(0, 1, 1, 1);
        public AnimationCurve ForceCurve = AnimationCurve.Linear(0, 1, 1, 1);

        [ShowIf("@this._WindSpawnMode==WindMode.Cylinder")]
        public AnimationCurve HeightCurve = AnimationCurve.Linear(0, 1, 1, 1);

        public float TimeMultiply = 1;
        public float ForceIntensity = 3;
        public float RadiusIntensity = 2;

        [ShowIf("@this._WindSpawnMode==WindMode.Cylinder")]
        public float HeightIntensity = 1;

        public bool MultiplyByMoveSpeed = false;
        [ShowIf("@MultiplyByMoveSpeed")] public float SpeedMultiply = 1;

        private float moveSpeed = 1;

        public bool IsLoop = false;


        private float _Timer;
        private Vector3 lastTickPos;

        public enum WindMode
        {
            Directional,
            Vortex,
            Omni,
            Ball,
            Cylinder
        }

        void OnEnable()
        {
            _Timer = 0;
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
                case WindMode.Ball:
                    StableWind.Instance.AddMotorBall(this);
                    break;
                case WindMode.Cylinder:
                    StableWind.Instance.AddMotorCylinder(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void Update()
        {
            _Timer += UnityEngine.Time.deltaTime;

            float percent = _Timer / TimeMultiply;
            if (_Timer > TimeMultiply)
            {
                if (IsLoop)
                {
                    _Timer = 0;
                }
                else
                {
                    this.enabled = false;
                }
            }

            Force = ForceCurve.Evaluate(percent) * ForceIntensity;
            Radius = RadiusCurve.Evaluate(percent) * RadiusIntensity;
            Height = HeightCurve.Evaluate(percent) * HeightIntensity;

            if (MultiplyByMoveSpeed)
            {
                if (lastTickPos != this.transform.position)
                {
                    moveSpeed = (lastTickPos - this.transform.position).magnitude * SpeedMultiply;
                    lastTickPos = this.transform.position;
                }
                else
                {
                    moveSpeed = 0;
                }

                Force *= moveSpeed;
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
                case WindMode.Ball:
                    StableWind.Instance.RemoveMotorBall(this);
                    break;
                case WindMode.Cylinder:
                    StableWind.Instance.RemoveMotorCylinder(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Mesh CylinderMesh;
        void Awake()
        {
            CylinderMesh=(Mesh)Resources.Load<Mesh>("CylinderGizmos");
        }
        void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(this.transform.position, Radius);

            if (this.WindSpawnMode == WindMode.Directional || this.WindSpawnMode == WindMode.Directional ||
                this.WindSpawnMode == WindMode.Ball)
            {
                Gizmos.DrawRay(this.transform.position, this.transform.forward);
            }

            else if (this.WindSpawnMode == WindMode.Cylinder && Application.isPlaying)
            {
                Gizmos.DrawWireMesh(CylinderMesh, this.transform.position,
                    this.transform.rotation, new Vector3(Radius*2,  Radius*2,Height));
            }
        }
    }
}