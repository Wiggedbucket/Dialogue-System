using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

[UseWithContext(typeof(DialogueContextNode))]
[Serializable]
public class ChoiceBlockNode : BlockNode
{
    public const string ChoiceTextPortName = "choice text";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<string>(ChoiceTextPortName).Build();
        context.AddOutputPort("out")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
    }
}