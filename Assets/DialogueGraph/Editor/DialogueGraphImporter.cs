using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        // Get entry node
        INode startNode = editorGraph.GetNodes().FirstOrDefault(n => n.GetType().Name == "StartNode");
        if (startNode != null)
        {
            IPort nextPort = startNode.GetOutputPorts().FirstOrDefault()?.firstConnectedPort;
            if (nextPort != null)
                runtimeGraph.entryNodeID = nodeIDMap[nextPort.GetNode()];
        }

        // Blackboard values
        foreach (IVariable variable in editorGraph.GetVariables())
        {
            runtimeGraph.blackboardVariables.Add(CreateBlackBoardVariable(variable));
        }

        // Process nodes
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

        // Save the asset
        ctx.AddObjectToAsset("RuntimeDialogueGraph", runtimeGraph);
        ctx.SetMainObject(runtimeGraph);
    }

    public BlackBoardVariableBase CreateBlackBoardVariable(IVariable variable)
    {
        if (variable == null)
        {
            Debug.LogError("Variable is null!");
            return null;
        }

        if (variable.dataType == null)
        {
            Debug.LogError($"Variable '{variable.name}' has no data type!");
            return null;
        }

        // Create a runtime instance of BlackBoardVariable<T>
        var genericType = typeof(BlackBoardVariable<>).MakeGenericType(variable.dataType);
        var genericObject = Activator.CreateInstance(genericType);
        if (genericObject == null)
        {
            Debug.LogError($"Failed to create BlackBoardVariable for type {variable.dataType}");
            return null;
        }

        var bbVar = genericObject as BlackBoardVariableBase;
        bbVar.name = variable.name;

        // Try to copy default value if available
        if (variable.TryGetDefaultValue(out object val) && val != null)
        {
            var valueField = genericType.GetField("Value");
            if (valueField != null)
            {
                valueField.SetValue(bbVar, val);
            }
            else
            {
                Debug.LogWarning($"No Value field found for variable {variable.name}");
            }
        }

        return bbVar;
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

                        ValueComparer comparer = BuildValueComparer(compareNode);
                        currentChoice.comparisons.Add(comparer);
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

                        ValueComparer comparer = BuildValueComparer(compareNode);
                        currentOutput.comparisons.Add(comparer);
                        break;
                    }
            }
        }

        return runtimeSplitterNode;
    }

    #endregion

    #region Helper Functions

    private ValueComparer BuildValueComparer(CompareBlockNode compareNode)
    {
        VariableType type = GetVariableTypeOption(compareNode, CompareBlockNode.SelectVariableTypePortName);
        ComparisonType comparisonType = GetPortValueSafe<ComparisonType>(compareNode, CompareBlockNode.ComparisonTypePortName);
        bool equals = GetPortValueSafe<bool>(compareNode, CompareBlockNode.EqualsPortName);

        ValueComparer comparer = new()
        {
            variableType = type,
            comparisonType = comparisonType,
            equals = equals,
        };

        // Fill in the typed variable and value fields based on the chosen type
        switch (type)
        {
            case VariableType.Bool:
                comparer.boolVariable = GetPortValueSafe<bool>(compareNode, CompareBlockNode.VariablePortName);
                comparer.boolValue = GetPortValueSafe<bool>(compareNode, CompareBlockNode.ValuePortName);
                break;
            case VariableType.String:
                comparer.stringVariable = GetPortValueSafe<string>(compareNode, CompareBlockNode.VariablePortName);
                comparer.stringValue = GetPortValueSafe<string>(compareNode, CompareBlockNode.ValuePortName);
                break;
            case VariableType.Float:
                comparer.floatVariable = GetPortValueSafe<float>(compareNode, CompareBlockNode.VariablePortName);
                comparer.floatValue = GetPortValueSafe<float>(compareNode, CompareBlockNode.ValuePortName);
                break;
            case VariableType.Int:
                comparer.intVariable = GetPortValueSafe<int>(compareNode, CompareBlockNode.VariablePortName);
                comparer.intValue = GetPortValueSafe<int>(compareNode, CompareBlockNode.ValuePortName);
                break;
        }

        return comparer;
    }

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

    #endregion
}
