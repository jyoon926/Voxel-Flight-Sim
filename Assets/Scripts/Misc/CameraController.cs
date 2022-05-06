using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    //Public
        [Header("Rotation")]
        public float _rotationSmoothing;
        public float _sensitivity;
        public Vector2 _verticalClamping;

        [Header("Position")]
        public Transform _player;
        public float _positionSmoothing;
        public float _heightOffset;

    //Private
        private Quaternion _cameraRotation;
    
    private void Start() {
        Cursor.visible = false;
        _cameraRotation = transform.rotation;
    }

    private void Update() {
        Rotate();
        Move();
    }

    private void Rotate() {
        Vector2 input = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        _cameraRotation.y += input.x * _sensitivity;
        _cameraRotation.x += input.y * _sensitivity * -1f;
        _cameraRotation.x = Mathf.Clamp(_cameraRotation.x, _verticalClamping.x, _verticalClamping.y);

        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(_cameraRotation.x, _cameraRotation.y, _cameraRotation.z), _rotationSmoothing * Time.deltaTime);
    }

    private void Move() {
        transform.position = Vector3.Lerp(transform.position, new Vector3(_player.position.x, _player.position.y + _heightOffset, _player.position.z), _positionSmoothing * Time.deltaTime);
    }
}