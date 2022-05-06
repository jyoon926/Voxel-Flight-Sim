using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class AutoFocus : MonoBehaviour
{
    public PostProcessLayer v2_PostProcess;
    public LayerMask layers;
    DepthOfField dph;

    private void Start() {
        List<PostProcessVolume> volList = new List<PostProcessVolume>();
        PostProcessManager.instance.GetActiveVolumes(v2_PostProcess, volList, true, true);
        foreach (PostProcessVolume vol in volList) {
            PostProcessProfile ppp = vol.profile;
            if (ppp) {
                if (!ppp.TryGetSettings<DepthOfField>(out dph)) {
                    throw new System.NullReferenceException(nameof(dph));
                }
            }
        }
    }

    void Update()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layers)) {
            dph.focusDistance.Override(Mathf.Lerp(dph.focusDistance.value, hit.distance, Time.deltaTime * 10f));
        } else {
            dph.focusDistance.Override(Mathf.Lerp(dph.focusDistance.value, 500f, Time.deltaTime * 10f));
        }
    }
}
