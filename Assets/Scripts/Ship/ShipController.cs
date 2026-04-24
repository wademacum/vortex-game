using UnityEngine;
using Vortex.Physics;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vortex.Ship
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RelativisticBody))]
    public sealed class ShipController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float thrustForce = 16f;
        [SerializeField, Min(0f)] private float strafeForce = 14f;
        [SerializeField, Min(0f)] private float verticalForce = 12f;

        private RelativisticBody body;

        private void Awake()
        {
            body = GetComponent<RelativisticBody>();
        }

        private void FixedUpdate()
        {
            if (body == null || body.IsTimeFrozen)
            {
                return;
            }

            Vector3 input = ReadInputVector();
            if (input.sqrMagnitude <= 0f)
            {
                return;
            }

            Vector3 worldThrust =
                transform.forward * (input.z * thrustForce) +
                transform.right * (input.x * strafeForce) +
                transform.up * (input.y * verticalForce);

            Vector3 deltaV = worldThrust * Time.fixedUnscaledDeltaTime;
            Vector4 fv = body.FourVelocity;
            fv.x += deltaV.x;
            fv.y += deltaV.y;
            fv.z += deltaV.z;
            body.FourVelocity = fv;
        }

        private static Vector3 ReadInputVector()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null)
            {
                return Vector3.zero;
            }

            float x = 0f;
            float y = 0f;
            float z = 0f;

            if (kb.aKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed) x += 1f;
            if (kb.leftShiftKey.isPressed) y -= 1f;
            if (kb.spaceKey.isPressed) y += 1f;
            if (kb.sKey.isPressed) z -= 1f;
            if (kb.wKey.isPressed) z += 1f;

            return Vector3.ClampMagnitude(new Vector3(x, y, z), 1f);
#else
            float x = 0f;
            float y = 0f;
            float z = 0f;

            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;
            if (Input.GetKey(KeyCode.LeftShift)) y -= 1f;
            if (Input.GetKey(KeyCode.Space)) y += 1f;
            if (Input.GetKey(KeyCode.S)) z -= 1f;
            if (Input.GetKey(KeyCode.W)) z += 1f;

            return Vector3.ClampMagnitude(new Vector3(x, y, z), 1f);
#endif
        }
    }
}
