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
    public PortValue<CharacterEmotion> characterEmotion = new();

    public PortValue<bool> isVisible = new();
    public PortValue<float> characterAppearanceDelay = new();

    public PortValue<bool> isTalking = new();
    public PortValue<bool> hideName = new(); // Will be displayed as "", "???" or "..." if true

    public PortValue<bool> smoothMove = new();
    public PortValue<Vector2> characterPosition = new();
    public PortValue<float> characterRotation = new();
    public PortValue<Vector2> characterScale = new();
}

public enum CharacterEmotion
{
    Neutral,
    Angry,
    Happy,
    Sad,
    Surprised,
    Confused,
    Excited,
    Tired,
    Scared,
    Embarrassed,
    Proud,
    Bored,
    Curious,
    Determined,
    Frustrated,
    Relaxed,
    Shocked,
    Worried,
}