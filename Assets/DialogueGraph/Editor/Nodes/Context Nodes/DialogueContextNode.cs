using System;
using System.Collections.Generic;
using TMPro;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[Serializable]
public class DialogueContextNode : ContextNode
{
    private const string EditSettingsName = "edit settings";
    private const string QueueMusicName = "queue music";
    private const string PlayMultipleSfxName = "play multiple sfx";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(EditSettingsName);
        context.AddOption<bool>(QueueMusicName);
        context.AddOption<bool>(PlayMultipleSfxName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var portTypeOption = GetNodeOptionByName(EditSettingsName);
        portTypeOption.TryGetValue<bool>(out bool editSettings);

        portTypeOption = GetNodeOptionByName(QueueMusicName);
        portTypeOption.TryGetValue<bool>(out bool queueMusic);

        portTypeOption = GetNodeOptionByName(PlayMultipleSfxName);
        portTypeOption.TryGetValue<bool>(out bool playMultipleSfx);

        context.AddInputPort("in").Build();

        context.AddInputPort<List<CharacterData>>("initial character states").Build();

        if (!editSettings)
            return;

        context.AddInputPort<Sprite>("background image").Build();
        context.AddInputPort<bool>("smooth background transition").Build();

        if (queueMusic)
            context.AddInputPort<List<AudioResource>>("music audio queue").Build();
        else
            context.AddInputPort<AudioResource>("music audio").Build();

        if (playMultipleSfx)
            context.AddInputPort<List<AudioResource>>("all dialogue audio").Build();
        else
            context.AddInputPort<AudioResource>("dialogue audio").Build();

        context.AddInputPort<TMP_FontAsset>("font").Build();

        context.AddInputPort<float>("print speed")
            .WithDefaultValue(1f)
            .Build();
        context.AddInputPort<float>("delay text").Build();
    }
}
