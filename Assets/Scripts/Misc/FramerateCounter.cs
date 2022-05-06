using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FramerateCounter : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _fpsText;
    [SerializeField] private float _frames;
    float _cumulative = 0;
    int _counter = 1;
 
    private void Update() {
        int fps = (int)(1f / Time.unscaledDeltaTime);
        _cumulative += fps;
        if (_counter == _frames) {
            _fpsText.text = (int)(_cumulative / _frames) + " fps";
            _cumulative = 0;
            _counter = 1;
        } else {
            _counter++;
        }
    }
}
