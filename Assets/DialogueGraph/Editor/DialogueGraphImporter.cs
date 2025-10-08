using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Imports the DialogueGraph (.dialoguegraph) editor graph into a runtime-friendly format.
/// </summary>
[ScriptedImporter(1, DialogueGraph.AssetExtension)]
public class DialogueGraphImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        DialogueGraph editorGraph = GraphDatabase.LoadGraphForImporter<DialogueGraph>(ctx.assetPath);
        if (editorGraph == null)
        {
            Debug.LogError($"Failed to load DialogueGraph at path: {ctx.assetPath}");
            return;
        }

        RuntimeDialogueGraph runtimeGraph = ScriptableObject.CreateInstance<RuntimeDialogueGraph>();
        Dictionary<INode, string> nodeIDMap = new();

        // Assign unique IDs to nodes
        foreach (INode node in editorGraph.GetNodes())
            nodeIDMap[node] = Guid.NewGuid().ToString();

        // --- Entry Node ---
        INode startNode = editorGraph.GetNodes().FirstOrDefault(n => n.GetType().Name == "StartNode");
        if (startNode != null)
        {
            IPort nextPort = startNode.GetOutputPorts().FirstOrDefault()?.firstConnectedPort;
            if (nextPort != null)
                runtimeGraph.entryNodeID = nodeIDMap[nextPort.GetNode()];
        }

        // --- Blackboard Variables ---
        foreach (var variable in editorGraph.GetVariables())
        {
            object val = null;
            variable.TryGetDefaultValue(out val);

            RuntimeVariable runtimeVar = new RuntimeVariable
            {
                name = variable.name,
                type = ConvertType(variable.dataType),
                value = val
            };

            runtimeGraph.blackboardVariables.Add(runtimeVar);
        }

        // --- Nodes ---
        foreach (INode node in editorGraph.GetNodes())
        {
            if (node is StartNode or EndNode)
                continue;

            RuntimeNode runtimeNode = null;

            switch (node)
            {
                case DialogueContextNode dialogueContextNode:
                    runtimeNode = ProcessDialogueContextNode(dialogueContextNode, nodeIDMap);
                    break;
                case SplitterContextNode splitterContextNode:
                    runtimeNode = ProcessSplitterContextNode(splitterContextNode, nodeIDMap);
                    break;
                default:
                    // Do nothing if the node type isn't recognized
                    break;
            }

            if (runtimeNode != null)
            {
                runtimeNode.nodeID = nodeIDMap[node];

                // Determine next node if there’s an “out” port
                IPort outPort = node.GetOutputPorts().FirstOrDefault(p => p.name == "out");
                if (outPort != null && outPort.firstConnectedPort != null)
                    runtimeNode.nextNodeID = nodeIDMap[outPort.firstConnectedPort.GetNode()];
                runtimeGraph.nodes.Add(runtimeNode);
            }
        }

        // --- Save asset ---
        ctx.AddObjectToAsset("RuntimeDialogueGraph", runtimeGraph);
        ctx.SetMainObject(runtimeGraph);
    }

    #region Node Processors

    private RuntimeDialogueNode ProcessDialogueContextNode(DialogueContextNode node, Dictionary<INode, string> idMap)
    {
        RuntimeDialogueNode runtimeDialogueNode = new();
        
        // Dialogue data
        runtimeDialogueNode.dialogueText = GetPortValueSafe<DialogueData>(node, DialogueContextNode.DialoguePortName).text;

        // Settings
        runtimeDialogueNode.dialogueSettings = new DialogueSettings
        {
            nextDialogueText = GetBoolOption(node, DialogueContextNode.NextDialogueTextPortName, true),
            delayWithClick = GetBoolOption(node, DialogueContextNode.DelayWithClickPortName, false),
            keepPreviousText = GetBoolOption(node, DialogueContextNode.KeepPreviousTextPortName, false),
            editSettings = GetBoolOption(node, DialogueContextNode.EditSettingsPortName, false),
            editTextSettings = GetBoolOption(node, DialogueContextNode.EditTextSettingsPortName, false),
            editEnvironmentSettings = GetBoolOption(node, DialogueContextNode.EditEnvironmentSettingsPortName, false),

            printSpeed = GetPortValueSafe<float>(node, DialogueContextNode.PrintSpeedPortName),
            delayText = GetPortValueSafe<float>(node, DialogueContextNode.DelayTextPortName),
            broadcastString = GetPortValueSafe<string>(node, DialogueContextNode.BroadcastStringPortName),

            bold = GetPortValueSafe<bool>(node, DialogueContextNode.BoldPortName),
            italic = GetPortValueSafe<bool>(node, DialogueContextNode.ItalicPortName),
            underline = GetPortValueSafe<bool>(node, DialogueContextNode.UnderlinePortName),
            font = GetPortValueSafe<TMP_FontAsset>(node, DialogueContextNode.FontPortName),
            textAlign = GetPortValueSafe<TextAlignmentOptions>(node, DialogueContextNode.TextAlignPortName),
            wrapText = GetPortValueSafe<bool>(node, DialogueContextNode.WrapTextPortName),
            
            musicQueue = GetPortValueSafe<List<AudioResource>>(node, DialogueContextNode.MusicAudioQueuePortName),
            audioList = GetPortValueSafe<List<AudioResource>>(node, DialogueContextNode.PlayAudioPortName),
            backgroundImage = GetPortValueSafe<Sprite>(node, DialogueContextNode.BackgroundImagePortName),
            backgroundTransition = GetPortValueSafe<BackgroundTransition>(node, DialogueContextNode.BackgroundTransitionPortName),
        };

        // Character block nodes data
        List<CharacterData> characters = new();
        foreach (BlockNode blockNode in node.blockNodes.OfType<CharacterBlockNode>())
        {
            characters.Add(new CharacterData
            {
                changePosition = GetBoolOption(blockNode, CharacterBlockNode.ChangePositionPortName, false),

                characterSprite = GetPortValueSafe<Sprite>(blockNode, CharacterBlockNode.CharacterSpritePortName),
                name = GetPortValueSafe<string>(blockNode, CharacterBlockNode.CharacterNamePortName),
                characterEmotion = GetPortValueSafe<CharacterEmotion>(blockNode, CharacterBlockNode.EmotionPortName),
                isVisible = GetPortValueSafe<bool>(blockNode, CharacterBlockNode.VisiblePortName),
                characterAppearanceDelay = GetPortValueSafe<float>(blockNode, CharacterBlockNode.AppearanceDelayPortName),
                isTalking = GetPortValueSafe<bool>(blockNode, CharacterBlockNode.IsTalkingPortName),
                hideName = GetPortValueSafe<bool>(blockNode, CharacterBlockNode.HideNamePortName),

                smoothMove = GetPortValueSafe<bool>(blockNode, CharacterBlockNode.SmoothMovementPortName),
                characterPosition = GetPortValueSafe<Vector2>(blockNode, CharacterBlockNode.PositionPortName),
                characterRotation = GetPortValueSafe<float>(blockNode, CharacterBlockNode.RotationPortName),
                characterScale = GetPortValueSafe<Vector2>(blockNode, CharacterBlockNode.ScalePortName),
            });
        }
        runtimeDialogueNode.characters = characters;

        // Choice block nodes data
        RuntimeChoice currentChoice = null;
        foreach (BlockNode blockNode in node.blockNodes)
        {
            switch (blockNode)
            {
                // When we hit a choice block, create a new branch to attach future comparisons to
                case ChoiceBlockNode choiceBlock:
                    {
                        IPort outPort = choiceBlock.GetOutputPorts().FirstOrDefault(p => p.name == "out");
                        string nextNodeID = outPort?.firstConnectedPort != null
                            ? idMap[outPort.firstConnectedPort.GetNode()]
                            : null;

                        currentChoice = new RuntimeChoice
                        {
                            nextNodeID = nextNodeID,
                            comparisons = new List<ValueComparer>(),
                            choiceText = GetPortValueSafe<string>(choiceBlock, ChoiceBlockNode.ChoiceTextPortName),
                            showIfConditionNotMet = GetPortValueSafe<bool>(choiceBlock, ChoiceBlockNode.ShowIfConditionNotMetName),
                        };

                        runtimeDialogueNode.choices.Add(currentChoice);
                        break;
                    }

                // Comparisons after the most recent choice block
                case CompareBlockNode compareNode:
                    {
                        if (currentChoice == null)
                        {
                            Debug.LogWarning($"CompareBlockNode found before any choice block in a DialogueContextNode. It will be ignored.");
                            continue;
                        }

                        currentChoice.comparisons.Add(new ValueComparer
                        {
                            variableType = GetVariableTypeOption(compareNode, CompareBlockNode.SelectVariableTypePortName),
                            variable = GetPortValueSafe<object>(compareNode, CompareBlockNode.VariablePortName),
                            comparisonType = GetPortValueSafe<ComparisonType>(compareNode, CompareBlockNode.ComparisonTypePortName),
                            value = GetPortValueSafe<object>(compareNode, CompareBlockNode.ValuePortName)
                        });

                        break;
                    }
            }
        }

        return runtimeDialogueNode;
    }

    private RuntimeSplitterNode ProcessSplitterContextNode(SplitterContextNode node, Dictionary<INode, string> idMap)
    {
        RuntimeSplitterNode runtimeSplitterNode = new();

        RuntimeSplitterOutput currentOutput = null;
        foreach (BlockNode blockNode in node.blockNodes)
        {
            switch (blockNode)
            {
                // When we hit an output block, create a new branch to attach future comparisons to
                case SplitterOutputBlockNode outputBlock:
                    {
                        IPort outPort = outputBlock.GetOutputPorts().FirstOrDefault(p => p.name == "out");
                        string nextNodeID = outPort?.firstConnectedPort != null
                            ? idMap[outPort.firstConnectedPort.GetNode()]
                            : null;

                        currentOutput = new RuntimeSplitterOutput
                        {
                            nextNodeID = nextNodeID,
                            comparisons = new List<ValueComparer>()
                        };

                        runtimeSplitterNode.outputs.Add(currentOutput);
                        break;
                    }

                // When we hit the default block, track it separately
                case SplitterDefaultOutputBlockNode defaultBlock:
                    {
                        IPort outPort = defaultBlock.GetOutputPorts().FirstOrDefault(p => p.name == "out");
                        string nextNodeID = outPort?.firstConnectedPort != null
                            ? idMap[outPort.firstConnectedPort.GetNode()]
                            : null;

                        runtimeSplitterNode.defaultOutputNodeID = nextNodeID;
                        break;
                    }

                // Comparisons after the most recent output block
                case CompareBlockNode compareNode:
                    {
                        if (currentOutput == null)
                        {
                            Debug.LogWarning($"CompareBlockNode found before any output block in a SplitterContextNode. It will be ignored.");
                            continue;
                        }

                        Debug.Log(GetPortValueSafe<object>(compareNode, CompareBlockNode.VariablePortName));

                        currentOutput.comparisons.Add(new ValueComparer
                        {
                            variableType = GetVariableTypeOption(compareNode, CompareBlockNode.SelectVariableTypePortName),
                            variable = GetPortValueSafe<object>(compareNode, CompareBlockNode.VariablePortName),
                            comparisonType = GetPortValueSafe<ComparisonType>(compareNode, CompareBlockNode.ComparisonTypePortName),
                            value = GetPortValueSafe<object>(compareNode, CompareBlockNode.ValuePortName)
                        });
                        break;
                    }
            }
        }

        return runtimeSplitterNode;
    }

    #endregion

    #region Helper Functions

    private VariableType GetVariableTypeOption(Node node, string name, VariableType defaultValue = VariableType.Float)
    {
        var opt = node.GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<VariableType>(out var value) ? value : defaultValue;
    }

    private bool GetBoolOption(Node node, string name, bool defaultValue = false)
    {
        var opt = node.GetNodeOptionByName(name);
        return opt != null && opt.TryGetValue<bool>(out var value) ? value : defaultValue;
    }

    private T GetPortValueSafe<T>(INode node, string portName)
    {
        var port = node.GetInputPorts().FirstOrDefault(p => p.name == portName);
        if (port != null && port.TryGetValue(out T value))
            return value;
        return default;
    }

    private VariableType ConvertType(Type type)
    {
        if (type == typeof(float)) return VariableType.Float;
        if (type == typeof(int)) return VariableType.Int;
        if (type == typeof(bool)) return VariableType.Bool;
        if (type == typeof(string)) return VariableType.String;
        return VariableType.String;
    }

    #endregion
}
