using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.UI;

[UseWithContext(typeof(CharacterContextNode), typeof(DialogueContextNode))]
[Serializable]
public class CharacterBlockNode : BlockNode
{
    private const string EditMultipleName = "edit multiple";
    private const string ChangePositionName = "change position";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(EditMultipleName);
        context.AddOption<bool>(ChangePositionName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var portTypeOption = GetNodeOptionByName(EditMultipleName);
        portTypeOption.TryGetValue<bool>(out bool editMultiple);

        portTypeOption = GetNodeOptionByName(ChangePositionName);
        portTypeOption.TryGetValue<bool>(out bool changePosition);

        if (editMultiple)
        {
            context.AddInputPort<List<CharacterData>>("character states").Build();
            return;
        }

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
