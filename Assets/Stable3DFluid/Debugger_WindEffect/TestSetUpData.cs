using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;
using Wind.Core;

public class TestSetUpData : MonoBehaviour
{
    public VisualEffect VFX;

    
    [Button]
    void Start()
    {
        RenderTexture velocityMap = StableWind.VFB.V1;
        VFX.SetTexture("WindTex",velocityMap);
        VFX.SetVector3("_WindCenter",StableWind.Instance._WindCenter);
        VFX.SetVector3("_DivisionSize", StableWind.Instance._DivisionSize);
        VFX.SetBool("Init",true);
    }

    void Update()
    {
        VFX.SetVector3("_WindCenter",StableWind.Instance._WindCenter);
    }

}
