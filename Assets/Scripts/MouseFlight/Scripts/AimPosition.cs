using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimPosition : MonoBehaviour {
    public MFlight.MouseFlightController flightController;
    public float smoothSpeed;
    [HideInInspector] public Vector3 cursorPosition;
    
    void Update()
    {
        cursorPosition = Camera.main.WorldToScreenPoint(flightController.MouseAimPos);
        cursorPosition = Vector3.Lerp(GetComponent<RectTransform>().anchoredPosition, cursorPosition, smoothSpeed * Time.deltaTime);
        GetComponent<RectTransform>().anchoredPosition = cursorPosition;
    }
}
