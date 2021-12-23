
using UnityEngine;

namespace Wind.Core
{
    public class Kernels
    {
        private const string CS_WIND_KERNEL_NAME = "CSWindMain";

        public int KernelID = 0;
        //public int ApplyMotorKernelIndex;
        //public int AdvectionKernelIndex;
        //public int CombineKernelIndex;
        //public int SplitKernelIndex;

        public Kernels(
            //ComputeShader p_ApplyMotor,
            //ComputeShader p_ReverseAdvection,
            //ComputeShader p_Combine,
            //ComputeShader p_Split
        )
        {
            //ApplyMotorKernelIndex = p_ApplyMotor.FindKernel(CS_WIND_KERNEL_NAME);
            //AdvectionKernelIndex = p_ReverseAdvection.FindKernel(CS_WIND_KERNEL_NAME);
            //CombineKernelIndex = p_Combine.FindKernel(CS_WIND_KERNEL_NAME);
            //SplitKernelIndex = p_Split.FindKernel(CS_WIND_KERNEL_NAME);

            //Debug.Log($"{ApplyMotorKernelIndex},{AdvectionKernelIndex},{CombineKernelIndex},{SplitKernelIndex}");

        }
    }

    public class ShaderIDs
    {
        public int MotorGroup,MotorCount, WindGlobalTex,ToTex,FromTex;

        public int _Density, DeltaTime, _OmniCount, _OmniMotors, _VortexCount, _VortexMotors;

        public int Dissipation, VelocityTex_Read, VelocityDensityTex, ResultTex;
        public int CurlTex;
        public int Curl;
        public int PressureTex;
        public int DivergenceTex;
        public int DensityTex1;
        public int DensityTex2;

        public ShaderIDs()
        {
            MotorGroup = Shader.PropertyToID("_Motors");
            MotorCount = Shader.PropertyToID("_MotorCount");
            WindGlobalTex = Shader.PropertyToID("_WindTex");
            ToTex = Shader.PropertyToID("_ToTex");
            FromTex = Shader.PropertyToID("_FromTex");
            DeltaTime = Shader.PropertyToID("DeltaTime");
            _Density = Shader.PropertyToID("_Density");
            _OmniCount = Shader.PropertyToID("_OmniCount");
            _VortexCount = Shader.PropertyToID("_VortexCount");
            _VortexMotors = Shader.PropertyToID("_VortexMotors");
           

            _OmniMotors = Shader.PropertyToID("_OmniMotors");

            Dissipation = Shader.PropertyToID("Dissipation");
            VelocityTex_Read = Shader.PropertyToID("VelocityTex_Read");
            VelocityDensityTex = Shader.PropertyToID("VelocityDensityTex");
            ResultTex = Shader.PropertyToID("ResultTex");
            CurlTex = Shader.PropertyToID("CurlTex");
            Curl = Shader.PropertyToID("Curl");
            PressureTex = Shader.PropertyToID("PressureTex");
            DivergenceTex = Shader.PropertyToID("DivergenceTex");
            DensityTex1 = Shader.PropertyToID("DensityTex1");
            DensityTex2 = Shader.PropertyToID("DensityTex2");
        }
    }

}
