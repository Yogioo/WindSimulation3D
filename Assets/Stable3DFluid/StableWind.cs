using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Wind.Common;
using Wind.Motor;

namespace Wind.Core
{
    /// <summary>
    /// 风场模拟
    /// </summary>
    public class StableWind : MonoBehaviour
    {
        static class Kernels
        {
            public const int Advect = 0;
            public const int Jacobi = 1;
            public const int Divergence = 2;
            public const int Gradient = 3;
            public const int AddForce = 4;
            public const int Clear = 5;
            public const int Clear3D = 6;
            public const int JacobiPressure = 7;
            public const int Move = 8;

        }
        static class VFB
        {
            public static RenderTexture V1;
            public static RenderTexture V2;
            public static RenderTexture V3;
            public static RenderTexture DV;
            public static RenderTexture P1;
            public static RenderTexture P2;
            public static RenderTexture P3;//Debug
        }

        public static StableWind Instance => _Instance;
        private static StableWind _Instance;

        #region Config
        public ComputeShader Compute;
        public bool Move = true; // 移动
        public bool Advect = true;// 平流
        public bool Diffusion = true; // 扩散
        public bool Force = true; // 力
        public bool Divergence = true; // 散度
        public bool Pressure = true; // 压力

        [Header("风场移动时的移动距离")]
        public float MoveValue = 2;
        [Header("平流系数")]
        public float AdvectValue = 1;
        [Header("平流能量衰减")]
        public float AdvectFade = 0.99f;

        [Header("扩散系数")]
        public float _V = 1;
        [Header("扩散迭代次数"), Range(10, 25)]
        public int _Iteration = 25;

        [Header("压力迭代次数"),Range(10,20)] //20-40
        public int _IterationPressure = 20;

        [Header("压力系数")]
        public float PressureValue = .1f;
        [Header("风场大小")]
        public Vector3Int Size = new Vector3Int(32, 16, 32);
        #endregion

        #region Tmp
        int ThreadCountX { get { return Size.x / 8; } }
        int ThreadCountY { get { return Size.y / 8; } }
        int ThreadCountZ { get { return Size.z / 8; } }
        int ResolutionX { get { return ThreadCountX * 8; } }
        int ResolutionY { get { return ThreadCountY * 8; } }
        int ResolutionZ { get { return ThreadCountZ * 8; } }

        [Header("显示散度贴图")]
        public bool DisplayDivergence = false;
        [Header("显示压力贴图(需要取消勾选显示散度贴图)")]
        public bool DisplayPressure = false;

        private Vector3 moveDir;
        private Vector3 lastTickPos;
        private Vector3 _DivisionSize;
        #endregion

        RenderTexture AllocateBuffer(int componentCount, int width = 0, int height = 0)
        {
            var format = RenderTextureFormat.ARGBHalf;
            if (componentCount == 1) format = RenderTextureFormat.RHalf;
            if (componentCount == 2) format = RenderTextureFormat.RGHalf;
            if (componentCount == 3) format = RenderTextureFormat.ARGBHalf;

            if (width == 0) width = ResolutionX;
            if (height == 0) height = ResolutionY;

            var rt = new RenderTexture(width, height, 0, format);
            //3D Texture
            rt.dimension = TextureDimension.Tex3D;
            rt.volumeDepth = ResolutionZ;
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }

