using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(CharacterData))]
public class CharacterDataDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        return GenericDataDrawer.Draw(property);
    }
}

[Serializable]
public class CharacterData
{
    public PortValue<string> name = new();

    public PortValue<Sprite> characterSprite = new();

    public PortValue<bool> isVisible = new();
    public PortValue<float> characterAppearanceDelay = new();

    public PortValue<bool> isTalking = new();
    public PortValue<bool> hideName = new();

    public PortValue<bool> smoothMove = new();
    public PortValue<Vector2> characterPosition = new();
    public PortValue<float> characterRotation = new();
    public PortValue<Vector2> characterScale = new();
}