using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Wind.Common;
using Wind.Motor;

namespace Wind.Core
{
    public class WindSimulation : MonoBehaviour, IDisposable
    {
        private static WindSimulation _Singleton;
        public static WindSimulation Instance
        {
            get
            {
                return _Singleton;
            }
        }

        public Vector3Int BufferSize = new Vector3Int(32, 16, 32);

        public RenderTexture VelocityRT1;
        public RenderTexture VelocityRT2;

        public RenderTexture DensityRT1;
        public RenderTexture DensityRT2;

        public RenderTexture CurlRT, DivergenceRT, PressureRT2, PressureRT1;

        // 平流
        public ComputeShader AdvectionCompute;
        // 风力发动
        public ComputeShader ApplyMotorCompute;
        // 卷曲计算
        public ComputeShader CurlCompute;
        // 卷曲应用
        public ComputeShader VorticityCompute;
        // 扩散
        public ComputeShader DivergenceCompute;
        public ComputeShader PressureCompute;

        public ComputeShader SubtractCompute;

        public ComputeShader CombineCompute;
        public ComputeShader SplitCompute;

        public MotorDirectionalConfig DirectionalConfig;
        public MotorOmniConfig OmniConfig;
        public MotorVortexConfig VortexConfig;

        // 贴图采样的缩放
        public Vector3 divisionSize;

        public bool MotorsOn = true, AdvectionOn = true;

        private Kernels Kernels;
        private ShaderIDs ShaderIDs;

        [Range(0.95f, 1.0f)]
        public float DensityDiffusion = 0.995f;//密度消失速度，此值越大则粘度越小，越容易看到烟雾效果
        [Range(0.95f, 1.0f)]
        public float VelocityDiffusion = 0.995f;//速度扩散速度，此值越大则粘度越小，越容易看到烟雾效果
        [Range(1, 60)]
        public int Iterations = 50;//泊松方程迭代次数
        [Range(0, 60)]
        public float Vorticity = 50f;//控制漩涡缩放
        private void InitProperty()
        {
            ShaderIDs = new ShaderIDs();
            Kernels = new Kernels();
            divisionSize = this.BufferSize / 2;
        }
        private void InitBuffer()
        {
            VelocityRT1 = new RenderTexture(BufferSize.x, BufferSize.y, 0, RenderTextureFormat.ARGBFloat)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = BufferSize.z,
                enableRandomWrite = true,
                name = "VelocityRT1"
            };
            VelocityRT1.Create();

            VelocityRT2 = new RenderTexture(BufferSize.x, BufferSize.y, 0, RenderTextureFormat.ARGBFloat)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = BufferSize.z,
                enableRandomWrite = true,
                name = "VelocityRT2"
            };
            VelocityRT2.Create();

