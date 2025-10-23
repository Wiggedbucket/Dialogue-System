using System;
using System.Collections.Generic;

[Serializable]
public class RuntimeChoice
{
    public PortValue<string> choiceText = new();
    public PortValue<bool> showIfConditionNotMet = new();
    public List<ValueComparer> comparisons = new();
    public string nextNodeID;
}
