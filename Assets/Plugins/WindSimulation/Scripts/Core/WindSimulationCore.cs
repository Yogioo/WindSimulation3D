using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Wind.Common;
using Wind.Motor;

namespace Wind.Core
{

    public class WindSimulationCore : MonoBehaviour
    {

        /// <summary>
        /// 粘度
        /// </summary>
        [Range(1e-20f, 0.1f)]
        [SerializeField]
        float _viscosity = 0.01f;

        [SerializeField]
        float _interpolate = 50f;


        private static WindSimulationCore _instance;

        public static WindSimulationCore Instance
        {
            get
            {
                return _instance;
            }
        }

        static class Kernel
        {
            public const int ApplyMotors = 0;
            public const int Advect = 1;
            public static int Jacobi2 = 2;
            public static int PSetup = 3;
            public static int PFinish = 4;
            public static int Jacobi1 = 5;
        }
        public ComputeShader _compute;

        public Vector3Int Resolution = new Vector3Int(32, 16, 32);

        int ThreadCountX { get { return (Resolution.x) / 8; } }
        int ThreadCountY { get { return (Resolution.y) / 8; } }
        int ThreadCountZ { get { return (Resolution.z) / 8; } }
        public Vector3 divisionSize
        {
            get
            {
                return Resolution / 2;
            }
        }


        #region Motors



        public MotorDirectionalConfig DirectionalConfig;
        public MotorOmniConfig OmniConfig;
        public MotorVortexConfig VortexConfig;


