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
    public Sprite characterSprite;
    public string name;
    public CharacterEmotion characterEmotion;

    public bool isVisible;
    public float characterAppearanceDelay;

    public bool isTalking;
    public bool hideName; // Will be displayed as "", "???" or "..." if true

    public bool changePosition;
    public bool smoothMove;
    public Vector2 characterPosition;
    public float characterRotation;
    public Vector2 characterScale;
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