using Cinemachine;
using UnityEngine;

namespace Vortex.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class CinemachineMiddleMouseOrbit : MonoBehaviour
    {
        [SerializeField] private CinemachineFreeLook freeLook;
        [SerializeField, Min(0.1f)] private float distance = 8f;
        [SerializeField] private float topHeight = 3.0f;
        [SerializeField] private float middleHeight = 1.7f;
        [SerializeField] private float bottomHeight = 0.5f;
        [SerializeField, Min(1f)] private float xSensitivity = 180f;
        [SerializeField, Min(0.1f)] private float ySensitivity = 1.2f;
        [SerializeField] private bool invertY = false;

        private void Reset()
        {
            freeLook = GetComponent<CinemachineFreeLook>();
        }

        private void Awake()
        {
            if (freeLook == null)
            {
                freeLook = GetComponent<CinemachineFreeLook>();
            }

            if (freeLook == null)
            {
                enabled = false;
                return;
            }

            // Disable built-in axis names to prevent drift from legacy axes/devices.
            freeLook.m_XAxis.m_InputAxisName = string.Empty;
            freeLook.m_YAxis.m_InputAxisName = string.Empty;
            freeLook.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;

            ApplyOrbitPreset();
        }

        private void OnValidate()
        {
            if (freeLook == null)
            {
                freeLook = GetComponent<CinemachineFreeLook>();
            }

            if (freeLook != null)
            {
                ApplyOrbitPreset();
            }
        }

        private void LateUpdate()
        {
            if (freeLook == null)
            {
                return;
            }

            if (!Input.GetMouseButton(2))
            {
                return;
            }

            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            float mouseX = Input.GetAxisRaw("Mouse X");
            float mouseY = Input.GetAxisRaw("Mouse Y");

            freeLook.m_XAxis.Value += mouseX * xSensitivity * dt;

            float yDelta = mouseY * ySensitivity * dt;
            freeLook.m_YAxis.Value = Mathf.Clamp01(
                freeLook.m_YAxis.Value + (invertY ? yDelta : -yDelta));
        }

        private void ApplyOrbitPreset()
        {
            float clampedDistance = Mathf.Max(0.1f, distance);
            freeLook.m_Orbits[0] = new CinemachineFreeLook.Orbit(topHeight, clampedDistance * 0.95f);
            freeLook.m_Orbits[1] = new CinemachineFreeLook.Orbit(middleHeight, clampedDistance);
            freeLook.m_Orbits[2] = new CinemachineFreeLook.Orbit(bottomHeight, clampedDistance * 0.9f);
        }
    }
}
