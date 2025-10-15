using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

[UseWithContext(typeof(DialogueContextNode))]
[Serializable]
public class ChoiceBlockNode : BlockNode
{
    public const string ChoiceTextPortName = "choice text";
    public const string ShowIfConditionNotMetPortName = "show if condition isn't met";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<string>(ChoiceTextPortName).Build();
        context.AddInputPort<bool>(ShowIfConditionNotMetPortName).Build();
        context.AddOutputPort("out")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
    }
}