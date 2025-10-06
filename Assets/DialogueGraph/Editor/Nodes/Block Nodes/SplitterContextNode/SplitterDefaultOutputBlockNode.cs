using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[UseWithContext(typeof(SplitterContextNode))]
[Serializable]
public class SplitterDefaultOutputBlockNode : BlockNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddOutputPort("out")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
    }
}
