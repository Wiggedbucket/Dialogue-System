using System;
using System.Collections.Generic;
using TMPro;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[UseWithContext(typeof(DialogueContextNode))]
[Serializable]
public class DialogueBlockNode : BlockNode
{
    private const string DelayWithClickName = "delay with click";
    private const string EditSettingsName = "edit settings";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(DelayWithClickName);
        context.AddOption<bool>(EditSettingsName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var portTypeOption = GetNodeOptionByName(EditSettingsName);
        portTypeOption.TryGetValue<bool>(out bool editSettings);

        context.AddInputPort<string>("text").Build();

        if (!editSettings)
            return;

        context.AddInputPort<TMP_FontAsset>("font").Build();

        context.AddInputPort<float>("print speed")
            .WithDefaultValue(1f)
            .Build();
        context.AddInputPort<float>("delay text").Build();
    }
}
