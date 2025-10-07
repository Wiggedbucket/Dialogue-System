using System;
using UnityEditor;
using UnityEngine.UIElements;

[Serializable]
public class ValueComparer
{
    public object variable;
    public ComparisonType comparison;
    public object value;

    public bool Evaluate()
    {
        if (variable is IComparable v && value is IComparable val)
        {
            int cmp = v.CompareTo(val);
            switch (comparison)
            {
                case ComparisonType.Equal: return cmp == 0;
                case ComparisonType.NotEqual: return cmp != 0;
                case ComparisonType.Greater: return cmp > 0;
                case ComparisonType.Less: return cmp < 0;
                case ComparisonType.GreaterOrEqual: return cmp >= 0;
                case ComparisonType.LessOrEqual: return cmp <= 0;
            }
        }
        else
        {
            // Fallback for bool or unsupported types
            if (comparison == ComparisonType.Equal) return variable.Equals(value);
            if (comparison == ComparisonType.NotEqual) return !variable.Equals(value);
        }

        return false;
    }
}

public enum ComparisonType
{
    Equal,
    NotEqual,
    Greater,
    Less,
    GreaterOrEqual,
    LessOrEqual,
}