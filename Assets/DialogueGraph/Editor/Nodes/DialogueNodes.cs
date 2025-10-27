using System;
using Unity.GraphToolkit.Editor;

[Serializable]
public class StartNode : Node
{
    public const string AllowEscapeOptionName = "allow escape";
    public const string AllowFastAdvanceOptionName = "allow fast advance";
    public const string TextShadowOnMultipleCharactersTalkingOptionName = "text shadow when multiple characters are talking";
    public const string NotTalkingTypeOptionName = "not talking type";
    public const string DialogueBoxTransitionOptionName = "dialogue transition";
    public const string TransitionDurationOptionName = "transition duration";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(AllowEscapeOptionName).Build();
        context.AddOption<bool>(AllowFastAdvanceOptionName).WithDefaultValue(true).Build();
        context.AddOption<bool>(TextShadowOnMultipleCharactersTalkingOptionName).WithDefaultValue(true).Build();
        context.AddOption<NotTalkingType>(NotTalkingTypeOptionName).WithDefaultValue(NotTalkingType.None).Build();
        context.AddOption<DialogueBoxTransition>(DialogueBoxTransitionOptionName).WithDefaultValue(DialogueBoxTransition.None).Build();
        context.AddOption<float>(TransitionDurationOptionName).WithDefaultValue(0.5f).Build();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddOutputPort("out").Build();
    }
}

[Serializable]
public class InteruptNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in").Build();
        context.AddOutputPort("out").Build();
    }
}

[Serializable]
public class EndNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in").Build();
    }
}