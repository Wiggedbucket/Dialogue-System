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
    private const string QueueMusicName = "queue music";
    private const string PlayMultipleSfxName = "play multiple sfx";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(QueueMusicName);
        context.AddOption<bool>(PlayMultipleSfxName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var queueMusic = GetBoolOption(QueueMusicName);

        var playMultipleSfx = GetBoolOption(PlayMultipleSfxName);

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

    private bool GetBoolOption(string name, bool defaultValue = false)
    {
        var opt = GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<bool>(out var value) ? value : defaultValue;
    }
}
