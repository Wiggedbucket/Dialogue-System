using System;
using Unity.GraphToolkit.Editor;

[UseWithContext(typeof(SplitterContextNode), typeof(DialogueContextNode))]
[Serializable]
public class CompareBlockNode : BlockNode
{
    public const string SelectVariableTypePortName = "select variable type";

    public const string VariablePortName = "variable";
    public const string ValuePortName = "value";
    public const string ComparisonTypePortName = "comparison type";

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<VariableType>(SelectVariableTypePortName);
    }

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        VariableType variableType = GetEnumOption(SelectVariableTypePortName);

        switch (variableType)
        {
            case VariableType.Bool:
                context.AddInputPort<bool>(VariablePortName).Build();
                context.AddInputPort<bool>(ValuePortName).Build();
                break;
            case VariableType.String:
                context.AddInputPort<string>(VariablePortName).Build();
                context.AddInputPort<string>(ValuePortName).Build();
                break;
            case VariableType.Float:
                context.AddInputPort<float>(VariablePortName).Build();
                context.AddInputPort<ComparisonType>(ComparisonTypePortName).Build();
                context.AddInputPort<float>(ValuePortName).Build();
                break;
            case VariableType.Int:
                context.AddInputPort<int>(VariablePortName).Build();
                context.AddInputPort<ComparisonType>(ComparisonTypePortName).Build();
                context.AddInputPort<int>(ValuePortName).Build();
                break;
        }
    }

    private VariableType GetEnumOption(string name, VariableType defaultValue = VariableType.Float)
    {
        var opt = GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<VariableType>(out var value) ? value : defaultValue;
    }
}