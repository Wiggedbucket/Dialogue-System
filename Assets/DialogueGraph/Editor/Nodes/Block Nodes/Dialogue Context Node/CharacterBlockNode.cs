using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.UI;

[UseWithContext(typeof(DialogueContextNode))]
[Serializable]
public class CharacterBlockNode : BlockNode
{
    public const string ChangeSpritePortName = "change sprite";
    public const string ChangeEmotionPortName = "change emotion";
    public const string ChangeVisibilityPortName = "change visibility";
    public const string ChangeAppearanceDelayPortName = "change appearance delay";
    public const string ChangeIsTalkingPortName = "change is talking";
    public const string ChangeHideNamePortName = "change hide name";
    public const string ChangeSmoothMovementPortName = "change smooth movement";
    public const string ChangePositionPortName = "change position value";
    public const string ChangeRotationPortName = "change rotation";
    public const string ChangeScalePortName = "change scale";

    public const string CharacterSpritePortName = "character sprite";
    public const string CharacterNamePortName = "character name";
    public const string EmotionPortName = "emotion";
    public const string VisiblePortName = "visible";
    public const string AppearanceDelayPortName = "appearance delay";
    public const string IsTalkingPortName = "is talking";
    public const string HideNamePortName = "hide name";
    public const string SmoothMovementPortName = "smooth movement";
    public const string PositionPortName = "position";
    public const string RotationPortName = "rotation";
    public const string ScalePortName = "scale";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(ChangeSpritePortName);
        context.AddOption<bool>(ChangeEmotionPortName);
        context.AddOption<bool>(ChangeVisibilityPortName);
        context.AddOption<bool>(ChangeAppearanceDelayPortName);
        context.AddOption<bool>(ChangeIsTalkingPortName);
        context.AddOption<bool>(ChangeHideNamePortName);
        context.AddOption<bool>(ChangeSmoothMovementPortName);
        context.AddOption<bool>(ChangePositionPortName);
        context.AddOption<bool>(ChangeRotationPortName);
        context.AddOption<bool>(ChangeScalePortName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var changeSprite = GetBoolOption(ChangeSpritePortName);
        var changeEmotion = GetBoolOption(ChangeEmotionPortName);
        var changeVisibility = GetBoolOption(ChangeVisibilityPortName);
        var changeAppearanceDelay = GetBoolOption(ChangeAppearanceDelayPortName);
        var changeIsTalking = GetBoolOption(ChangeIsTalkingPortName);
        var changeHideName = GetBoolOption(ChangeHideNamePortName);
        var changeSmoothMovement = GetBoolOption(ChangeSmoothMovementPortName);
        var changePosition = GetBoolOption(ChangePositionPortName);
        var changeRotation = GetBoolOption(ChangeRotationPortName);
        var changeScale = GetBoolOption(ChangeScalePortName);

        context.AddInputPort<string>(CharacterNamePortName).Build();

        if (changeSprite)
            context.AddInputPort<Sprite>(CharacterSpritePortName).Build();
        if (changeEmotion)
            context.AddInputPort<CharacterEmotion>(EmotionPortName).Build();

        if (changeVisibility)
            context.AddInputPort<bool>(VisiblePortName).Build();

        if (changeAppearanceDelay)
            context.AddInputPort<float>(AppearanceDelayPortName).Build();
        if (changeIsTalking)
            context.AddInputPort<bool>(IsTalkingPortName).Build();
        if (changeHideName)
            context.AddInputPort<bool>(HideNamePortName).Build();

        if (changeSmoothMovement)
            context.AddInputPort<bool>(SmoothMovementPortName).Build();
        if (changePosition)
            context.AddInputPort<Vector2>(PositionPortName).Build();
        if (changeRotation)
            context.AddInputPort<float>(RotationPortName).Build();
        if (changeScale)
            context.AddInputPort<Vector2>(ScalePortName).Build();
    }

    private bool GetBoolOption(string name, bool defaultValue = false)
    {
        var opt = GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<bool>(out var value) ? value : defaultValue;
    }
}
