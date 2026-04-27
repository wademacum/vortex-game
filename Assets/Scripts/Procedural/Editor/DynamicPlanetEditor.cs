#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Vortex.Procedural.Editor
{
    [CustomEditor(typeof(DynamicPlanet))]
    public sealed class DynamicPlanetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            DynamicPlanet planet = (DynamicPlanet)target;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Template Actions", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Selected template'in body type'ina uygun shape/noise degerlerini yeniden uretir.");

                if (GUILayout.Button("Random Shape"))
                {
                    Undo.RecordObject(planet, "Randomize Dynamic Planet Template");
                    if (planet.RandomizeSelectedTemplateShape())
                    {
                        EditorUtility.SetDirty(planet);
                    }
                }
            }
        }
    }
}
#endif
