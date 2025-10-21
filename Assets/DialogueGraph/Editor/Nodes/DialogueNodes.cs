using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class StartNode : Node
{
    public const string AllowEscapeOptionName = "allow escape";
    public const string AllowFastAdvanceOptionName = "allow fast advance";
    public const string TextShadowOnMultipleCharactersTalkingOptionName = "text shadow when multiple characters are talking";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(AllowEscapeOptionName).Build();
        context.AddOption<bool>(AllowFastAdvanceOptionName).WithDefaultValue(true).Build();
        context.AddOption<bool>(TextShadowOnMultipleCharactersTalkingOptionName).WithDefaultValue(true).Build();
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