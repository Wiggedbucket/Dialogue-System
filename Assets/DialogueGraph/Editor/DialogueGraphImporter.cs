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
                case ConditionContextNode:
                    // Get's processed in the dialogue context node
                    break;
                default:
                    Debug.LogWarning($"Unrecognized node type: {node}");
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
            nextDialogueText = GetNodeOptionValue(node, DialogueContextNode.NextDialogueTextPortName, true),
            delayWithClick = GetNodeOptionValue(node, DialogueContextNode.DelayWithClickPortName, false),
            keepPreviousText = GetNodeOptionValue(node, DialogueContextNode.KeepPreviousTextPortName, false),
            editSettings = GetNodeOptionValue(node, DialogueContextNode.EditSettingsPortName, false),
            editTextSettings = GetNodeOptionValue(node, DialogueContextNode.EditTextSettingsPortName, false),
            editEnvironmentSettings = GetNodeOptionValue(node, DialogueContextNode.EditEnvironmentSettingsPortName, false),

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
                changePosition = GetNodeOptionValue(blockNode, CharacterBlockNode.ChangePositionPortName, false),

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
        List<RuntimeChoice> choices = new();
        foreach (BlockNode blockNode in node.blockNodes.OfType<ChoiceBlockNode>())
        {
            IPort conditionPort = blockNode.GetInputPortByName(ChoiceBlockNode.ConditionsChoicePortName).firstConnectedPort;
            IPort outputPort = blockNode.GetOutputPortByName(ChoiceBlockNode.ChoicePortName);

            List<ValueComparer> comparisons = conditionPort != null && conditionPort.GetNode() is ConditionContextNode conditionNode
                ? ProcessConditionContextNode(conditionNode, idMap)
                : new List<ValueComparer>();

            choices.Add(new RuntimeChoice
            {
                choiceText = GetPortValueSafe<string>(blockNode, ChoiceBlockNode.ChoiceTextPortName),
                comparisons = comparisons,
                nextNodeID = outputPort.firstConnectedPort != null ? idMap[outputPort.firstConnectedPort.GetNode()] : null,
            });
        }
        runtimeDialogueNode.choices = choices;

        return runtimeDialogueNode;
    }

    private List<ValueComparer> ProcessConditionContextNode(ConditionContextNode node, Dictionary<INode, string> idMap)
    {
        List<ValueComparer> comparisons = new();
        foreach (BlockNode blockNode in node.blockNodes.OfType<CompareBlockNode>())
        {
            ValueComparer comparer = new ValueComparer
            {
                variable = GetPortValueSafe<string>(blockNode, CompareBlockNode.VariablePortName),
                comparison = GetPortValueSafe<ComparisonType>(blockNode, CompareBlockNode.ComparisonTypePortName),
                value = GetPortValueSafe<object>(blockNode, CompareBlockNode.ValuePortName),
            };
            comparisons.Add(comparer);
        }
        return comparisons;
    }

    private RuntimeSplitterNode ProcessSplitterContextNode(SplitterContextNode node, Dictionary<INode, string> idMap)
    {
        RuntimeSplitterNode runtimeSplitterNode = new();

        // Temporary storage for CompareBlockNodes that appear before an output
        List<ValueComparer> currentComparisons = new();

        foreach (BlockNode blockNode in node.blockNodes)
        {
            switch (blockNode)
            {
                // Collect comparisons
                case CompareBlockNode compareNode:
                    currentComparisons.Add(new ValueComparer
                    {
                        variable = GetPortValueSafe<string>(compareNode, CompareBlockNode.VariablePortName),
                        comparison = GetPortValueSafe<ComparisonType>(compareNode, CompareBlockNode.ComparisonTypePortName),
                        value = GetPortValueSafe<object>(compareNode, CompareBlockNode.ValuePortName)
                    });
                    break;

                // When we hit an output block, we create a new branch
                case SplitterOutputBlockNode outputBlock:
                    {
                        IPort outPort = outputBlock.GetOutputPorts().FirstOrDefault(p => p.name == "out");
                        string nextNodeID = outPort?.firstConnectedPort != null
                            ? idMap[outPort.firstConnectedPort.GetNode()]
                            : null;

                        // Store the collected comparisons for this branch
                        runtimeSplitterNode.outputs.Add(new RuntimeSplitterOutput
                        {
                            nextNodeID = nextNodeID,
                            comparisons = new List<ValueComparer>(currentComparisons)
                        });

                        // Reset comparisons for the next branch
                        currentComparisons.Clear();
                        break;
                    }

                // Handle the default output
                case SplitterDefaultOutputBlockNode defaultBlock:
                    {
                        IPort outPort = defaultBlock.GetOutputPorts().FirstOrDefault(p => p.name == "out");
                        if (outPort?.firstConnectedPort != null)
                            runtimeSplitterNode.defaultOutputNodeID = idMap[outPort.firstConnectedPort.GetNode()];

                        currentComparisons.Clear();
                        break;
                    }
            }
        }

        return runtimeSplitterNode;
    }

    #endregion

    #region Helper Functions

    private T GetPortValueSafe<T>(INode node, string portName)
    {
        var port = node.GetInputPorts().FirstOrDefault(p => p.name == portName);
        if (port != null && port.TryGetValue(out T value))
            return value;
        return default;
    }

    private T GetNodeOptionValue<T>(INode node, string optionName, T defaultValue = default)
    {
        if (node == null)
            return defaultValue;

        var method = node.GetType().GetMethod("GetNodeOptionByName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null)
            return defaultValue;

        var opt = method.Invoke(node, new object[] { optionName });
        if (opt == null)
            return defaultValue;

        var tryGetValue = opt.GetType().GetMethod("TryGetValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (tryGetValue == null)
            return defaultValue;

        try
        {
            var generic = tryGetValue.IsGenericMethodDefinition ? tryGetValue.MakeGenericMethod(typeof(T)) : tryGetValue;
            object[] args = new object[] { default(T) };
            bool success = (bool)generic.Invoke(opt, args);
            return success ? (T)args[0] : defaultValue;
        }
        catch
        {
            return defaultValue;
        }
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
