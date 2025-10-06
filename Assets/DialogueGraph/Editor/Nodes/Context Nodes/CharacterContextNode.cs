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
        var showCharacters = GetBoolOption(ShowCharactersName);

        context.AddOutputPort<List<CharacterData>>("out").Build();
    }

    private bool GetBoolOption(string name, bool defaultValue = false)
    {
        var opt = GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<bool>(out var value) ? value : defaultValue;
    }
}
