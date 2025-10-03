using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

[Serializable]
public class CharacterContextNode : ContextNode
{
    private const string ShowCharactersName = "show characters";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<bool>(ShowCharactersName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        var portTypeOption = GetNodeOptionByName(ShowCharactersName);
        portTypeOption.TryGetValue<bool>(out bool showCharacters);

        context.AddOutputPort<List<CharacterData>>("out").Build();
    }
}
