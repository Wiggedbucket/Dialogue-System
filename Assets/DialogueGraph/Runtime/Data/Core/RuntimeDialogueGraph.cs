using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[Serializable]
public class RuntimeDialogueGraph : ScriptableObject
{
    public string entryNodeID;

    // Settings
    public bool allowEscape = false; // If true, allows exiting the dialogue at any time (e.g., pressing escape)
    public bool allowFastAdvance = true; // If true, allows advancing through text printing with left click
    public bool textShadowOnMultipleCharactersTalking = false;
    public NotTalkingType notTalkingType = NotTalkingType.None; // The effect applied to a character when not talking
    public DialogueBoxTransition dialogueBoxTransition = DialogueBoxTransition.None;

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