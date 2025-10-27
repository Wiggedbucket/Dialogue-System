using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class DialogueSettings
{
    public bool awaitContinueEvent = false;
    public bool delayNextWithClick = false;
    public bool keepPreviousText = false;

    public PortValue<float> printSpeed = new();
    public PortValue<float> delayText = new();
    public PortValue<string> broadcastString = new();

    public PortValue<bool> bold = new();
    public PortValue<bool> italic = new();
    public PortValue<bool> underline = new();
    public PortValue<Color> color = new();
    public PortValue<TMP_FontAsset> font = new();
    public PortValue<TextAlignmentOptions> textAlign = new();
    public PortValue<TextWrappingModes> wrapText = new();

    public PortValue<List<AudioClip>> musicQueue = new();
    public PortValue<bool> loop = new();
    public PortValue<bool> shuffle = new();
    public PortValue<List<AudioClip>> audioList = new();

    public PortValue<Color> dialogueBoxColor = new();
    public PortValue<Sprite> dialogueBoxImage = new();
    public PortValue<Color> namePlateColor = new();
    public PortValue<Sprite> namePlateImage = new();

    public PortValue<Sprite> backgroundImage = new();
    public PortValue<BackgroundTransition> backgroundTransition = new();
}
