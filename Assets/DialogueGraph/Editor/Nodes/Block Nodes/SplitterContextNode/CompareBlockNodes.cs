using System;
using Unity.GraphToolkit.Editor;

[UseWithContext(typeof(SplitterContextNode))]
[Serializable]
public class CompareFloatsBlockNode : BlockNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<float>("variable").Build();
        context.AddInputPort<ComparisonType>("comparison type").Build();
        context.AddInputPort<float>("value").Build();
    }
}

[UseWithContext(typeof(SplitterContextNode))]
[Serializable]
public class CompareIntsBlockNode : BlockNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<int>("variable").Build();
        context.AddInputPort<ComparisonType>("comparison type").Build();
        context.AddInputPort<int>("value").Build();
    }
}

[UseWithContext(typeof(SplitterContextNode))]
[Serializable]
public class CompareBoolsBlockNode : BlockNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<bool>("variable").Build();
        context.AddInputPort<bool>("value").Build();
    }
}

[UseWithContext(typeof(SplitterContextNode))]
[Serializable]
public class CompareStringsBlockNode : BlockNode
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<string>("variable").Build();
        context.AddInputPort<string>("value").Build();
    }
}