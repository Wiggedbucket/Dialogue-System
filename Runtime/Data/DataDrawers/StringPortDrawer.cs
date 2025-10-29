using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(DialogueData))]
public class DialogueTextDrawer : PropertyDrawer
{
    private float lineHeight = 15f;

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var textProp = property.FindPropertyRelative("text");
        var textArea = new TextField
        {
            multiline = true,
        };

        // Style the main field container
        textArea.style.minHeight = 120;
        textArea.style.minWidth = 400;
        textArea.style.whiteSpace = WhiteSpace.Normal;

        // Let the text input expand too
        textArea.Q("unity-text-input").style.flexGrow = 1;
        textArea.Q("unity-text-input").style.height = new Length(100, LengthUnit.Percent);

        textArea.RegisterCallback<ChangeEvent<string>>(evt =>
        {
            var lineCount = evt.newValue.Split('\n').Length;
            textArea.style.height = Mathf.Max(120, lineCount * lineHeight);
        });

        textArea.bindingPath = textProp.propertyPath;
        textArea.Bind(property.serializedObject);
        return textArea;
    }
}

[Serializable]
public class DialogueData
{
    public string text;
}