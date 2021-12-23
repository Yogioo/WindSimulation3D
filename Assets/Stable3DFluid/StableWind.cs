using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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

    }
    static class VFB
    {
        public static RenderTexture V1;
        public static RenderTexture V2;
        public static RenderTexture V3;
        public static RenderTexture DV;
        public static RenderTexture P1;
        public static RenderTexture P2;
    }

    public ComputeShader Compute;

    public bool Advect = true;
    public bool Diffusion = true;
    public bool Force = true;
    public bool Divergence = true;
    public bool Pressure = true;


    [Header("平流系数")]
    public float AdvectValue = 1;

    [Header("扩散系数")]
    public float _V;
    [Header("扩散迭代次数"), Range(10, 25)]
    public int _Iteration = 20;

    [Header("压力迭代次数"), Range(20, 40)]
    public int _IterationPressure = 20;

    public float PressureValue = 0.9f;


    public Vector3Int Size = new Vector3Int(32, 16, 32);
    public Vector3 _WindCenter, _DivisionSize;

    int ThreadCountX { get { return Size.x / 8; } }
    int ThreadCountY { get { return Size.y / 8; } }
    int ThreadCountZ { get { return Size.z / 8; } }
    int ResolutionX { get { return ThreadCountX * 8; } }
    int ResolutionY { get { return ThreadCountY * 8; } }
    int ResolutionZ { get { return ThreadCountZ * 8; } }

    public bool DisplayDivergence = false;
    public bool DisplayPressure = false;
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

    void Start()
    {
        VFB.V1 = AllocateBuffer(3);
        VFB.V2 = AllocateBuffer(3);
        VFB.V3 = AllocateBuffer(3);
        VFB.DV = AllocateBuffer(1);
        VFB.P1 = AllocateBuffer(1);
        VFB.P2 = AllocateBuffer(1);
    }
    void OnDestroy()
    {
        Destroy(VFB.V1);
        Destroy(VFB.V2);
        Destroy(VFB.V3);
        Destroy(VFB.DV);
        Destroy(VFB.P1);
        Destroy(VFB.P2);
    }

    public Vector3 worldPos;
    void Update()
    {
        float dt = Time.deltaTime;
        float dx, alpha, beta;
        dx = 1.0f / ResolutionX;

        Compute.SetFloat("DeltaTime", dt);
        Compute.SetVector("worldPos", worldPos);

        // Advection
        if (Advect)
        {
            Compute.SetFloat("AdvectValue", AdvectValue * dt);
            Compute.SetTexture(Kernels.Advect, "U_in", VFB.V1);
            Compute.SetTexture(Kernels.Advect, "U_out", VFB.V2);
            Compute.Dispatch(Kernels.Advect, ThreadCountX, ThreadCountY, ThreadCountZ);
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

            alpha = 1 / _V * dt;
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
        }

        if (Force)
        {
            Compute.SetTexture(Kernels.AddForce, "U_out", VFB.V1);
            Compute.Dispatch(Kernels.AddForce, ThreadCountX, ThreadCountY, ThreadCountZ);
        }

        // Divergence

        // Divergence Init
        if (Divergence)
        {
            Compute.SetTexture(Kernels.Divergence, "U_in", VFB.V1);
            Compute.SetTexture(Kernels.Divergence, "Dv_out", VFB.DV);
            Compute.Dispatch(Kernels.Divergence, ThreadCountX, ThreadCountY, ThreadCountZ);
        }

        if (Pressure)
        {
            // Clear Pressure
            Compute.SetTexture(Kernels.Clear3D, "P_out", VFB.P1);
            Compute.Dispatch(Kernels.Clear3D, ThreadCountX, ThreadCountY, ThreadCountZ);
            // Calc Gradient
            Compute.SetTexture(Kernels.JacobiPressure, "Dv_in", VFB.DV);
            alpha = - 1 * PressureValue;
            beta = 1 / (6.0f);
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
        }



        if (DisplayDivergence)
        {
            Shader.SetGlobalTexture("_VelocityMap", VFB.DV);
        }
        else if (DisplayPressure)
        {
            Shader.SetGlobalTexture("_VelocityMap", VFB.P1);
        }
        else
        {
            Shader.SetGlobalTexture("_VelocityMap", VFB.V1);
        }




        Shader.SetGlobalVector("_WindCenter", _WindCenter);
        Shader.SetGlobalVector("_DivisionSize", _DivisionSize);
    }
}
