using UnityEngine;

namespace Vortex.Ship
{
    [DisallowMultipleComponent]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -14f);
        [SerializeField, Min(0.01f)] private float positionLerpSpeed = 8f;
        [SerializeField, Min(0.01f)] private float rotationLerpSpeed = 6f;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.TransformPoint(offset);
            float posT = 1f - Mathf.Exp(-positionLerpSpeed * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, posT);

            Vector3 lookPoint = target.position;
            Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            float rotT = 1f - Mathf.Exp(-rotationLerpSpeed * Time.unscaledDeltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotT);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
