using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancing.Samples
{
    [RequireComponent(typeof(CharacterController))]
    public class SampleCharacterController : MonoBehaviour
    {
        [Header("Player settings")]
        public float moveSpeed;
        public float cameraHorizontalSpeed;
        public float cameraVerticalSpeed;

        public float cameraMaxPitch;
        public Transform _camera;

        //Private
        private CharacterController _cc;
        private float yaw = 0;
        private float pitch = 0;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

            Camera();
            Movement();

        }

        private void Camera()
        {
            yaw += Input.GetAxis("Mouse X") * cameraHorizontalSpeed;
            pitch += Input.GetAxis("Mouse Y") * cameraVerticalSpeed;
            pitch = Mathf.Clamp(pitch, -cameraMaxPitch, cameraMaxPitch);
            _camera.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        }

        private void Movement()
        {

            Vector3 movement = new Vector3(
                Input.GetAxis("Horizontal"),
                0,
                Input.GetAxis("Vertical"));

            movement = _camera.TransformDirection(movement);
            movement.y = 0;
            movement = movement.normalized;

            if (Input.GetKeyDown(KeyCode.LeftShift))
                moveSpeed *= 2;
            if (Input.GetKeyUp(KeyCode.LeftShift))
                moveSpeed /= 2;

            if (!_cc.isGrounded)
                movement += Physics.gravity;

            _cc.Move(movement * moveSpeed * Time.deltaTime);
        }
    }
}