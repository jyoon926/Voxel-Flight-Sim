using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    
    public Light light;

    private void Start() {
        StartCoroutine(Off());
    }

    IEnumerator Off() {
        float intensity = 3f;
        while (intensity > 0) {
            yield return 0;
            intensity -= 0.1f;
            light.intensity = intensity;
        }
        light.intensity = 0;
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }
}
