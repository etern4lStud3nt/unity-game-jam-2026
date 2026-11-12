using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

// Lets the swing's hit areas be dragged in the Scene view like a collider: select the character (or open its
// prefab) and drag the dots on the edges of the side, up and down boxes. The side box is shown facing right; it is
// mirrored in play when the character faces left. The values are the same Size / Offset fields in the inspector.
[CustomEditor(typeof(PlayerAttack))]
public class PlayerAttackEditor : Editor
{
    private readonly BoxBoundsHandle sideHandle = new();
    private readonly BoxBoundsHandle upHandle = new();
    private readonly BoxBoundsHandle downHandle = new();

    private void OnSceneGUI()
    {
        serializedObject.Update();
        Vector3 origin = ((PlayerAttack)target).transform.position;
        DrawArea(sideHandle, "sideSize", "sideOffset", "Side", new Color(1f, 0.8f, 0.2f), origin);
        DrawArea(upHandle, "upSize", "upOffset", "Up", new Color(0.3f, 0.9f, 1f), origin);
        DrawArea(downHandle, "downSize", "downOffset", "Down", new Color(1f, 0.4f, 0.4f), origin);
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawArea(BoxBoundsHandle handle, string sizeField, string offsetField, string label, Color color,
                          Vector3 origin)
    {
        SerializedProperty size = serializedObject.FindProperty(sizeField);
        SerializedProperty offset = serializedObject.FindProperty(offsetField);

        using (new Handles.DrawingScope(color, Matrix4x4.Translate(origin)))
        {
            handle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
            handle.center = offset.vector2Value;
            handle.size = size.vector2Value;

            EditorGUI.BeginChangeCheck();
            handle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                offset.vector2Value = handle.center;
                size.vector2Value = handle.size;
            }

            Handles.Label(offset.vector2Value + new Vector2(-size.vector2Value.x, size.vector2Value.y) * 0.5f, label);
        }
    }
}
