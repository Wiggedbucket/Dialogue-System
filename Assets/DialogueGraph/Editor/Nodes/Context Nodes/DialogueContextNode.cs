using System;
using System.Collections.Generic;
using TMPro;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class DialogueContextNode : ContextNode
{
    public const string NextDialogueTextName = "next dialogue text";
    public const string DelayWithClickName = "delay with click";
    public const string KeepPreviousTextName = "keep previous text";
    public const string EditSettingsName = "edit settings";
    public const string EditTextSettingsName = "edit text settings";
    public const string EditEnvironmentSettingsName = "edit environment settings";

    public const string DialogueName = "dialogue";

    public const string PrintSpeedName = "print speed";
    public const string DelayTextName = "delay text";
    public const string BroadcastStringName = "broadcast string";

    public const string BoldName = "bold";
    public const string ItalicName = "italic";
    public const string UnderlineName = "underline";
    public const string FontName = "font";
    public const string TextAlignName = "text align";
    public const string WrapTextName = "wrap text";

    public const string MusicAudioQueueName = "music audio queue";
    public const string PlayAudioName = "play audio";
    public const string BackgroundImageName = "background image";
    public const string SmoothBackgroundTransitionName = "smooth background transition";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(NextDialogueTextName)
            .WithDefaultValue(true)
            .Build();
        context.AddOption<bool>(DelayWithClickName);
        context.AddOption<bool>(KeepPreviousTextName);
        context.AddOption<bool>(EditSettingsName);
        context.AddOption<bool>(EditTextSettingsName);
        context.AddOption<bool>(EditEnvironmentSettingsName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();

        var editSettings = GetBoolOption(EditSettingsName);

        var editTextSettings = GetBoolOption(EditTextSettingsName);

        var editEnvironmentSettings = GetBoolOption(EditEnvironmentSettingsName);

        context.AddOutputPort("out")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();

        context.AddInputPort<DialogueData>(DialogueName).Build();

        context.AddInputPort<string>(BroadcastStringName).Build();

        if (editSettings)
        {
            context.AddInputPort<float>(PrintSpeedName)
            .WithDefaultValue(1f)
            .Build();
            context.AddInputPort<float>(DelayTextName).Build();
        }

        if (editTextSettings)
        {
            context.AddInputPort<bool>(BoldName).Build();
            context.AddInputPort<bool>(ItalicName).Build();
            context.AddInputPort<bool>(UnderlineName).Build();

            context.AddInputPort<TMP_FontAsset>(FontName).Build();
            context.AddInputPort<TextAlignmentOptions>(TextAlignName).Build();
            context.AddInputPort<bool>(WrapTextName)
                .WithDefaultValue(true)
                .Build();
        }

        if (editEnvironmentSettings)
        {
            context.AddInputPort<List<AudioResource>>(MusicAudioQueueName).Build();
            context.AddInputPort<List<AudioResource>>(PlayAudioName).Build();

            context.AddInputPort<Sprite>(BackgroundImageName).Build();
            context.AddInputPort<bool>(SmoothBackgroundTransitionName).Build();
        }
    }

    private bool GetBoolOption(string name, bool defaultValue = false)
    {
        var opt = GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<bool>(out var value) ? value : defaultValue;
    }
}
