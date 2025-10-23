using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Dialogue/Runtime Dialogue Graph")]
[Serializable]
public class RuntimeDialogueGraph : ScriptableObject
{
    public string entryNodeID;

    // Settings
    public bool allowEscape = false; // If true, allows exiting the dialogue at any time (e.g., pressing escape)
    public bool allowFastAdvance = true; // If true, allows advancing through text printing with left click
    public bool textShadowOnMultipleCharactersTalking = false;
    public NotTalkingType notTalkingType = NotTalkingType.None; // The effect applied to a character when not talking

    [SerializeReference]
    public List<RuntimeNode> nodes = new();
    [SerializeReference]
    public List<BlackBoardVariableBase> blackboardVariables = new();

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
public class PortValue<T>
{
    // If true, use the value, otherwise, skip over this variable
    public bool usePortValue = false;

    // If not empty, get the value from the blackboard
    public string blackboardVariableName = null;
    // The value set in the node, used if blackboardVariableName is null or empty
    public T value = default;

    public bool GetValue(DialogueBlackboard blackboard, out T portValue)
    {
        if (!string.IsNullOrEmpty(blackboardVariableName) && blackboard.TryGetValue<T>(blackboardVariableName, out var bbValue))
        {
            portValue = bbValue;
        } else
        {
            portValue = value;
        }

        return usePortValue;
    }

    public T GetValue(DialogueBlackboard blackboard)
    {
        if (!string.IsNullOrEmpty(blackboardVariableName) && blackboard.TryGetValue<T>(blackboardVariableName, out var bbValue))
        {
            return bbValue;
        }

        return value;
    }
}

[Serializable]
public class DialogueSettings
{
    // Node options
    public bool awaitContinueEvent = false;
    public bool delayNextWithClick = false;
    public bool keepPreviousText = false;

    // Variables
    public PortValue<float> printSpeed = new();
    public PortValue<float> delayText = new();
    public PortValue<string> broadcastString = new();

    public PortValue<bool> bold = new();
    public PortValue<bool> italic = new();
    public PortValue<bool> underline = new();
    public PortValue<Color> color = new();
    public PortValue<TMP_FontAsset> font = new();
    public PortValue<TextAlignmentOptions> textAlign = new();
    public PortValue<TextWrappingModes> wrapText = new();

    public PortValue<List<AudioClip>> musicQueue = new();
    public PortValue<bool> loop = new();
    public PortValue<bool> shuffle = new();
    public PortValue<List<AudioClip>> audioList = new();

    public PortValue<DialogueBoxTransition> dialogueBoxTransition = new(); // TODO Make animations
    public PortValue<Color> dialogueBoxColor = new();
    public PortValue<Sprite> dialogueBoxImage = new();
    public PortValue<Color> namePlateColor = new();
    public PortValue<Sprite> namePlateImage = new();

    public PortValue<Sprite> backgroundImage = new(); // TODO
    public PortValue<BackgroundTransition> backgroundTransition = new(); // TODO
}

[Serializable]
public class RuntimeChoice
{
    public PortValue<string> choiceText = new();
    public PortValue<bool> showIfConditionNotMet = new();
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

        switch (variableType)
        {
            case VariableType.Bool:
                return eq
                    ? boolVariable.GetValue(blackBoard) == boolValue.GetValue(blackBoard)
                    : boolVariable.GetValue(blackBoard) != boolValue.GetValue(blackBoard);

            case VariableType.String:
                return eq
                    ? stringVariable.GetValue(blackBoard) == stringValue.GetValue(blackBoard)
                    : stringVariable.GetValue(blackBoard) != stringValue.GetValue(blackBoard);

            case VariableType.Float:
                return Compare(blackBoard, floatVariable.GetValue(blackBoard), floatValue.GetValue(blackBoard));

            case VariableType.Int:
                return Compare(blackBoard, intVariable.GetValue(blackBoard), intValue.GetValue(blackBoard));

            default:
                return false;
        }
    }

    private bool Compare<T>(DialogueBlackboard blackBoard, T a, T b) where T : IComparable
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
            _ => false
        };
    }
}

[Serializable]
public class RuntimeInteruptNode : RuntimeNode
{
    
}
#endregion

#region Enums
[Serializable]
public enum NotTalkingType
{
    None,
    GreyOut,
    //ScaleDown,
}

[Serializable]
public enum PredefinedPosition
{
    None,
    Left,
    MiddleLeft,
    MiddleRight,
    Right,
}

[Serializable]
public enum MovementType
{
    Instant,
    Linear,
    Smooth
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
public enum DialogueBoxTransition
{
    None,
    FadeIn,
    SlideUp,
    SlideDown,
    SlideLeft,
    SlideRight,
    ExpandHorizontal,
    ExpandVertical,
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