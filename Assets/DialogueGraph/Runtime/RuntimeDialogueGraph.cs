using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Dialogue/Runtime Dialogue Graph")]
[Serializable]
public class RuntimeDialogueGraph : ScriptableObject
{
    public string entryNodeID;
    [SerializeReference]
    public List<RuntimeNode> nodes = new();
    public List<RuntimeVariable> blackboardVariables = new();

    // Quick lookup
    private Dictionary<string, RuntimeNode> nodeLookup;

    public RuntimeNode GetNode(string id)
    {
        nodeLookup ??= BuildLookup();
        nodeLookup.TryGetValue(id, out var node);
        return node;
    }

    private Dictionary<string, RuntimeNode> BuildLookup()
    {
        var map = new Dictionary<string, RuntimeNode>();
        foreach (var n in nodes)
            map[n.nodeID] = n;
        return map;
    }
}

[Serializable]
public abstract class RuntimeNode
{
    public string nodeID;
    public string nextNodeID;
}

[Serializable]
public class RuntimeDialogueNode : RuntimeNode
{
    public string dialogueText;
    public DialogueSettings dialogueSettings;

    public List<CharacterData> characters = new();
    public List<RuntimeChoice> choices = new();
}

[Serializable]
public class RuntimeChoice
{
    public string choiceText;
    public bool showIfConditionNotMet;
    public List<ValueComparer> comparisons = new();
    public string nextNodeID;
}

[Serializable]
public class DialogueSettings
{
    // Node options
    public bool nextDialogueText = true;
    public bool delayWithClick = false;
    public bool keepPreviousText = false;
    public bool editSettings = false;
    public bool editTextSettings = false;
    public bool editEnvironmentSettings = false;

    // Variables
    public float printSpeed = 1f;
    public float delayText = 0f;
    public string broadcastString = "";

    public bool bold = false;
    public bool italic = false;
    public bool underline = false;
    public TMP_FontAsset font = null;
    public TextAlignmentOptions textAlign = TextAlignmentOptions.TopLeft;
    public bool wrapText = true;

    public List<AudioResource> musicQueue;
    public List<AudioResource> audioList;
    public Sprite backgroundImage;
    public BackgroundTransition backgroundTransition;
}

[Serializable]
public enum BackgroundTransition
{
    None,
    FadeOutAndIn,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,
    FadeRight,
    FadeLeft,
    FadeUp,
    FadeDown,
}

[Serializable]
public class RuntimeSplitterOutput
{
    public string nextNodeID;
    public List<ValueComparer> comparisons = new();
}

[Serializable]
public class RuntimeSplitterNode : RuntimeNode
{
    public List<RuntimeSplitterOutput> outputs = new();
    public string defaultOutputNodeID;
}

public class RuntimeBlackboard
{
    private readonly Dictionary<string, RuntimeVariable> variables = new();

    public RuntimeBlackboard(List<RuntimeVariable> variableList)
    {
        foreach (var v in variableList)
            variables[v.name] = v;
    }

    public T Get<T>(string name)
    {
        return variables.TryGetValue(name, out var v) ? v.GetValue<T>() : default;
    }

    public void Set(string name, object newValue)
    {
        if (variables.TryGetValue(name, out var v))
            v.SetValue(newValue);
    }

    public bool Compare(ValueComparer comparer)
    {
        if (!variables.TryGetValue(comparer.variable.ToString(), out var v))
            return false;

        comparer.variable = v.value;
        return comparer.Evaluate();
    }

    /*
     * 
     *  To use in runtime:
     *      var runtimeBlackboard = new RuntimeBlackboard(runtimeGraph.blackboardVariables);
     *      runtimeBlackboard.Set("hasKey", true);
     *      bool hasKey = runtimeBlackboard.Get<bool>("hasKey");
     * 
     */
}

[Serializable]
public class RuntimeVariable
{
    public string name;
    public VariableType type;
    public object value;

    public T GetValue<T>() => value is T t ? t : default;
    public void SetValue(object newValue) => value = newValue;
}

[Serializable]
public class ValueComparer
{
    public VariableType variableType;
    public ComparisonType comparisonType;
    public object variable;
    public object value;

    public bool Evaluate()
    {
        if (variable == null || value == null)
            return false;

        switch (variableType)
        {
            case VariableType.Bool:
                return (bool)variable == (bool)value;

            case VariableType.String:
                return (string)variable == (string)value;

            case VariableType.Float:
                return Compare((float)variable, (float)value);

            case VariableType.Int:
                return Compare((int)variable, (int)value);

            default:
                return false;
        }
    }

    private bool Compare<T>(T a, T b) where T : IComparable
    {
        int cmp = a.CompareTo(b);
        return comparisonType switch
        {
            ComparisonType.Equal => cmp == 0,
            ComparisonType.NotEqual => cmp != 0,
            ComparisonType.Greater => cmp > 0,
            ComparisonType.Less => cmp < 0,
            ComparisonType.GreaterOrEqual => cmp >= 0,
            ComparisonType.LessOrEqual => cmp <= 0,
            _ => false
        };
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

public enum VariableType
{
    Float,
    Int,
    Bool,
    String,
}
