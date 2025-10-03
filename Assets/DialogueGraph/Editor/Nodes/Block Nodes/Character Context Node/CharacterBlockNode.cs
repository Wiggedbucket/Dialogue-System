using System;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.UI;

[UseWithContext(typeof(CharacterContextNode), typeof(DialogueContextNode))]
[Serializable]
public class CharacterBlockNode : BlockNode
{
    private const string ChangePositionName = "change position";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(ChangePositionName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var portTypeOption = GetNodeOptionByName(ChangePositionName);
        portTypeOption.TryGetValue<bool>(out bool changePosition);

        context.AddInputPort<Image>("character image").Build();
        context.AddInputPort<string>("name").Build();
        context.AddInputPort<CharacterEmotion>("emotion").Build();

        context.AddInputPort<bool>("visible").Build();

        context.AddInputPort<float>("appearance delay").Build();
        context.AddInputPort<bool>("talking").Build();
        context.AddInputPort<bool>("hide name").Build();

        if (!changePosition)
            return;

        context.AddInputPort<bool>("smooth movement").Build();
        context.AddInputPort<Vector2>("position").Build();
        context.AddInputPort<float>("rotation").Build();
        context.AddInputPort<Vector2>("scale").Build();
    }
}
