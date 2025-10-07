using System;
using Unity.GraphToolkit.Editor;

[UseWithContext(typeof(SplitterContextNode), typeof(ConditionContextNode))]
[Serializable]
public class CompareBlockNode : BlockNode
{
    private const string SelectVariableTypeName = "select variable type";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<VariableType>(SelectVariableTypeName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        VariableType variableType = GetEnumOption(SelectVariableTypeName);

        switch (variableType)
        {
            case VariableType.Bool:
                context.AddInputPort<bool>("variable").Build();
                context.AddInputPort<bool>("value").Build();
                break;
            case VariableType.String:
                context.AddInputPort<string>("variable").Build();
                context.AddInputPort<string>("value").Build();
                break;
            case VariableType.Float:
                context.AddInputPort<float>("variable").Build();
                context.AddInputPort<ComparisonType>("comparison type").Build();
                context.AddInputPort<float>("value").Build();
                break;
            case VariableType.Int:
                context.AddInputPort<int>("variable").Build();
                context.AddInputPort<ComparisonType>("comparison type").Build();
                context.AddInputPort<int>("value").Build();
                break;
        }
    }

    private VariableType GetEnumOption(string name, VariableType defaultValue = VariableType.Float)
    {
        var opt = GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<VariableType>(out var value) ? value : defaultValue;
    }
}