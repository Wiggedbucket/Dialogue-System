using System;

[Serializable]
public class PortValue<T>
{
    public bool usePortValue = false;
    public string blackboardVariableName = null;
    public T value = default;

    public bool GetValue(DialogueBlackboard blackboard, out T portValue)
    {
        if (!string.IsNullOrEmpty(blackboardVariableName) &&
            blackboard.TryGetValue<T>(blackboardVariableName, out var bbValue))
        {
            portValue = bbValue;
        }
        else
        {
            portValue = value;
        }

        return usePortValue;
    }

    public T GetValue(DialogueBlackboard blackboard)
    {
        if (!string.IsNullOrEmpty(blackboardVariableName) &&
            blackboard.TryGetValue<T>(blackboardVariableName, out var bbValue))
        {
            return bbValue;
        }

        return value;
    }
}
