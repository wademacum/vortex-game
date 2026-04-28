#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Vortex.Procedural.Editor
{
    [CustomEditor(typeof(AsteroidClusterTemplate))]
    public sealed class AsteroidTemplateEditor : UnityEditor.Editor
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
            DrawSection("Base Shape", "baseShapeConfig");
            DrawSection("Asteroid Shape", "asteroidShapeConfig");
            DrawSection("Asteroid Shading", "asteroidShadingConfig");
            DrawSection("Moon-like Surface", "moonBiomeConfig", "moonSurfaceConfig", "biomeColorCurves", "emissiveRange");

            serializedObject.ApplyModifiedProperties();

            AsteroidClusterTemplate template = (AsteroidClusterTemplate)target;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Asteroid Presets", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Moon texture kalir, sekil tarafi hizli presetlerle ayarlanir.");

                if (GUILayout.Button("Chunky"))
                {
                    Undo.RecordObject(template, "Asteroid Preset Chunky");
                    ApplyChunkyPreset(template);
                    template.ApplyAuthoringDefaults();
                    EditorUtility.SetDirty(template);
                }

                if (GUILayout.Button("Jagged"))
                {
                    Undo.RecordObject(template, "Asteroid Preset Jagged");
                    ApplyJaggedPreset(template);
                    template.ApplyAuthoringDefaults();
                    EditorUtility.SetDirty(template);
                }

                if (GUILayout.Button("Pitted"))
                {
                    Undo.RecordObject(template, "Asteroid Preset Pitted");
                    ApplyPittedPreset(template);
                    template.ApplyAuthoringDefaults();
                    EditorUtility.SetDirty(template);
                }

                if (GUILayout.Button("Randomize Asteroid"))
                {
                    Undo.RecordObject(template, "Randomize Asteroid Template");
                    CelestialBodyTemplate.Randomize(template, System.Environment.TickCount);
                    template.ApplyAuthoringDefaults();
                    EditorUtility.SetDirty(template);
                }

                if (GUILayout.Button("Apply Defaults"))
                {
                    Undo.RecordObject(template, "Apply Asteroid Defaults");
                    template.ApplyAuthoringDefaults();
                    EditorUtility.SetDirty(template);
                }
            }
        }

        private static void ApplyChunkyPreset(AsteroidClusterTemplate t)
        {
            t.baseShapeConfig.verticalSquash = 0.9f;
            t.asteroidShapeConfig.surfaceIrregularity = 0.42f;
            t.asteroidShapeConfig.pitCount = 24;
            t.asteroidShapeConfig.pitDepth = 0.022f;
            t.asteroidShapeConfig.pitRimSharpness = 0.62f;
            t.asteroidShapeConfig.pitRadiusRange = new Vector2(0.03f, 0.14f);
            t.moonSurfaceConfig.textureBlendStrength = 0.52f;
            t.moonSurfaceConfig.steepDarkening = 0.22f;
        }

        private static void ApplyJaggedPreset(AsteroidClusterTemplate t)
        {
            t.baseShapeConfig.verticalSquash = 1.08f;
            t.asteroidShapeConfig.surfaceIrregularity = 0.56f;
            t.asteroidShapeConfig.pitCount = 28;
            t.asteroidShapeConfig.pitDepth = 0.02f;
            t.asteroidShapeConfig.pitRimSharpness = 0.9f;
            t.asteroidShapeConfig.pitRadiusRange = new Vector2(0.018f, 0.09f);
            t.asteroidShapeConfig.detailA.amplitude = Mathf.Max(3.6f, t.asteroidShapeConfig.detailA.amplitude);
            t.moonSurfaceConfig.flatContrast = 1.16f;
            t.moonSurfaceConfig.steepContrast = 1.14f;
        }

        private static void ApplyPittedPreset(AsteroidClusterTemplate t)
        {
            t.baseShapeConfig.verticalSquash = 0.95f;
            t.asteroidShapeConfig.surfaceIrregularity = 0.34f;
            t.asteroidShapeConfig.pitCount = 72;
            t.asteroidShapeConfig.pitDepth = 0.036f;
            t.asteroidShapeConfig.pitRimSharpness = 0.7f;
            t.asteroidShapeConfig.pitRadiusRange = new Vector2(0.016f, 0.11f);
            t.moonSurfaceConfig.ejectaBrightness = 0.06f;
            t.moonSurfaceConfig.microDetailStrength = 0.46f;
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
