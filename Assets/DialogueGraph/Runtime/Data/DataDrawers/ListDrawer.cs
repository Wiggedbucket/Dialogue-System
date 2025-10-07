using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(List<>))]
public class ListDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new VisualElement();
        var listProp = property.FindPropertyRelative("list");
        root.Add(new PropertyField(listProp));
        return root;
    }
}
