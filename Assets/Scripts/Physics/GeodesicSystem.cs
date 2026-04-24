using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Physics
{
    public sealed class GeodesicSystem : MonoBehaviour
    {
        [SerializeField] private bool autoCollectBodies = true;
        [SerializeField] private List<RelativisticBody> bodies = new List<RelativisticBody>();
        [SerializeField] private bool pauseSimulationWhenUnfocused = true;
        [SerializeField] private bool pauseSimulationWhenUnfocusedInEditor = false;
        [SerializeField, Min(0)] private int settleFixedStepsAfterFocusReturn = 2;

        private bool hasFocus = true;
        private int focusSettleStepsRemaining;

        private void Start()
        {
            if (autoCollectBodies)
            {
                RefreshBodies();
            }
        }

        private void FixedUpdate()
        {
            if (ShouldPauseForFocusLoss())
            {
                return;
            }

            if (focusSettleStepsRemaining > 0)
            {
                focusSettleStepsRemaining--;
                return;
            }

            if (autoCollectBodies)
            {
                RemoveNullBodies();
            }

            IReadOnlyList<GravityWell> wells = GravityWellRegistry.GetAll();
            float dt = Time.fixedUnscaledDeltaTime;

            for (int i = 0; i < bodies.Count; i++)
            {
                RelativisticBody body = bodies[i];
                if (body == null)
                {
                    continue;
                }

                GeodesicIntegrator.Integrate(body, wells, bodies, dt);
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            hasFocus = focus;
            if (focus)
            {
                focusSettleStepsRemaining = Mathf.Max(0, settleFixedStepsAfterFocusReturn);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            hasFocus = !paused;
            if (!paused)
            {
                focusSettleStepsRemaining = Mathf.Max(0, settleFixedStepsAfterFocusReturn);
            }
        }

        private bool ShouldPauseForFocusLoss()
        {
            if (!pauseSimulationWhenUnfocused || hasFocus)
            {
                return false;
            }

            if (Application.isEditor && !pauseSimulationWhenUnfocusedInEditor)
            {
                return false;
            }

            return true;
        }

        public void RefreshBodies()
        {
            bodies.Clear();
            RelativisticBody[] foundBodies = FindObjectsByType<RelativisticBody>(FindObjectsSortMode.None);
            for (int i = 0; i < foundBodies.Length; i++)
            {
                bodies.Add(foundBodies[i]);
            }
        }

        public void RegisterBody(RelativisticBody body)
        {
            if (body == null || bodies.Contains(body))
            {
                return;
            }

            bodies.Add(body);
        }

        public void UnregisterBody(RelativisticBody body)
        {
            if (body == null)
            {
                return;
            }

            bodies.Remove(body);
        }

        private void RemoveNullBodies()
        {
            for (int i = bodies.Count - 1; i >= 0; i--)
            {
                if (bodies[i] == null)
                {
                    bodies.RemoveAt(i);
                }
            }
        }
    }
}
