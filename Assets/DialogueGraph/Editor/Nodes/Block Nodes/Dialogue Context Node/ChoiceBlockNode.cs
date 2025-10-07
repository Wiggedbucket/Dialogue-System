using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

[UseWithContext(typeof(DialogueContextNode))]
[Serializable]
public class ChoiceBlockNode : BlockNode
{
    const string portCountName = "port count";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        // Adds the options to the node when the user is finished with writing the number (Due to .Delayed())
        context.AddOption<int>(portCountName)
            .WithDisplayName("port count")
            .WithDefaultValue(2)
            .Delayed();
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // Adds all the option input and output ports
        var option = GetNodeOptionByName(portCountName);
        option.TryGetValue<int>(out int portCount);

        for (int i = 0; i < portCount; i++)
        {
            context.AddInputPort<string>($"choice text {i}").Build();
            context.AddInputPort<List<ValueComparer>>($"conditions choice {i}").Build();
            context.AddOutputPort($"choice {i}")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
        }
    }
}