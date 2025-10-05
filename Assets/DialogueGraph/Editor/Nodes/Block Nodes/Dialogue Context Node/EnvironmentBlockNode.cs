using System;
using System.Collections.Generic;
using TMPro;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Audio;

[UseWithContext(typeof(DialogueContextNode))]
[Serializable]
public class EnvironmentBlockNode : BlockNode
{
    private const string DelayWithClickName = "delay with click";
    private const string QueueMusicName = "queue music";
    private const string PlayMultipleSfxName = "play multiple sfx";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(DelayWithClickName);
        context.AddOption<bool>(QueueMusicName);
        context.AddOption<bool>(PlayMultipleSfxName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var portTypeOption = GetNodeOptionByName(QueueMusicName);
        portTypeOption.TryGetValue<bool>(out bool queueMusic);

        portTypeOption = GetNodeOptionByName(PlayMultipleSfxName);
        portTypeOption.TryGetValue<bool>(out bool playMultipleSfx);

        if (queueMusic)
            context.AddInputPort<List<AudioResource>>("music audio queue").Build();
        else
            context.AddInputPort<AudioResource>("music audio").Build();

        if (playMultipleSfx)
            context.AddInputPort<List<AudioResource>>("all dialogue audio").Build();
        else
            context.AddInputPort<AudioResource>("dialogue audio").Build();

        context.AddInputPort<Sprite>("background image").Build();
        context.AddInputPort<bool>("smooth background transition").Build();
    }
}
