using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;

[UseWithContext(typeof(DialogueContextNode))]
[Serializable]
public class EndDialogueBlockNode : BlockNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddOutputPort("out").Build();
    }
}
