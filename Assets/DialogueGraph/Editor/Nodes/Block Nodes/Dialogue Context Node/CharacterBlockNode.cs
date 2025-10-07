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
    public const string ChangePositionName = "change position";

    public const string CharacterSpriteName = "character sprite";
    public const string CharacterNameName = "character name";
    public const string EmotionName = "emotion";
    public const string VisibleName = "visible";
    public const string AppearanceDelayName = "appearance delay";
    public const string IsTalkingName = "is talking";
    public const string HideNameName = "hide name";
    public const string SmoothMovementName = "smooth movement";
    public const string PositionName = "position";
    public const string RotationName = "rotation";
    public const string ScaleName = "scale";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(ChangePositionName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var changePosition = GetBoolOption(ChangePositionName);

        context.AddInputPort<Sprite>(CharacterSpriteName).Build();
        context.AddInputPort<string>(CharacterNameName).Build();
        context.AddInputPort<CharacterEmotion>(EmotionName).Build();

        context.AddInputPort<bool>(VisibleName).Build();

        context.AddInputPort<float>(AppearanceDelayName).Build();
        context.AddInputPort<bool>(IsTalkingName).Build();
        context.AddInputPort<bool>(HideNameName).Build();

        if (!changePosition)
            return;

        context.AddInputPort<bool>(SmoothMovementName).Build();
        context.AddInputPort<Vector2>(PositionName).Build();
        context.AddInputPort<float>(RotationName).Build();
        context.AddInputPort<Vector2>(ScaleName).Build();
    }

    private bool GetBoolOption(string name, bool defaultValue = false)
    {
        var opt = GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<bool>(out var value) ? value : defaultValue;
    }
}
