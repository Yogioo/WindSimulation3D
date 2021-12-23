using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wind.Core;
using Wind.Motor;

namespace Wind.Common
{
    [System.Serializable]
    public class MotorOmniConfig : IDisposable
    {
        public const int MAX_COUNT = 100;
        public List<WindMotor> MotorTrans;
        public MotorOmni[] MotorValue;
        public ComputeBuffer ComputeBuffer;

        public void InitMotorConfig()
        {
            MotorTrans = new List<WindMotor>(MotorDirectionalConfig.MAX_COUNT);
            MotorValue = new MotorOmni[MotorDirectionalConfig.MAX_COUNT];
            ComputeBuffer = new ComputeBuffer(MAX_COUNT, sizeof(float) * 5);
        }

        public void UpdateComputeBuffer()
        {
            for (int i = 0; i < MotorTrans.Count; i++)
            {
                var p = (MotorTrans[i].transform.position) ;
                MotorValue[i].posWS = p;
                MotorValue[i].force = MotorTrans[i].Force * Time.deltaTime;
                MotorValue[i].radiusSq = Mathf.Pow(MotorTrans[i].Radius, 2);
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
