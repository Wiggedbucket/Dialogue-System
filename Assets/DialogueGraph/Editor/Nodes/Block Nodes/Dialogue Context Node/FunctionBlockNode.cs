using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Events;

[UseWithContext(typeof(DialogueContextNode))]
[Serializable]
public class FunctionBlockNode : BlockNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<string>("class name").Build();
        context.AddInputPort<string>("function name").Build();

        context.AddInputPort<UnityEvent>("unity event").Build();
    }
}