        void OnEnable()
        {
            if (_Instance == null)
            {
                _Instance = this;
                InitMotors();
            }
        }
        void Start()
        {
            _DivisionSize = new Vector3(ResolutionX / 2.0f, ResolutionY / 2.0f, ResolutionZ / 2.0f);
            VFB.V1 = AllocateBuffer(3);
            VFB.V2 = AllocateBuffer(3);
            VFB.V3 = AllocateBuffer(3);
            VFB.DV = AllocateBuffer(1);
            VFB.P1 = AllocateBuffer(1);
            VFB.P2 = AllocateBuffer(1);
            VFB.P3 = AllocateBuffer(1);

        }
        void OnDestroy()
        {
            Destroy(VFB.V1);
            Destroy(VFB.V2);
            Destroy(VFB.V3);
            Destroy(VFB.DV);
            Destroy(VFB.P1);
            Destroy(VFB.P2);
            Destroy(VFB.P3);
            this.DirectionalConfig.Dispose();
            this.OmniConfig.Dispose();
            this.VortexConfig.Dispose();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float alpha, beta;
            float dx = 1.0f / ResolutionX;

            Compute.SetFloat("DeltaTime", dt);
            Compute.SetFloat("_Time", Time.time);

            // Advection
            if (Advect)
            {
                Profiler.BeginSample("WindSimulate.Advection");
                Compute.SetFloat("AdvectValue", AdvectValue * dt);
                Compute.SetFloat("AdvectFade", AdvectFade);
                Compute.SetTexture(Kernels.Advect, "U_in", VFB.V1);
                Compute.SetTexture(Kernels.Advect, "U_out", VFB.V2);
                Compute.Dispatch(Kernels.Advect, ThreadCountX, ThreadCountY, ThreadCountZ);
                Profiler.EndSample();
            }
            else
            {
                Graphics.CopyTexture(VFB.V1, VFB.V2);
            }

            Graphics.CopyTexture(VFB.V2, VFB.V1);
            Graphics.CopyTexture(VFB.V2, VFB.V3);

            // Diffusion
            if (Diffusion)
            {
                Profiler.BeginSample("WindSimulate.Diffusion");
                alpha = 1 / (_V * dt);
                beta = 1 / (6.0f + alpha);
                Compute.SetFloat("Alpha", alpha);
                Compute.SetFloat("Beta", beta);

                Compute.SetTexture(Kernels.Jacobi, "U_in", VFB.V1);
                for (int i = 0; i < _Iteration; i++)
                {
                    Compute.SetTexture(Kernels.Jacobi, "U_inout", VFB.V3);
                    Compute.SetTexture(Kernels.Jacobi, "U_out", VFB.V2);
                    Compute.Dispatch(Kernels.Jacobi, ThreadCountX, ThreadCountY, ThreadCountZ);


                    Compute.SetTexture(Kernels.Jacobi, "U_inout", VFB.V2);
                    Compute.SetTexture(Kernels.Jacobi, "U_out", VFB.V3);
                    Compute.Dispatch(Kernels.Jacobi, ThreadCountX, ThreadCountY, ThreadCountZ);
                }
                Graphics.CopyTexture(VFB.V3, VFB.V1);
                Profiler.EndSample();
            }

            if (Force)
            {
                Profiler.BeginSample("WindSimulate.Force");
                this.DirectionalConfig.UpdateComputeBuffer();
                this.OmniConfig.UpdateComputeBuffer();
                this.VortexConfig.UpdateComputeBuffer();
                this.MotorBallConfig.UpdateComputeBuffer();
                Compute.SetTexture(Kernels.AddForce, "U_out", VFB.V1);
                Compute.Dispatch(Kernels.AddForce, ThreadCountX, ThreadCountY, ThreadCountZ);
                Profiler.EndSample();
            }

            // Divergence

            // Divergence Init
            if (Divergence)
            {
                Profiler.BeginSample("WindSimulate.Divergence");
                Compute.SetTexture(Kernels.Divergence, "U_in", VFB.V1);
                Compute.SetTexture(Kernels.Divergence, "Dv_out", VFB.DV);
                Compute.Dispatch(Kernels.Divergence, ThreadCountX, ThreadCountY, ThreadCountZ);
                Profiler.EndSample();
            }

            if (Pressure)
            {
                Profiler.BeginSample("WindSimulate.Pressure");
                // Clear Pressure
                Compute.SetTexture(Kernels.Clear3D, "P_out", VFB.P1);
                Compute.Dispatch(Kernels.Clear3D, ThreadCountX, ThreadCountY, ThreadCountZ);
                // Calc Gradient
                Compute.SetTexture(Kernels.JacobiPressure, "Dv_in", VFB.DV);
                alpha = - dx * dx * PressureValue;
                beta = 1.0f / (6.0f);
                Compute.SetFloat("Alpha", alpha);
                Compute.SetFloat("Beta", beta);
                for (int i = 0; i < _IterationPressure; i++)
                {
                    Compute.SetTexture(Kernels.JacobiPressure, "P_in", VFB.P1);
                    Compute.SetTexture(Kernels.JacobiPressure, "P_out", VFB.P2);
                    Compute.Dispatch(Kernels.JacobiPressure, ThreadCountX, ThreadCountY, ThreadCountZ);


                    Compute.SetTexture(Kernels.JacobiPressure, "P_in", VFB.P2);
                    Compute.SetTexture(Kernels.JacobiPressure, "P_out", VFB.P1);
                    Compute.Dispatch(Kernels.JacobiPressure, ThreadCountX, ThreadCountY, ThreadCountZ);
                }
                // Minus Gradient
                Compute.SetTexture(Kernels.Gradient, "P_in", VFB.P1);
                Compute.SetTexture(Kernels.Gradient, "U_out", VFB.V1);
                Compute.Dispatch(Kernels.Gradient, ThreadCountX, ThreadCountY, ThreadCountZ);
                Profiler.EndSample();
            }

            if (Move)
            {
                var dir = lastTickPos - this.transform.position;
                var moveDistance = dir.sqrMagnitude;
                if (moveDistance > 0.01f)
                {
                    lastTickPos = this.transform.position;
                    moveDir = dir * Time.deltaTime * MoveValue;

                    Profiler.BeginSample("WindSimulate.Move");
                    Graphics.CopyTexture(VFB.V1, VFB.V2);
                    Compute.SetVector("_MoveDir", moveDir);
                    Compute.SetTexture(Kernels.Move, "U_in", VFB.V2);
                    Compute.SetTexture(Kernels.Move, "U_out", VFB.V1);
                    Compute.Dispatch(Kernels.Move, ThreadCountX, ThreadCountY, ThreadCountZ);
                    Profiler.EndSample();
                }
            }

            if (DisplayDivergence)
            {
                Shader.SetGlobalTexture("_VelocityMap", VFB.DV);
            }
            else if (DisplayPressure)
            {
                Graphics.CopyTexture(VFB.P1, VFB.P3);
                Shader.SetGlobalTexture("_VelocityMap", VFB.P3);
            }
            else
            {
                Shader.SetGlobalTexture("_VelocityMap", VFB.V1);
            }

            Shader.SetGlobalVector("_WindCenter", this.transform.position);
            Shader.SetGlobalVector("_DivisionSize", _DivisionSize);
        }

