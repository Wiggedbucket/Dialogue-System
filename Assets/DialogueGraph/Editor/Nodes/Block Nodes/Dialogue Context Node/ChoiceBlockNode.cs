using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

[UseWithContext(typeof(DialogueContextNode))]
[Serializable]
public class ChoiceBlockNode : BlockNode
{
    public const string ChoiceTextPortName = "choice text";
    public const string ConditionsChoicePortName = "conditions choice";
    public const string ChoicePortName = "choice";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<string>(ChoiceTextPortName).Build();
        context.AddInputPort<List<ValueComparer>>(ConditionsChoicePortName).Build();
        context.AddOutputPort(ChoicePortName)
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
    }
}