            DensityRT1 = new RenderTexture(BufferSize.x, BufferSize.y, 0, RenderTextureFormat.RFloat)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = BufferSize.z,
                enableRandomWrite = true,
                name = "DensityRT1"
            };
            DensityRT1.Create();

            DensityRT2 = new RenderTexture(BufferSize.x, BufferSize.y, 0, RenderTextureFormat.RFloat)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = BufferSize.z,
                enableRandomWrite = true,
                name = "DensityRT2"
            };
            DensityRT2.Create();


            CurlRT = new RenderTexture(BufferSize.x, BufferSize.y, 0, RenderTextureFormat.RFloat)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = BufferSize.z,
                enableRandomWrite = true,
                name = "CurlRT"
            };
            CurlRT.Create();


            DivergenceRT = new RenderTexture(BufferSize.x, BufferSize.y, 0, RenderTextureFormat.RFloat)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = BufferSize.z,
                enableRandomWrite = true,
                name = "DivergenceRT"
            };
            DivergenceRT.Create();


            PressureRT2 = new RenderTexture(BufferSize.x, BufferSize.y, 0, RenderTextureFormat.RFloat)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = BufferSize.z,
                enableRandomWrite = true,
                name = "PressureRT2"
            };
            PressureRT2.Create();

            PressureRT1 = new RenderTexture(BufferSize.x, BufferSize.y, 0, RenderTextureFormat.RFloat)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = BufferSize.z,
                enableRandomWrite = true,
                name = "PressureRT1"
            };
            PressureRT1.Create();

        }

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


        private void SetUpComputeShader()
        {
            Shader.SetGlobalTexture(ShaderIDs.WindGlobalTex, VelocityRT1);
        }

        public void AddMotorDirectional(WindMotor p_Motor)
        {
            this.DirectionalConfig.MotorTrans.Add(p_Motor);
            UpdateMotorDirectional();
        }
        public void RemoveMotorDirectional(WindMotor p_Motor)
        {
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
            if (this.VortexConfig.MotorTrans.Remove(p_Motor))
            {
                UpdateMotorVortex();
            }
        }

        private void UpdateMotorDirectional()
        {
            this.ApplyMotorCompute.SetInt(ShaderIDs.MotorCount, this.DirectionalConfig.GetCurrentIndex());
            this.ApplyMotorCompute.SetBuffer(Kernels.KernelID, ShaderIDs.MotorGroup, this.DirectionalConfig.ComputeBuffer);
        }

        private void UpdateMotorOmni()
        {
            this.ApplyMotorCompute.SetInt(ShaderIDs._OmniCount, this.OmniConfig.GetCurrentIndex());
            this.ApplyMotorCompute.SetBuffer(Kernels.KernelID, ShaderIDs._OmniMotors, this.OmniConfig.ComputeBuffer);
        }

        private void UpdateMotorVortex()
        {
            this.ApplyMotorCompute.SetInt(ShaderIDs._VortexCount, this.VortexConfig.GetCurrentIndex());
            this.ApplyMotorCompute.SetBuffer(Kernels.KernelID, ShaderIDs._VortexMotors, this.VortexConfig.ComputeBuffer);
        }

        void OnEnable()
        {
            _Singleton = this;
            InitProperty();
            InitBuffer();
            InitMotors();

            SetUpComputeShader();
        }

        private void ApplyMotors()
        {
            this.DirectionalConfig.UpdateComputeBuffer();
            this.OmniConfig.UpdateComputeBuffer();
            this.VortexConfig.UpdateComputeBuffer();

            ApplyMotorCompute.SetTexture(Kernels.KernelID, ShaderIDs.FromTex, VelocityRT2);
            ApplyMotorCompute.SetTexture(Kernels.KernelID, ShaderIDs.ToTex, VelocityRT1);

            ApplyMotorCompute.SetTexture(Kernels.KernelID, ShaderIDs.DensityTex2, DensityRT2);
            ApplyMotorCompute.SetTexture(Kernels.KernelID, ShaderIDs.DensityTex1, DensityRT1);
            DispatchShader(ApplyMotorCompute);
            //this.ApplyMotorCompute.Dispatch(Kernels.KernelID, BufferSize.x / 8, BufferSize.y / 8, BufferSize.z / 8);
            Graphics.CopyTexture(VelocityRT1, VelocityRT2);
            Graphics.CopyTexture(DensityRT1, DensityRT2);

        }

        void DispatchShader(ComputeShader shader)
        {
            shader.Dispatch(Kernels.KernelID, BufferSize.x / 8, BufferSize.y / 8, BufferSize.z / 8);
        }
        void Update()
        {
            Shader.SetGlobalVector("_WindCenter", this.transform.position);
            Shader.SetGlobalVector("_DivisionSize", divisionSize);

            AdvectionCompute.SetFloat(ShaderIDs.DeltaTime, Time.deltaTime);

            if (AdvectionOn)
            {
                AdvectionCompute.SetFloat(ShaderIDs.Dissipation, VelocityDiffusion);
                AdvectionCompute.SetTexture(Kernels.KernelID, ShaderIDs.VelocityTex_Read, VelocityRT2);
                AdvectionCompute.SetTexture(Kernels.KernelID, ShaderIDs.VelocityDensityTex, VelocityRT2);
                AdvectionCompute.SetTexture(Kernels.KernelID, ShaderIDs.ResultTex, VelocityRT1);
                DispatchShader(AdvectionCompute);
                Graphics.CopyTexture(VelocityRT1, VelocityRT2);
            }

            if (MotorsOn)
            {
                ApplyMotors();
            }



            // 2 是过程 1是结果

            //// 计算卷曲程度
            //CurlCompute.SetTexture(Kernels.KernelID, ShaderIDs.VelocityTex_Read, VelocityRT2);
            //CurlCompute.SetTexture(Kernels.KernelID, ShaderIDs.ResultTex, CurlRT);
            //DispatchShader(CurlCompute);


            //// 为速度应用卷曲效果 得到有散度的速度场
            //VorticityCompute.SetFloat(ShaderIDs.DeltaTime, Time.deltaTime);
            //VorticityCompute.SetFloat(ShaderIDs.Curl, Vorticity);
            //VorticityCompute.SetTexture(Kernels.KernelID, ShaderIDs.CurlTex, CurlRT);
            //VorticityCompute.SetTexture(Kernels.KernelID, ShaderIDs.VelocityTex_Read, VelocityRT2);
            //VorticityCompute.SetTexture(Kernels.KernelID, ShaderIDs.ResultTex, VelocityRT1);
            //DispatchShader(VorticityCompute);
            //Graphics.CopyTexture(VelocityRT1, VelocityRT2);

            ////return;

            ////第六步：计算散度贴图
            //DivergenceCompute.SetTexture(Kernels.KernelID, ShaderIDs.VelocityTex_Read, VelocityRT1);
            //DivergenceCompute.SetTexture(Kernels.KernelID, ShaderIDs.ResultTex, DivergenceRT);
            //DispatchShader(DivergenceCompute);
            

            ////第七步：计算压力
            //PressureCompute.SetTexture(Kernels.KernelID, ShaderIDs.DivergenceTex, DivergenceRT);
            //for (int i = 0; i < Iterations; i++)
            //{
            //    PressureCompute.SetTexture(Kernels.KernelID, ShaderIDs.PressureTex, PressureRT2);
            //    PressureCompute.SetTexture(Kernels.KernelID, ShaderIDs.ResultTex, PressureRT1);
            //    DispatchShader(PressureCompute);
            //    Graphics.CopyTexture(PressureRT1, PressureRT2);
            //}

            ////第八步：速度场减去压力梯度，得到无散度的速度场
            //SubtractCompute.SetTexture(Kernels.KernelID, ShaderIDs.PressureTex, PressureRT2);
            //SubtractCompute.SetTexture(Kernels.KernelID, ShaderIDs.VelocityTex_Read, VelocityRT2);
            //SubtractCompute.SetTexture(Kernels.KernelID, ShaderIDs.ResultTex, VelocityRT1);
            //DispatchShader(SubtractCompute);
            //Graphics.CopyTexture(VelocityRT1, VelocityRT2);

            ////第九步：用最终速度去平流密度
            //AdvectionCompute.SetFloat(ShaderIDs.Dissipation, DensityDiffusion);
            //AdvectionCompute.SetTexture(Kernels.KernelID, ShaderIDs.VelocityTex_Read, VelocityRT2);
            //AdvectionCompute.SetTexture(Kernels.KernelID, ShaderIDs.VelocityDensityTex, DensityRT2);
            //AdvectionCompute.SetTexture(Kernels.KernelID, ShaderIDs.ResultTex, DensityRT1);
            //DispatchShader(AdvectionCompute);
            //Graphics.CopyTexture(DensityRT1, DensityRT2);

        }





        void OnDisable()
        {
            Dispose();
        }

        public void Dispose()
        {
            this.DensityRT1?.Release();
            this.DensityRT1 = null;
            this.DensityRT2?.Release();
            this.DensityRT2 = null;

            this.VelocityRT1?.Release();
            this.VelocityRT1 = null;
            this.VelocityRT2?.Release();
            this.VelocityRT2 = null;

            DirectionalConfig?.Dispose();
            OmniConfig?.Dispose();
            VortexConfig?.Dispose();
        }


    }
}