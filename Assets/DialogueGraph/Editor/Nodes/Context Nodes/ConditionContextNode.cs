using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

[Serializable]
public class ConditionContextNode : ContextNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddOutputPort<List<ValueComparer>>("out")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
    }
}
