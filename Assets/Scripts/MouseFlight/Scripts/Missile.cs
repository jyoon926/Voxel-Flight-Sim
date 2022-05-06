using System;
using UnityEngine;

public class Missile : MonoBehaviour {
    private Rigidbody _rb;
    World world;
    AimPosition aimPosition;
    public float _speed = 15;
    public float _rotateSpeed = 95;
    public LayerMask collisionLayer;
    public Transform meshes;

    public void Init(World world, AimPosition aimPosition) {
        _rb = GetComponent<Rigidbody>();
        this.world = world;
        this.aimPosition = aimPosition;
    }

    private void FixedUpdate() {
        _rb.velocity = transform.forward * _speed;
        RotateRocket();
        meshes.localRotation = Quaternion.Euler(0, 0, meshes.localEulerAngles.z + 2f);
    }

    private void RotateRocket() {
        Vector3 cursorPosition = aimPosition.cursorPosition;
        Vector3 target = Camera.main.ScreenToWorldPoint(new Vector3(cursorPosition.x, cursorPosition.y, 1000f));
        var heading = target - transform.position;
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(cursorPosition), out hit, Mathf.Infinity, collisionLayer)) {
            heading = hit.point - transform.position;
        }
        var rotation = Quaternion.LookRotation(heading);
        _rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, rotation, _rotateSpeed * Time.deltaTime));
    }

    private void OnCollisionEnter(Collision collision) {
        world.Build(transform.position, 0);
        Destroy(gameObject);
    }
}