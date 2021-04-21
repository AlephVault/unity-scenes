using UnityEngine;
using UnityEditor;

namespace AlephVault.Unity.Scenes
{
    namespace Authoring
    {
        namespace Types
        {
            [CustomPropertyDrawer(typeof(SceneReference))]
            public sealed class SceneReferencePropertyDrawer : PropertyDrawer
            {
                public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
                {
                    var relative = property.FindPropertyRelative("_asset");

                    var content = EditorGUI.BeginProperty(position, label, relative);

                    EditorGUI.BeginChangeCheck();

                    var source = relative.objectReferenceValue;
                    var target = EditorGUI.ObjectField(position, content, source, typeof(SceneAsset), false);

                    if (EditorGUI.EndChangeCheck())
                        relative.objectReferenceValue = target;

                    EditorGUI.EndProperty();
                }

                public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
                {
                    return EditorGUIUtility.singleLineHeight;
                }
            }
        }
    }
}