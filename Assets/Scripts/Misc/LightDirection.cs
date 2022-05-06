using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LightDirection : MonoBehaviour
{
    void Update()
    {
        Shader.SetGlobalVector("_SunDirection", transform.forward);
    }
}
