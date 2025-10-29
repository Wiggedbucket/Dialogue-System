using System;

[Serializable]
public class ValueComparer
{
    public VariableType variableType;
    public PortValue<ComparisonType> comparisonType = new();
    public PortValue<bool> equals = new();

    public PortValue<float> floatVariable = new();
    public PortValue<float> floatValue = new();

    public PortValue<int> intVariable = new();
    public PortValue<int> intValue = new();

    public PortValue<bool> boolVariable = new();
    public PortValue<bool> boolValue = new();

    public PortValue<string> stringVariable = new();
    public PortValue<string> stringValue = new();

    public bool Evaluate(DialogueBlackboard blackBoard)
    {
        bool eq = equals.GetValue(blackBoard);

        return variableType switch
        {
            VariableType.Bool => eq
                ? boolVariable.GetValue(blackBoard) == boolValue.GetValue(blackBoard)
                : boolVariable.GetValue(blackBoard) != boolValue.GetValue(blackBoard),

            VariableType.String => eq
                ? stringVariable.GetValue(blackBoard) == stringValue.GetValue(blackBoard)
                : stringVariable.GetValue(blackBoard) != stringValue.GetValue(blackBoard),

            VariableType.Float => Compare(floatVariable.GetValue(blackBoard), floatValue.GetValue(blackBoard), blackBoard),
            VariableType.Int => Compare(intVariable.GetValue(blackBoard), intValue.GetValue(blackBoard), blackBoard),
            _ => false,
        };
    }

    private bool Compare<T>(T a, T b, DialogueBlackboard blackBoard) where T : IComparable
    {
        int cmp = a.CompareTo(b);
        return comparisonType.GetValue(blackBoard) switch
        {
            ComparisonType.Equal => cmp == 0,
            ComparisonType.NotEqual => cmp != 0,
            ComparisonType.Greater => cmp > 0,
            ComparisonType.Less => cmp < 0,
            ComparisonType.GreaterOrEqual => cmp >= 0,
            ComparisonType.LessOrEqual => cmp <= 0,
            _ => false,
        };
    }
}
