using System;
using System.Collections.Generic;
using TMPro;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class DialogueContextNode : ContextNode
{
    public const string NextDialogueTextPortName = "next dialogue text";
    public const string DelayWithClickPortName = "delay with click";
    public const string KeepPreviousTextPortName = "keep previous text";
    public const string EditSettingsPortName = "edit settings";
    public const string EditTextSettingsPortName = "edit text settings";
    public const string EditEnvironmentSettingsPortName = "edit environment settings";

    public const string DialoguePortName = "dialogue";

    public const string PrintSpeedPortName = "print speed";
    public const string DelayTextPortName = "delay text";
    public const string BroadcastStringPortName = "broadcast string";

    public const string BoldPortName = "bold";
    public const string ItalicPortName = "italic";
    public const string UnderlinePortName = "underline";
    public const string FontPortName = "font";
    public const string TextAlignPortName = "text align";
    public const string WrapTextPortName = "wrap text";

    public const string MusicAudioQueuePortName = "music audio queue";
    public const string PlayAudioPortName = "play audio";
    public const string BackgroundImagePortName = "background image";
    public const string BackgroundTransitionPortName = "smooth background transition";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(NextDialogueTextPortName)
            .WithDefaultValue(true)
            .Build();
        context.AddOption<bool>(DelayWithClickPortName);
        context.AddOption<bool>(KeepPreviousTextPortName);
        context.AddOption<bool>(EditSettingsPortName);
        context.AddOption<bool>(EditTextSettingsPortName);
        context.AddOption<bool>(EditEnvironmentSettingsPortName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();

        var editSettings = GetBoolOption(EditSettingsPortName);

        var editTextSettings = GetBoolOption(EditTextSettingsPortName);

        var editEnvironmentSettings = GetBoolOption(EditEnvironmentSettingsPortName);

        context.AddOutputPort("out")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();

        context.AddInputPort<DialogueData>(DialoguePortName).Build();

        context.AddInputPort<string>(BroadcastStringPortName).Build();

        if (editSettings)
        {
            context.AddInputPort<float>(PrintSpeedPortName)
            .WithDefaultValue(1f)
            .Build();
            context.AddInputPort<float>(DelayTextPortName).Build();
        }

        if (editTextSettings)
        {
            context.AddInputPort<bool>(BoldPortName).Build();
            context.AddInputPort<bool>(ItalicPortName).Build();
            context.AddInputPort<bool>(UnderlinePortName).Build();

            context.AddInputPort<TMP_FontAsset>(FontPortName).Build();
            context.AddInputPort<TextAlignmentOptions>(TextAlignPortName).Build();
            context.AddInputPort<bool>(WrapTextPortName)
                .WithDefaultValue(true)
                .Build();
        }

        if (editEnvironmentSettings)
        {
            context.AddInputPort<List<AudioResource>>(MusicAudioQueuePortName).Build();
            context.AddInputPort<List<AudioResource>>(PlayAudioPortName).Build();

            context.AddInputPort<Sprite>(BackgroundImagePortName).Build();
            context.AddInputPort<BackgroundTransition>(BackgroundTransitionPortName).Build();
        }
    }

    private bool GetBoolOption(string name, bool defaultValue = false)
    {
        var opt = GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<bool>(out var value) ? value : defaultValue;
    }
}
