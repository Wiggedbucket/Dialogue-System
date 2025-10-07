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
    public List<RuntimeFunctionCall> functions = new();
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
    public bool smoothTransition;
}

[Serializable]
public class RuntimeSplitterNode : RuntimeNode
{
    public List<ValueComparer> comparisons = new();
    public string defaultOutputNodeID;
}

[Serializable]
public class RuntimeChoice
{
    public string choiceText;
    public List<ValueComparer> comparisons = new();
    public string nextNodeID;
}

[Serializable]
public class RuntimeFunctionCall
{
    public string unityEventName;
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

public enum VariableType
{
    Float,
    Int,
    Bool,
    String,
}
