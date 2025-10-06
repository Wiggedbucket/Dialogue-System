using System;
using Unity.GraphToolkit.Editor;

[Serializable]
public class SplitterContextNode : ContextNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
    }
}
