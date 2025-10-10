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
    [SerializeReference]
    public List<BlackBoardVariableBase> blackboardVariables = new();

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

#region Runtime Nodes
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
public class RuntimeChoice
{
    public string choiceText;
    public bool showIfConditionNotMet;
    public List<ValueComparer> comparisons = new();
    public string nextNodeID;
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

[Serializable]
public class ValueComparer
{
    public string variableName;
    public VariableType variableType;
    public ComparisonType comparisonType;
    public bool equals;

    public float floatVariable;
    public float floatValue;

    public int intVariable;
    public int intValue;

    public bool boolVariable;
    public bool boolValue;

    public string stringVariable;
    public string stringValue;

    public bool Evaluate()
    {
        switch (variableType)
        {
            case VariableType.Bool:
                return equals ? boolVariable == boolValue : boolVariable != boolValue;

            case VariableType.String:
                return equals ? stringVariable == stringValue : stringVariable != stringValue;

            case VariableType.Float:
                return Compare(floatVariable, floatValue);

            case VariableType.Int:
                return Compare(intVariable, intValue);

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
#endregion

#region Enums
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
public enum ComparisonType
{
    Equal,
    NotEqual,
    Greater,
    Less,
    GreaterOrEqual,
    LessOrEqual,
}

[Serializable]
public enum VariableType
{
    Float,
    Int,
    Bool,
    String,
}
#endregion