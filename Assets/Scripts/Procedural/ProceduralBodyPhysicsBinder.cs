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

            StructuralResponseBody structural = target.GetComponent<StructuralResponseBody>();
            if (structural == null)
            {
                structural = target.AddComponent<StructuralResponseBody>();
            }

            bool canNova = data.bodyClass == BodyClass.Star || data.bodyClass == BodyClass.NeutronStar || data.bodyClass == BodyClass.Supergiant;
            structural.ConfigureFromRuntimeData(
                data.corePressureSupport,
                data.fractureThreshold,
                data.collapseThreshold,
                data.novaThreshold,
                data.structuralDamping,
                canNova);

            if (data.enableMeshNodeDeformation)
            {
                MeshNodeDeformer deformer = target.GetComponent<MeshNodeDeformer>();
                if (deformer == null)
                {
                    deformer = target.AddComponent<MeshNodeDeformer>();
                }

                deformer.Configure(
                    data.meshTidalStartThreshold,
                    data.meshTidalMaxThreshold,
                    data.meshAxialStretchAtFull,
                    data.meshRadialSqueezeAtFull);
            }
        }
    }
}
