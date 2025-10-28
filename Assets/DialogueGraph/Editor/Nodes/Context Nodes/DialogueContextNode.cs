using System;
using System.Collections.Generic;
using TMPro;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class DialogueContextNode : ContextNode
{
    public const string DelayTypePortName = "delay type next node";
    public const string KeepPreviousTextPortName = "keep previous text";

    public const string ChangePrintSpeedPortName = "change print speed";
    public const string ActivateBroadcastStringPortName = "activate broadcast string";
    public const string ChangeBoldPortName = "change bold";
    public const string ChangeItalicPortName = "change italic";
    public const string ChangeUnderlinePortName = "change underline";
    public const string ChangeTextColorPortName = "change text color";
    public const string ChangeFontPortName = "change font";
    public const string ChangeTextAlignPortName = "change text align";
    public const string ChangeWrapTextPortName = "change wrap text";
    public const string ChangeMusicAudioQueuePortName = "change music audio queue";
    public const string SetPlayAudioPortName = "set play audio";
    public const string SetDialogueBoxColorPortName = "set dialogue box color";
    public const string SetDialogueBoxImagePortName = "set dialogue box image";
    public const string SetNamePlateColorPortName = "set name plate color";
    public const string SetNamePlateImagePortName = "set name plate image";
    public const string ChangeBackgroundImagePortName = "change background image";
    public const string ChangeBackgroundTransitionPortName = "change background transition";
    public const string CHangeBackgroundTransitionDurationPortName = "change background transition duration";

    public const string DialoguePortName = "dialogue";

    public const string PrintSpeedPortName = "print speed";
    public const string DelayTextPortName = "delay text";
    public const string BroadcastStringPortName = "broadcast string";

    public const string BoldPortName = "bold";
    public const string ItalicPortName = "italic";
    public const string UnderlinePortName = "underline";
    public const string ColorPortName = "color";
    public const string FontPortName = "font";
    public const string TextAlignPortName = "text align";
    public const string WrapTextPortName = "wrap text";

    public const string MusicAudioQueuePortName = "music audio queue";
    public const string LoopMusicPortName = "loop music";
    public const string ShuffleMusicPortName = "shuffle music";
    public const string PlayAudioPortName = "play audio";

    public const string DialogueBoxColorPortName = "dialogue box color";
    public const string DialogueBoxImagePortName = "dialogue box image";
    public const string NamePlateColorPortName = "name plate color";
    public const string NamePlateImagePortName = "name plate image";

    public const string BackgroundImagePortName = "background image";
    public const string BackgroundTransitionPortName = "smooth background transition";
    public const string BackgroundTransitionDurationPortName = "background transition duration";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<DelayType>(DelayTypePortName)
            .WithDefaultValue(DelayType.Click)
            .Build();
        context.AddOption<bool>(KeepPreviousTextPortName);

        context.AddOption<bool>(ChangePrintSpeedPortName);

        context.AddOption<bool>(ActivateBroadcastStringPortName);
        context.AddOption<bool>(ChangeBoldPortName);
        context.AddOption<bool>(ChangeItalicPortName);
        context.AddOption<bool>(ChangeUnderlinePortName);
        context.AddOption<bool>(ChangeTextColorPortName);
        context.AddOption<bool>(ChangeFontPortName);
        context.AddOption<bool>(ChangeTextAlignPortName);
        context.AddOption<bool>(ChangeWrapTextPortName);

        context.AddOption<bool>(ChangeMusicAudioQueuePortName);
        context.AddOption<bool>(SetPlayAudioPortName);

        context.AddOption<bool>(SetDialogueBoxColorPortName);
        context.AddOption<bool>(SetDialogueBoxImagePortName);
        context.AddOption<bool>(SetNamePlateColorPortName);
        context.AddOption<bool>(SetNamePlateImagePortName);

        context.AddOption<bool>(ChangeBackgroundImagePortName);
        context.AddOption<bool>(ChangeBackgroundTransitionPortName);
        context.AddOption<bool>(CHangeBackgroundTransitionDurationPortName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();

        var changePrintSpeed = GetBoolOption(ChangePrintSpeedPortName);
        var activateBroadcastString = GetBoolOption(ActivateBroadcastStringPortName);
        var changeBold = GetBoolOption(ChangeBoldPortName);
        var changeItalic = GetBoolOption(ChangeItalicPortName);
        var changeUnderline = GetBoolOption(ChangeUnderlinePortName);
        var changeTextColor = GetBoolOption(ChangeTextColorPortName);
        var changeFont = GetBoolOption(ChangeFontPortName);
        var changeTextAlign = GetBoolOption(ChangeTextAlignPortName);
        var changeWrapText = GetBoolOption(ChangeWrapTextPortName);
        var changeMusicAudioQueue = GetBoolOption(ChangeMusicAudioQueuePortName);
        var setPlayAudio = GetBoolOption(SetPlayAudioPortName);
        var setDialogueBoxColor = GetBoolOption(SetDialogueBoxColorPortName);
        var setDialogueBoxImage = GetBoolOption(SetDialogueBoxImagePortName);
        var setNamePlateColor = GetBoolOption(SetNamePlateColorPortName);
        var setNamePlateImage = GetBoolOption(SetNamePlateImagePortName);
        var changeBackgroundImage = GetBoolOption(ChangeBackgroundImagePortName);
        var changeBackgroundTransition = GetBoolOption(ChangeBackgroundTransitionPortName);
        var changeBackgroundTransitionDuration = GetBoolOption(CHangeBackgroundTransitionDurationPortName);

        context.AddOutputPort("out")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();

        context.AddInputPort<DialogueData>(DialoguePortName).Build();

        if (activateBroadcastString)
            context.AddInputPort<string>(BroadcastStringPortName).Build();

        if (changePrintSpeed)
            context.AddInputPort<float>(PrintSpeedPortName)
            .WithDefaultValue(0.02f)
            .Build();
        context.AddInputPort<float>(DelayTextPortName).Build();

        if (changeBold)
            context.AddInputPort<bool>(BoldPortName).Build();
        if (changeItalic)
            context.AddInputPort<bool>(ItalicPortName).Build();
        if (changeUnderline)
            context.AddInputPort<bool>(UnderlinePortName).Build();

        if (changeTextColor)
            context.AddInputPort<Color>(ColorPortName)
                .WithDefaultValue(Color.black)
                .Build();

        if (changeFont)
            context.AddInputPort<TMP_FontAsset>(FontPortName).Build();
        if (changeTextAlign)
            context.AddInputPort<TextAlignmentOptions>(TextAlignPortName)
                .WithDefaultValue(TextAlignmentOptions.TopLeft)
                .Build();
        if (changeWrapText)
            context.AddInputPort<TextWrappingModes>(WrapTextPortName)
                .WithDefaultValue(TextWrappingModes.Normal)
                .Build();

        if (changeMusicAudioQueue)
        {
            context.AddInputPort<List<AudioClip>>(MusicAudioQueuePortName).Build();
            context.AddInputPort<bool>(LoopMusicPortName).WithDefaultValue(true).Build();
            context.AddInputPort<bool>(ShuffleMusicPortName).Build();
        }
        if (setPlayAudio)
            context.AddInputPort<List<AudioClip>>(PlayAudioPortName).Build();

        if (setDialogueBoxColor)
            context.AddInputPort<Color>(DialogueBoxColorPortName).Build();
        if (setDialogueBoxImage)
            context.AddInputPort<Sprite>(DialogueBoxImagePortName).Build();
        
        if (setNamePlateColor)
            context.AddInputPort<Color>(NamePlateColorPortName).Build();
        if (setNamePlateImage)
            context.AddInputPort<Sprite>(NamePlateImagePortName).Build();

        if (changeBackgroundImage)
            context.AddInputPort<Sprite>(BackgroundImagePortName).Build();
        if (changeBackgroundTransition)
            context.AddInputPort<BackgroundTransition>(BackgroundTransitionPortName).Build();
        if (changeBackgroundTransitionDuration)
            context.AddInputPort<float>(BackgroundTransitionDurationPortName).WithDefaultValue(0.5f).Build();
    }

    private bool GetBoolOption(string name, bool defaultValue = false)
    {
        var opt = GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<bool>(out var value) ? value : defaultValue;
    }
}
