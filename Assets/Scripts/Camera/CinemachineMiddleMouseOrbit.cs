using Unity.Cinemachine;
using UnityEngine;

namespace Vortex.CameraSystem
{
    /// <summary>
    /// Drives a <see cref="CinemachineOrbitalFollow"/> rig with middle-mouse-button orbit input.
    /// Requires Cinemachine 3.x – no deprecated FreeLook API used.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class CinemachineMiddleMouseOrbit : MonoBehaviour
    {
        [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
        [SerializeField, Min(0.1f)] private float distance = 8f;
        [SerializeField] private float topHeight    = 3.0f;
        [SerializeField] private float middleHeight = 1.7f;
        [SerializeField] private float bottomHeight = 0.5f;
        [SerializeField, Min(1f)]   private float xSensitivity = 180f;
        [SerializeField, Min(0.1f)] private float ySensitivity = 1.2f;
        [SerializeField] private bool invertY = false;

        private void Reset()
        {
            orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        }

        private void Awake()
        {
            if (orbitalFollow == null)
                orbitalFollow = GetComponent<CinemachineOrbitalFollow>();

            if (orbitalFollow == null)
            {
                Debug.LogWarning($"[{nameof(CinemachineMiddleMouseOrbit)}] No CinemachineOrbitalFollow found – disabling.", this);
                enabled = false;
                return;
            }

            // Let this script own all axis input; disable the automatic controller if present.
            var inputController = GetComponent<CinemachineInputAxisController>();
            if (inputController != null)
                inputController.enabled = false;

            ApplyOrbitPreset();
        }

        private void OnValidate()
        {
            if (orbitalFollow == null)
                orbitalFollow = GetComponent<CinemachineOrbitalFollow>();

            if (orbitalFollow != null)
                ApplyOrbitPreset();
        }

        private void LateUpdate()
        {
            if (orbitalFollow == null || !Input.GetMouseButton(2))
                return;

            float dt     = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            float mouseX = Input.GetAxisRaw("Mouse X");
            float mouseY = Input.GetAxisRaw("Mouse Y");

            // Horizontal axis wraps freely – no clamping needed.
            orbitalFollow.HorizontalAxis.Value += mouseX * xSensitivity * dt;

            // Vertical axis is clamped to the axis's own [min, max] range.
            float yDelta = mouseY * ySensitivity * dt;
            float newY   = orbitalFollow.VerticalAxis.Value + (invertY ? yDelta : -yDelta);
            orbitalFollow.VerticalAxis.Value = Mathf.Clamp(
                newY,
                orbitalFollow.VerticalAxis.Range.x,
                orbitalFollow.VerticalAxis.Range.y);
        }

        private void ApplyOrbitPreset()
        {
            orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing;
            float d = Mathf.Max(0.1f, distance);
            orbitalFollow.Orbits = new Cinemachine3OrbitRig.Settings
            {
                Top    = new Cinemachine3OrbitRig.Orbit { Height = topHeight,    Radius = d * 0.95f },
                Center = new Cinemachine3OrbitRig.Orbit { Height = middleHeight, Radius = d        },
                Bottom = new Cinemachine3OrbitRig.Orbit { Height = bottomHeight, Radius = d * 0.9f },
            };
        }
    }
}
