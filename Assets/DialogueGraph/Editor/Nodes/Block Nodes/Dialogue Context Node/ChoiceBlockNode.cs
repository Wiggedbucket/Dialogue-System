using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

[UseWithContext(typeof(DialogueContextNode))]
[Serializable]
public class ChoiceBlockNode : BlockNode
{
    public const string ChoiceTextPortName = "choice text";
    public const string ShowIfConditionNotMetName = "show if condition isn't met";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<string>(ChoiceTextPortName).Build();
        context.AddInputPort<bool>(ShowIfConditionNotMetName).Build();
        context.AddOutputPort("out")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
    }
}