using System;
using System.Numerics;
using UnityEngine.Audio;
using UnityEngine.UI;

[Serializable]
public class CharacterData
{
    public Image characterSpriteFolderPath;
    public string name;
    public CharacterEmotion characterEmotion;

    public bool isVisible;

    public float characterAppearanceDelay;
    public bool smoothMove;

    public bool isTalking;
    public bool hideName; // Will be displayed as "", "???" or "..." if true

    public Vector2 characterPosition;
    public float characterRotation;
    public Vector2 characterScale;
}

public enum CharacterEmotion
{
    Normal,
    Angry,
    Happy,
    Sad,
}