using Vortex.Physics;
using UnityEngine;

namespace Vortex.Procedural
{
    public static class ProceduralBodyPhysicsBinder
    {
        public static void Apply(GameObject target, RuntimeBodyData data)
        {
            if (target == null)
            {
                return;
            }

            GravityWell well = target.GetComponent<GravityWell>();
            if (well != null)
            {
                well.ApplyProceduralBody(data.mass, data.radius);
            }

            bool allowComponentCreation = Application.isPlaying;

            RelativisticBody relativisticBody = target.GetComponent<RelativisticBody>();
            if (relativisticBody == null && allowComponentCreation)
            {
                relativisticBody = target.AddComponent<RelativisticBody>();
            }

            if (relativisticBody != null)
            {
                relativisticBody.ConfigureIntrinsicSpin(data.rotationSpeed, Vector3.up);
            }

            StructuralResponseBody structural = target.GetComponent<StructuralResponseBody>();
            if (structural == null)
            {
                if (allowComponentCreation)
                {
                    structural = target.AddComponent<StructuralResponseBody>();
                }
            }

            if (structural != null)
            {
                bool canNova = data.bodyClass == BodyClass.Star || data.bodyClass == BodyClass.NeutronStar || data.bodyClass == BodyClass.Supergiant;
                structural.ConfigureFromRuntimeData(
                    data.corePressureSupport,
                    data.fractureThreshold,
                    data.collapseThreshold,
                    data.novaThreshold,
                    data.structuralDamping,
                    canNova);
            }

            if (data.enableMeshNodeDeformation)
            {
                MeshNodeDeformer deformer = target.GetComponent<MeshNodeDeformer>();
                if (deformer == null)
                {
                    if (allowComponentCreation)
                    {
                        deformer = target.AddComponent<MeshNodeDeformer>();
                    }
                }

                if (deformer != null)
                {
                    deformer.Configure(
                        data.meshTidalStartThreshold,
                        data.meshTidalMaxThreshold,
                        data.meshAxialStretchAtFull,
                        data.meshRadialSqueezeAtFull);
                }
            }
        }
    }
}
