using System;
using TMPro;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Audio;

[UseWithContext(typeof(DialogueContextNode))]
[Serializable]
public class EnvironmentBlockNode : BlockNode
{
    private const string DelayWithClickName = "delay with click";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(DelayWithClickName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<AudioResource>("music audio").Build();
        context.AddInputPort<AudioResource>("dialogue audio").Build();

        context.AddInputPort<Sprite>("background image").Build();
        context.AddInputPort<bool>("smooth background transition").Build();
    }
}
