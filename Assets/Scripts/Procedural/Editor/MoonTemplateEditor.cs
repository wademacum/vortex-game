#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Vortex.Procedural.Editor
{
    [CustomEditor(typeof(MoonTemplate))]
    public sealed class MoonTemplateEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawProperty("bodyClass");
            DrawProperty("spawnWeight");
            EditorGUILayout.Space(4f);
            DrawSection("Physical Ranges", "massRange", "radiusRange", "densityRange", "rotationRange", "temperatureRange", "albedoRange");
            DrawSection("Gameplay", "generationMode", "hasSurface", "hasAtmosphere", "hasEventHorizon", "supportsLanding", "radiationHazard", "anomalyChance");
            DrawSection("Structural Simulation", "corePressureSupport", "fractureThreshold", "collapseThreshold", "novaThreshold", "structuralDamping");
            DrawSection("Mesh Deformation", "enableMeshNodeDeformation", "meshTidalStartThreshold", "meshTidalMaxThreshold", "meshAxialStretchAtFull", "meshRadialSqueezeAtFull");
            DrawSection("Moon Shape", "baseShapeConfig", "moonShapeConfig");
            DrawSection("Moon Shading", "moonShadingConfig", "biomeColorCurves", "emissiveRange");
            DrawSection("Moon Surface", "moonSurfaceConfig");

            serializedObject.ApplyModifiedProperties();

            MoonTemplate template = (MoonTemplate)target;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Moon Authoring", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Sebastian referansina daha yakin crater ridge ve texture kombinasyonlari uretir.");
                if (GUILayout.Button("Randomize Moon"))
                {
                    Undo.RecordObject(template, "Randomize Moon Template");
                    CelestialBodyTemplate.Randomize(template, System.Environment.TickCount);
                    template.ApplyAuthoringDefaults();
                    EditorUtility.SetDirty(template);
                }

                if (GUILayout.Button("Apply Defaults"))
                {
                    Undo.RecordObject(template, "Apply Moon Defaults");
                    template.ApplyAuthoringDefaults();
                    EditorUtility.SetDirty(template);
                }
            }
        }

        private void DrawSection(string title, params string[] properties)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                for (int i = 0; i < properties.Length; i++)
                {
                    DrawProperty(properties[i]);
                }
            }
        }

        private void DrawProperty(string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, true);
            }
        }
    }
}
#endif