        #region Motors
        public MotorDirectionalConfig DirectionalConfig;
        public MotorOmniConfig OmniConfig;
        public MotorVortexConfig VortexConfig;
        public MotorBallConfig MotorBallConfig;
        private void InitMotors()
        {
            this.DirectionalConfig = new MotorDirectionalConfig();
            this.DirectionalConfig.InitMotorConfig();
            this.OmniConfig = new MotorOmniConfig();
            this.OmniConfig.InitMotorConfig();
            this.VortexConfig = new MotorVortexConfig();
            this.VortexConfig.InitMotorConfig();
            this.MotorBallConfig = new MotorBallConfig();
            this.MotorBallConfig.InitMotorConfig();


            UpdateMotorDirectional();
            UpdateMotorOmni();
            UpdateMotorVortex();
            UpdateMotorBall();
        }
        public void AddMotorDirectional(WindMotor p_Motor)
        {
            this.DirectionalConfig.MotorTrans.Add(p_Motor);
            UpdateMotorDirectional();
        }
        public void RemoveMotorDirectional(WindMotor p_Motor)
        {
            var index = this.DirectionalConfig.MotorTrans.IndexOf(p_Motor);
            if (index > -1)
            {
                this.DirectionalConfig.MotorValue[index].Reset();
            }

            if (this.DirectionalConfig.MotorTrans.Remove(p_Motor))
            {
                UpdateMotorDirectional();
            }
        }
        public void AddMotorOmni(WindMotor p_Motor)
        {
            this.OmniConfig.MotorTrans.Add(p_Motor);
            UpdateMotorOmni();
        }
        public void RemoveMotorOmni(WindMotor p_Motor)
        {
            var index = this.OmniConfig.MotorTrans.IndexOf(p_Motor);
            if (index > -1)
            {
                this.OmniConfig.MotorValue[index].Reset();
            }

            if (this.OmniConfig.MotorTrans.Remove(p_Motor))
            {
                UpdateMotorOmni();
            }
        }
        public void AddMotorVortexMotor(WindMotor p_Motor)
        {
            this.VortexConfig.MotorTrans.Add(p_Motor);
            UpdateMotorVortex();
        }
        public void RemoveMotorVortexMotor(WindMotor p_Motor)
        {
            var index = this.VortexConfig.MotorTrans.IndexOf(p_Motor);
            if (index > -1)
            {
                this.VortexConfig.MotorValue[index].Reset();
            }

            if (this.VortexConfig.MotorTrans.Remove(p_Motor))
            {
                UpdateMotorVortex();
            }
        }
        public void AddMotorBall(WindMotor p_Motor)
        {
            this.MotorBallConfig.MotorTrans.Add(p_Motor);
            UpdateMotorBall();
        }
        public void RemoveMotorBall(WindMotor p_Motor)
        {
            var index = this.MotorBallConfig.MotorTrans.IndexOf(p_Motor);
            if (index > -1)
            {
                this.MotorBallConfig.MotorValue[index].Reset();
            }

            if (this.MotorBallConfig.MotorTrans.Remove(p_Motor))
            {
                UpdateMotorBall();
            }
        }
        private void UpdateMotorDirectional()
        {
            Compute.SetInt("_MotorCount", this.DirectionalConfig.GetCurrentIndex());
            Compute.SetBuffer(Kernels.AddForce, "_Motors", this.DirectionalConfig.ComputeBuffer);
        }
        private void UpdateMotorOmni()
        {
            Compute.SetInt("_OmniCount", this.OmniConfig.GetCurrentIndex());
            Compute.SetBuffer(Kernels.AddForce, "_OmniMotors", this.OmniConfig.ComputeBuffer);
        }
        private void UpdateMotorVortex()
        {
            Compute.SetInt("_VortexCount", this.VortexConfig.GetCurrentIndex());
            Compute.SetBuffer(Kernels.AddForce, "_VortexMotors", this.VortexConfig.ComputeBuffer);
        }
        private void UpdateMotorBall()
        {
            Compute.SetInt("_BallCount", this.MotorBallConfig.GetCurrentIndex());
            Compute.SetBuffer(Kernels.AddForce, "_BallMotors", this.MotorBallConfig.ComputeBuffer);
        }

        #endregion

        private void OnDrawGizmos()
        {
            var originColor = Gizmos.color;
            Gizmos.color = Color.green;


            Gizmos.DrawWireCube(this.transform.position, this.Size);


            Gizmos.color = originColor;
        }

    }
}