        private void InitMotors()
        {
            this.DirectionalConfig = new MotorDirectionalConfig();
            this.DirectionalConfig.InitMotorConfig();
            this.OmniConfig = new MotorOmniConfig();
            this.OmniConfig.InitMotorConfig();
            this.VortexConfig = new MotorVortexConfig();
            this.VortexConfig.InitMotorConfig();

            UpdateMotorDirectional();
            UpdateMotorOmni();
            UpdateMotorVortex();
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

        private void UpdateMotorDirectional()
        {

            _compute.SetInt("_MotorCount", this.DirectionalConfig.GetCurrentIndex());
            _compute.SetBuffer(Kernel.ApplyMotors, "_Motors", this.DirectionalConfig.ComputeBuffer);
        }

        private void UpdateMotorOmni()
        {
            _compute.SetInt("_OmniCount", this.OmniConfig.GetCurrentIndex());
            _compute.SetBuffer(Kernel.ApplyMotors, "_OmniMotors", this.OmniConfig.ComputeBuffer);
        }

        private void UpdateMotorVortex()
        {
            _compute.SetInt("_VortexCount", this.VortexConfig.GetCurrentIndex());
            _compute.SetBuffer(Kernel.ApplyMotors, "_VortexMotors", this.VortexConfig.ComputeBuffer);
        }

        #endregion
        /// <summary>
        /// Vector Field Buffers
        /// </summary>
        static class VFB
        {
            public static RenderTexture V1;
            public static RenderTexture V2;
            public static RenderTexture V3;
            public static RenderTexture P1;
            public static RenderTexture P2;
        }
        RenderTexture AllocateBuffer(int componentCount, int width = 0, int height = 0, int volumeDepth = 0)
        {
            var format = RenderTextureFormat.ARGBFloat;
            if (componentCount == 1) format = RenderTextureFormat.RFloat;
            if (componentCount == 2) format = RenderTextureFormat.RGFloat;

            if (width == 0) width = Resolution.x;
            if (height == 0) height = Resolution.y;
            if (volumeDepth == 0) volumeDepth = Resolution.z;

            var rt = new RenderTexture(width, height, 0, format);
            rt.dimension = TextureDimension.Tex3D;
            rt.volumeDepth = volumeDepth;
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }

        void OnEnable()
        {
            _instance = this;
            InitMotors();
        }
        void Start()
        {
            VFB.V1 = AllocateBuffer(3);
            VFB.V2 = AllocateBuffer(3);
            VFB.V3 = AllocateBuffer(3);
            VFB.P1 = AllocateBuffer(1);
            VFB.P2 = AllocateBuffer(1);
        }

        void OnDestroy()
        {
            Destroy(VFB.V1);
            Destroy(VFB.V2);
            Destroy(VFB.V3);
            Destroy(VFB.P1);
            Destroy(VFB.P2);
            this.DirectionalConfig.Dispose();
            this.OmniConfig.Dispose();
            this.VortexConfig.Dispose();
        }


        void Update()
        {

            var dt = Time.deltaTime;
            var dx = 1.0f;

            _compute.SetFloat("DeltaTime", dt);
            _compute.SetVector("CenterWS", this.transform.position);

            //Advection
            _compute.SetTexture(Kernel.Advect, "U_in", VFB.V1);
            _compute.SetTexture(Kernel.Advect, "W_out", VFB.V2);
            _compute.Dispatch(Kernel.Advect, ThreadCountX, ThreadCountY, ThreadCountZ);

            //Diffuse setup
            var dif_alpha = 1 / (_viscosity * dt);
            _compute.SetFloat("Alpha", dif_alpha);
            _compute.SetFloat("Beta", 6 + dif_alpha);
            Graphics.CopyTexture(VFB.V2, VFB.V1);
            _compute.SetTexture(Kernel.Jacobi2, "B2_in", VFB.V1);

            // Jacobi iteration
            for (var i = 0; i < _interpolate; i++)
            {
                _compute.SetTexture(Kernel.Jacobi2, "X2_in", VFB.V2);
                _compute.SetTexture(Kernel.Jacobi2, "X2_out", VFB.V3);
                _compute.Dispatch(Kernel.Jacobi2, ThreadCountX, ThreadCountY, ThreadCountZ);

                _compute.SetTexture(Kernel.Jacobi2, "X2_in", VFB.V3);
                _compute.SetTexture(Kernel.Jacobi2, "X2_out", VFB.V2);
                _compute.Dispatch(Kernel.Jacobi2, ThreadCountX, ThreadCountY, ThreadCountZ);
            }

            //ApplyMotors
            this.DirectionalConfig.UpdateComputeBuffer();
            this.OmniConfig.UpdateComputeBuffer();
            this.VortexConfig.UpdateComputeBuffer();
            _compute.SetTexture(Kernel.ApplyMotors, "W_in", VFB.V2);
            _compute.SetTexture(Kernel.ApplyMotors, "W_out", VFB.V3);
            _compute.Dispatch(Kernel.ApplyMotors, ThreadCountX, ThreadCountY, ThreadCountZ);

            // //// Projection setup
            // _compute.SetTexture(Kernel.PSetup, "W_in", VFB.V3);
            // _compute.SetTexture(Kernel.PSetup, "DivW_out", VFB.V2);
            // _compute.SetTexture(Kernel.PSetup, "P_out", VFB.P1);
            // _compute.Dispatch(Kernel.PSetup, ThreadCountX, ThreadCountY, ThreadCountZ);

            // // Jacobi iteration
            // _compute.SetFloat("Alpha", -1);
            // _compute.SetFloat("Beta", 6);
            // _compute.SetTexture(Kernel.Jacobi1, "B1_in", VFB.V2);

            // for (var i = 0; i < Count; i++)
            // {
            //     _compute.SetTexture(Kernel.Jacobi1, "X1_in", VFB.P1);
            //     _compute.SetTexture(Kernel.Jacobi1, "X1_out", VFB.P2);
            //     _compute.Dispatch(Kernel.Jacobi1, ThreadCountX, ThreadCountY, ThreadCountZ);

            //     _compute.SetTexture(Kernel.Jacobi1, "X1_in", VFB.P2);
            //     _compute.SetTexture(Kernel.Jacobi1, "X1_out", VFB.P1);
            //     _compute.Dispatch(Kernel.Jacobi1, ThreadCountX, ThreadCountY, ThreadCountZ);
            // }

            // //Projection finish
            // _compute.SetTexture(Kernel.PFinish, "W_in", VFB.V3);
            // _compute.SetTexture(Kernel.PFinish, "P_in", VFB.P1);
            // _compute.SetTexture(Kernel.PFinish, "U_out", VFB.V1);
            // _compute.Dispatch(Kernel.PFinish, ThreadCountX, ThreadCountY, ThreadCountZ);

            // Graphics.CopyTexture(VFB.V1, VFB.V3);
            Shader.SetGlobalTexture("_WindTex", VFB.V1);
            Shader.SetGlobalTexture("_V2", VFB.V2);
            Shader.SetGlobalTexture("_V3", VFB.V3);
            Shader.SetGlobalTexture("_PressureTex", VFB.P1);
            Shader.SetGlobalTexture("_PressureTex2", VFB.P2);


            Shader.SetGlobalVector("_DivisionSize", this.divisionSize);
            Shader.SetGlobalVector("_WindCenter", this.transform.position);

        }

        public int Count = 50;


    }
}
