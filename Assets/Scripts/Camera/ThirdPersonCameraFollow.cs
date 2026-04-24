using UnityEngine;

namespace Vortex.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class ThirdPersonCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(1f)] private float distance = 8f;
        [SerializeField, Min(0f)] private float height = 2.5f;
        [SerializeField] private float yaw = 0f;
        [SerializeField] private float pitch = 20f;
        [SerializeField, Min(1f)] private float minPitch = -10f;
        [SerializeField, Min(1f)] private float maxPitch = 75f;
        [SerializeField, Min(0f)] private float orbitSensitivity = 180f;
        [SerializeField, Min(0f)] private float followSmoothTime = 0.08f;
        [SerializeField] private bool keepWorldUp = true;

        private Vector3 followVelocity;

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (target == null)
            {
                return;
            }

            if (Input.GetMouseButton(2))
            {
                float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
                yaw += Input.GetAxis("Mouse X") * orbitSensitivity * dt;
                pitch -= Input.GetAxis("Mouse Y") * orbitSensitivity * dt;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            Vector3 focusPoint = target.position + Vector3.up * height;
            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredOffset = orbitRotation * new Vector3(0f, 0f, -distance);

            Vector3 desiredPosition = focusPoint + desiredOffset;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref followVelocity,
                Mathf.Max(0.0001f, followSmoothTime));

            Vector3 lookDirection = focusPoint - transform.position;
            if (lookDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 up = keepWorldUp ? Vector3.up : target.up;
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, up);
            transform.rotation = desiredRotation;
        }
    }
}
