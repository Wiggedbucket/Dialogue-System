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

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(EditSettingsName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var portTypeOption = GetNodeOptionByName(EditSettingsName);
        portTypeOption.TryGetValue<bool>(out bool editSettings);

        context.AddInputPort("in").Build();

        context.AddInputPort<List<CharacterData>>("initial character states").Build();

        if (!editSettings)
            return;

        context.AddInputPort<Sprite>("background image").Build();
        context.AddInputPort<bool>("smooth background transition").Build();
        context.AddInputPort<AudioResource>("music audio").Build();
        context.AddInputPort<AudioResource>("dialogue audio").Build();
        context.AddInputPort<TMP_FontAsset>("font").Build();

        context.AddInputPort<float>("print speed")
            .WithDefaultValue(1f)
            .Build();
        context.AddInputPort<float>("delay text").Build();
    }
}
