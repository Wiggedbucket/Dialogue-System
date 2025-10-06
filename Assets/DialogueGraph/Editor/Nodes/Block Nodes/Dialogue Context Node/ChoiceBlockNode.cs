using System;
using Unity.GraphToolkit.Editor;

[UseWithContext(typeof(DialogueContextNode))]
[Serializable]
public class ChoiceBlockNode : BlockNode
{
    const string portCountName = "portCount";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        // Adds the options to the node when the user is finished with writing the number (Due to .Delayed())
        context.AddOption<int>(portCountName)
            .WithDisplayName("Port Count")
            .WithDefaultValue(2)
            .Delayed();

        context.AddOption<bool>("show characters");
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        // Adds all the option input and output ports
        var option = GetNodeOptionByName(portCountName);
        option.TryGetValue<int>(out int portCount);

        for (int i = 0; i < portCount; i++)
        {
            context.AddInputPort<string>($"Choice Text {i}").Build();
            context.AddOutputPort($"Choice {i}")
            .WithConnectorUI(PortConnectorUI.Arrowhead)
            .Build();
        }
    }
}