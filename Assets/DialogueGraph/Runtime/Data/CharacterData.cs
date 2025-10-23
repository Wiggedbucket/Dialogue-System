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
    public PortValue<float> characterAppearanceDelay = new(); // TODO

    public PortValue<bool> isTalking = new();
    public PortValue<bool> hideName = new();

    public PortValue<bool> preserveAspect = new();

    public PortValue<float> transitionDuration = new();
    public PortValue<MovementType> positionMovementType = new();
    public PortValue<MovementType> rotationMovementType = new();
    public PortValue<MovementType> scaleMovementType = new();

    public PortValue<PredefinedPosition> predefinedPosition = new();
    public PortValue<Vector2> characterPosition = new();

    public PortValue<Vector2> minAnchor = new();
    public PortValue<Vector2> maxAnchor = new();
    public PortValue<Vector2> pivot = new();
    public PortValue<float> characterRotation = new();

    public PortValue<Vector2> widthAndHeight = new();
    public PortValue<Vector2> characterScale = new()
    {
        value = new(1f, 1f),
    };
}