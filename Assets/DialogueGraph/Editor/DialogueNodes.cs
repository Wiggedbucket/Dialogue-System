using Unity.GraphToolkit.Editor;
using System;

[Serializable]
public class StartNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddOutputPort("out").Build();
    }
}

[Serializable]
public class EndNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in").Build();
    }
}

[Serializable]
public class DialogueNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in").Build();
        context.AddOutputPort("out").Build();

        context.AddInputPort<string>("speaker").Build();
        context.AddInputPort<string>("dialogue").Build();
    }
}

[Serializable]
public class ChoiceNode : Node
{
    const string portCountName = "portCount";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in").Build();

        context.AddInputPort<string>("speaker").Build();
        context.AddInputPort<string>("dialogue").Build();

        // Adds all the option input and output ports
        var option = GetNodeOptionByName(portCountName);
        option.TryGetValue(out int portCount);
        for (int i = 0; i < portCount; i++)
        {
            context.AddInputPort<string>($"Choice Text {i}").Build();
            context.AddOutputPort($"Choice {i}").Build();
        }
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        // Adds the options to the node when the user is finished with writing the number (Due to .Delayed())
        context.AddOption<int>(portCountName)
            .WithDisplayName("Port Count")
            .WithDefaultValue(2)
            .Delayed();
    }
}