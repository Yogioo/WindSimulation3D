using System;
using System.Collections.Generic;
using UnityEngine;
using Wind.Motor;

namespace Wind.Common
{
    [System.Serializable]
    public class MotorCylinderConfig : IDisposable
    {
        public const int MAX_COUNT = 100;
        public List<WindMotor> MotorTrans;
        public MotorCylinder[] MotorValue;
        public ComputeBuffer ComputeBuffer;

        public MotorCylinderConfig()
        {

        }

        public void InitMotorConfig()
        {
            MotorTrans = new List<WindMotor>(MAX_COUNT);
            MotorValue = new MotorCylinder[MAX_COUNT];
            ComputeBuffer = new ComputeBuffer(MAX_COUNT, sizeof(float) * 9);
        }

        public void UpdateComputeBuffer()
        {
            for (int i = 0; i < MotorTrans.Count; i++)
            {
                MotorValue[i].posWS = MotorTrans[i].transform.position;
                MotorValue[i].force = MotorTrans[i].Force * Time.deltaTime;
                MotorValue[i].direction = MotorTrans[i].transform.forward;
                MotorValue[i].sqRadius = Mathf.Pow(MotorTrans[i].Radius,2);
                MotorValue[i].height = MotorTrans[i].Height;
            }
            ComputeBuffer.SetData(MotorValue);

        }

        public int GetCurrentIndex()
        {
            return this.MotorTrans.Count;
        }


        public void Dispose()
        {
            MotorTrans?.Clear();
            ComputeBuffer?.Dispose();
        }
    }
}
