using System;
using System.Collections.Generic;
using TMPro;
using Unity.GraphToolkit.Editor;

[Serializable]
public class DialogueContextNode : ContextNode
{
    private const string NextDialogueTextName = "next dialogue text";
    private const string DelayWithClickName = "delay with click";
    private const string KeepPreviousTextName = "keep previous text";
    private const string EditSettingsName = "edit settings";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(NextDialogueTextName)
            .WithDefaultValue(true)
            .Build();
        context.AddOption<bool>(DelayWithClickName);
        context.AddOption<bool>(KeepPreviousTextName);
        context.AddOption<bool>(EditSettingsName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();

        var editSettings = GetBoolOption(EditSettingsName);

        context.AddOutputPort("out")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();

        context.AddInputPort<DialogueData>("dialogue")
            .Build();

        if (!editSettings)
            return;

        context.AddInputPort<bool>("bold").Build();
        context.AddInputPort<bool>("italic").Build();
        context.AddInputPort<bool>("underline").Build();

        context.AddInputPort<TMP_FontAsset>("font").Build();
        context.AddInputPort<TextAlignmentOptions>("text align").Build();
        context.AddInputPort<bool>("wrap text")
            .WithDefaultValue(true)
            .Build();

        context.AddInputPort<float>("print speed")
            .WithDefaultValue(1f)
            .Build();
        context.AddInputPort<float>("delay text").Build();
    }

    private bool GetBoolOption(string name, bool defaultValue = false)
    {
        var opt = GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<bool>(out var value) ? value : defaultValue;
    }
}
