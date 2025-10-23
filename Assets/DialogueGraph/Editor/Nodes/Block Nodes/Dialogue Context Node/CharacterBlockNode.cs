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
    public const string ChangeVisibilityPortName = "change visibility";
    public const string ChangeAppearanceDelayPortName = "change appearance delay";
    public const string ChangePreserveAspectPortName = "change preserve aspect";
    public const string ChangeTransitionDurationPortName = "change transition duration";
    public const string ChangePositionPortName = "change position";
    public const string ChangeAnchorsPortName = "change anchors";
    public const string ChangePivotPortName = "change pivot";
    public const string ChangeRotationPortName = "change rotation";
    public const string ChangeWidthAndHeightPortName = "change width and height";
    public const string ChangeScalePortName = "change scale";

    public const string CharacterSpritePortName = "character sprite";
    public const string CharacterNamePortName = "character name";
    public const string VisiblePortName = "visible";
    public const string AppearanceDelayPortName = "appearance delay";
    public const string IsTalkingPortName = "is talking";
    public const string HideNamePortName = "hide name";
    public const string PreserveAspectPortName = "preserve aspect";
    public const string TransitionDurationDelayPortName = "transition duration";
    public const string PositionMovementTypePortName = "movement type";
    public const string RotationMovementTypePortName = "rotation type";
    public const string ScaleMovementTypePortName = "scale type";
    public const string PositionPortName = "position";
    public const string PredefinedPositionsPortName = "predefined positions";
    public const string MinAnchorPortName = "min anchor";
    public const string MaxAnchorPortName = "max anchor";
    public const string PivotPortName = "pivot";
    public const string RotationPortName = "rotation";
    public const string WidthAndHeightPortName = "width and height";
    public const string ScalePortName = "scale";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(ChangeSpritePortName);
        context.AddOption<bool>(ChangeVisibilityPortName);
        context.AddOption<bool>(ChangeAppearanceDelayPortName);
        context.AddOption<bool>(ChangePreserveAspectPortName);
        context.AddOption<bool>(ChangeTransitionDurationPortName);
        context.AddOption<bool>(ChangePositionPortName);
        context.AddOption<bool>(ChangeAnchorsPortName);
        context.AddOption<bool>(ChangePivotPortName);
        context.AddOption<bool>(ChangeRotationPortName);
        context.AddOption<bool>(ChangeWidthAndHeightPortName);
        context.AddOption<bool>(ChangeScalePortName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var changeSprite = GetBoolOption(ChangeSpritePortName);
        var changeVisibility = GetBoolOption(ChangeVisibilityPortName);
        var changeAppearanceDelay = GetBoolOption(ChangeAppearanceDelayPortName);
        var changePreserveAspect = GetBoolOption(ChangePreserveAspectPortName);
        var changeTransitionDuration = GetBoolOption(ChangeTransitionDurationPortName);
        var changePosition = GetBoolOption(ChangePositionPortName);
        var changeAnchors = GetBoolOption(ChangeAnchorsPortName);
        var changePivot = GetBoolOption(ChangePivotPortName);
        var changeRotation = GetBoolOption(ChangeRotationPortName);
        var changeWidthAndHeight = GetBoolOption(ChangeWidthAndHeightPortName);
        var changeScale = GetBoolOption(ChangeScalePortName);

        context.AddInputPort<string>(CharacterNamePortName).Build();

        if (changeSprite)
            context.AddInputPort<Sprite>(CharacterSpritePortName).Build();

        if (changeVisibility)
            context.AddInputPort<bool>(VisiblePortName).Build();

        if (changeAppearanceDelay)
            context.AddInputPort<float>(AppearanceDelayPortName).Build();

        context.AddInputPort<bool>(IsTalkingPortName).Build();
        context.AddInputPort<bool>(HideNamePortName).Build();

        if (changePreserveAspect)
            context.AddInputPort<bool>(PreserveAspectPortName).Build();

        if (changeTransitionDuration)
            context.AddInputPort<float>(TransitionDurationDelayPortName).Build();

        if (changePosition)
        {
            context.AddInputPort<MovementType>(PositionMovementTypePortName)
                .WithDefaultValue(MovementType.Instant)
                .Build();

            context.AddInputPort<PredefinedPosition>(PredefinedPositionsPortName).WithDefaultValue(PredefinedPosition.None).Build();
            context.AddInputPort<Vector2>(PositionPortName).Build();
        }

        if (changeAnchors)
        {
            context.AddInputPort<Vector2>(MinAnchorPortName).Build();
            context.AddInputPort<Vector2>(MaxAnchorPortName).Build();
        }
        if (changePivot)
            context.AddInputPort<Vector2>(PivotPortName).Build();

        if (changeRotation)
        {
            context.AddInputPort<MovementType>(RotationMovementTypePortName)
                .WithDefaultValue(MovementType.Instant)
                .Build();

            context.AddInputPort<float>(RotationPortName).Build();
        }

        if (changeWidthAndHeight)
            context.AddInputPort<Vector2>(WidthAndHeightPortName).Build();
        if (changeScale)
        {
            context.AddInputPort<MovementType>(ScaleMovementTypePortName)
                .WithDefaultValue(MovementType.Instant)
                .Build();

            context.AddInputPort<Vector2>(ScalePortName).Build();
        }
    }

    private bool GetBoolOption(string name, bool defaultValue = false)
    {
        var opt = GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<bool>(out var value) ? value : defaultValue;
    }
}
