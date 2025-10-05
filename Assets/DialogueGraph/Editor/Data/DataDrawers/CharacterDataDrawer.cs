using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;

[CustomPropertyDrawer(typeof(CharacterData))]
public class CharacterDataDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        // Container for the whole drawer
        var container = new VisualElement();
        container.style.paddingLeft = 4;

        // Foldout to show/hide character data
        var fold = new Foldout
        {
            text = "Character",
            value = true,
        };

        // Create PropertyFields for each field (these will use standard drawers or nested custom drawers)
        fold.Add(new PropertyField(property.FindPropertyRelative(nameof(CharacterData.characterSprite))));
        fold.Add(new PropertyField(property.FindPropertyRelative(nameof(CharacterData.name))));
        fold.Add(new PropertyField(property.FindPropertyRelative(nameof(CharacterData.characterEmotion))));
        fold.Add(new PropertyField(property.FindPropertyRelative(nameof(CharacterData.isVisible))));
        fold.Add(new PropertyField(property.FindPropertyRelative(nameof(CharacterData.characterAppearanceDelay))));
        fold.Add(new PropertyField(property.FindPropertyRelative(nameof(CharacterData.smoothMove))));
        fold.Add(new PropertyField(property.FindPropertyRelative(nameof(CharacterData.isTalking))));
        fold.Add(new PropertyField(property.FindPropertyRelative(nameof(CharacterData.hideName))));
        fold.Add(new PropertyField(property.FindPropertyRelative(nameof(CharacterData.characterPosition))));
        fold.Add(new PropertyField(property.FindPropertyRelative(nameof(CharacterData.characterRotation))));
        fold.Add(new PropertyField(property.FindPropertyRelative(nameof(CharacterData.characterScale))));

        container.Add(fold);

        return container;
    }
}
