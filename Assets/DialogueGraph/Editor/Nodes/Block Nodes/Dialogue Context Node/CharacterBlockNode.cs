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
    public const string ChangePositionPortName = "change position";

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
        context.AddOption<bool>(ChangePositionPortName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var changePosition = GetBoolOption(ChangePositionPortName);

        context.AddInputPort<Sprite>(CharacterSpritePortName).Build();
        context.AddInputPort<string>(CharacterNamePortName).Build();
        context.AddInputPort<CharacterEmotion>(EmotionPortName).Build();

        context.AddInputPort<bool>(VisiblePortName).Build();

        context.AddInputPort<float>(AppearanceDelayPortName).Build();
        context.AddInputPort<bool>(IsTalkingPortName).Build();
        context.AddInputPort<bool>(HideNamePortName).Build();

        if (!changePosition)
            return;

        context.AddInputPort<bool>(SmoothMovementPortName).Build();
        context.AddInputPort<Vector2>(PositionPortName).Build();
        context.AddInputPort<float>(RotationPortName).Build();
        context.AddInputPort<Vector2>(ScalePortName).Build();
    }

    private bool GetBoolOption(string name, bool defaultValue = false)
    {
        var opt = GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<bool>(out var value) ? value : defaultValue;
    }
}
