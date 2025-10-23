using System;
using System.Collections.Generic;

[Serializable]
public class RuntimeSplitterNode : RuntimeNode
{
    public List<RuntimeSplitterOutput> outputs = new();
    public string defaultOutputNodeID;
}

[Serializable]
public class RuntimeSplitterOutput
{
    public string nextNodeID;
    public List<ValueComparer> comparisons = new();
}
