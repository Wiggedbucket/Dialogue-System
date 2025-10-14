using System;
using System.Collections.Generic;
using System.Linq;
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
            awaitContinueEvent = GetBoolOption(node, DialogueContextNode.AwaitContinueEventPortName, false),
            delayWithClick = GetBoolOption(node, DialogueContextNode.DelayWithClickPortName, false),
            keepPreviousText = GetBoolOption(node, DialogueContextNode.KeepPreviousTextPortName, false),

            printSpeed = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.PrintSpeedPortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.ChangePrintSpeedPortName),
                value = GetPortValueSafe<float>(node, DialogueContextNode.PrintSpeedPortName),
            },
            delayText = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.DelayTextPortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.ActivateTextDelayPortName),
                value = GetPortValueSafe<float>(node, DialogueContextNode.DelayTextPortName),
            },
            broadcastString = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.BroadcastStringPortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.ActivateBroadcastStringPortName),
                value = GetPortValueSafe<string>(node, DialogueContextNode.BroadcastStringPortName),
            },

            bold = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.BoldPortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.ChangeBoldPortName),
                value = GetPortValueSafe<bool>(node, DialogueContextNode.BoldPortName),
            },
            italic = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.ItalicPortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.ChangeItalicPortName),
                value = GetPortValueSafe<bool>(node, DialogueContextNode.ItalicPortName),
            },
            underline = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.UnderlinePortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.ChangeUnderlinePortName),
                value = GetPortValueSafe<bool>(node, DialogueContextNode.UnderlinePortName),
            },
            color = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.ColorPortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.ChangeTextColorPortName),
                value = GetPortValueSafe<Color>(node, DialogueContextNode.ColorPortName),
            },
            font = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.FontPortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.ChangeFontPortName),
                value = GetPortValueSafe<TMP_FontAsset>(node, DialogueContextNode.FontPortName),
            },
            textAlign = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.TextAlignPortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.ChangeTextAlignPortName),
                value = GetPortValueSafe<TextAlignmentOptions>(node, DialogueContextNode.TextAlignPortName),
            },
            wrapText = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.WrapTextPortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.ChangeWrapTextPortName),
                value = GetPortValueSafe<bool>(node, DialogueContextNode.WrapTextPortName),
            },

            musicQueue = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.MusicAudioQueuePortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.ChangeMusicAudioQueuePortName),
                value = GetPortValueSafe<List<AudioResource>>(node, DialogueContextNode.MusicAudioQueuePortName),
            },
            audioList = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.PlayAudioPortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.SetPlayAudioPortName),
                value = GetPortValueSafe<List<AudioResource>>(node, DialogueContextNode.PlayAudioPortName),
            },
            backgroundImage = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.BackgroundImagePortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.ChangeBackgroundImagePortName),
                value = GetPortValueSafe<Sprite>(node, DialogueContextNode.BackgroundImagePortName),
            },
            backgroundTransition = new()
            {
                blackboardVariableName = TryGetVariableName(node, DialogueContextNode.BackgroundTransitionPortName),
                usePortValue = GetBoolOption(node, DialogueContextNode.ChangeBackgroundTransitionPortName),
                value = GetPortValueSafe<BackgroundTransition>(node, DialogueContextNode.BackgroundTransitionPortName),
            },
        };

        // Character block nodes data
        List<CharacterData> characters = new();
        foreach (BlockNode blockNode in node.blockNodes.OfType<CharacterBlockNode>())
        {
            characters.Add(new CharacterData
            {
                name = new()
                {
                    blackboardVariableName = TryGetVariableName(blockNode, CharacterBlockNode.CharacterNamePortName),
                    usePortValue = true,
                    value = GetPortValueSafe<string>(blockNode, CharacterBlockNode.CharacterNamePortName),
                },

                characterSprite = new()
                {
                    blackboardVariableName = TryGetVariableName(blockNode, CharacterBlockNode.CharacterSpritePortName),
                    usePortValue = GetBoolOption(blockNode, CharacterBlockNode.ChangeSpritePortName),
                    value = GetPortValueSafe<Sprite>(blockNode, CharacterBlockNode.CharacterSpritePortName),
                },
                characterEmotion = new()
                {
                    blackboardVariableName = TryGetVariableName(blockNode, CharacterBlockNode.EmotionPortName),
                    usePortValue = GetBoolOption(blockNode, CharacterBlockNode.ChangeEmotionPortName),
                    value = GetPortValueSafe<CharacterEmotion>(blockNode, CharacterBlockNode.EmotionPortName),
                },
                isVisible = new()
                {
                    blackboardVariableName = TryGetVariableName(blockNode, CharacterBlockNode.VisiblePortName),
                    usePortValue = GetBoolOption(blockNode, CharacterBlockNode.ChangeVisibilityPortName),
                    value = GetPortValueSafe<bool>(blockNode, CharacterBlockNode.VisiblePortName),
                },
                characterAppearanceDelay = new()
                {
                    blackboardVariableName = TryGetVariableName(blockNode, CharacterBlockNode.AppearanceDelayPortName),
                    usePortValue = GetBoolOption(blockNode, CharacterBlockNode.ChangeAppearanceDelayPortName),
                    value = GetPortValueSafe<float>(blockNode, CharacterBlockNode.AppearanceDelayPortName),
                },
                isTalking = new()
                {
                    blackboardVariableName = TryGetVariableName(blockNode, CharacterBlockNode.IsTalkingPortName),
                    usePortValue = GetBoolOption(blockNode, CharacterBlockNode.ChangeIsTalkingPortName),
                    value = GetPortValueSafe<bool>(blockNode, CharacterBlockNode.IsTalkingPortName),
                },
                hideName = new()
                {
                    blackboardVariableName = TryGetVariableName(blockNode, CharacterBlockNode.HideNamePortName),
                    usePortValue = GetBoolOption(blockNode, CharacterBlockNode.ChangeHideNamePortName),
                    value = GetPortValueSafe<bool>(blockNode, CharacterBlockNode.HideNamePortName),
                },

                smoothMove = new()
                {
                    blackboardVariableName = TryGetVariableName(blockNode, CharacterBlockNode.SmoothMovementPortName),
                    usePortValue = GetBoolOption(blockNode, CharacterBlockNode.ChangeSmoothMovementPortName),
                    value = GetPortValueSafe<bool>(blockNode, CharacterBlockNode.SmoothMovementPortName),
                },
                characterPosition = new()
                {
                    blackboardVariableName = TryGetVariableName(blockNode, CharacterBlockNode.PositionPortName),
                    usePortValue = GetBoolOption(blockNode, CharacterBlockNode.ChangePositionPortName),
                    value = GetPortValueSafe<Vector2>(blockNode, CharacterBlockNode.PositionPortName),
                },
                characterRotation = new() 
                {
                    blackboardVariableName = TryGetVariableName(blockNode, CharacterBlockNode.RotationPortName),
                    usePortValue = GetBoolOption(blockNode, CharacterBlockNode.ChangeRotationPortName),
                    value = GetPortValueSafe<float>(blockNode, CharacterBlockNode.RotationPortName),
                },
                characterScale = new()
                {
                    blackboardVariableName = TryGetVariableName(blockNode, CharacterBlockNode.ScalePortName),
                    usePortValue = GetBoolOption(blockNode, CharacterBlockNode.ChangeScalePortName),
                    value = GetPortValueSafe<Vector2>(blockNode, CharacterBlockNode.ScalePortName),
                }
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

                            choiceText = new()
                            {
                                blackboardVariableName = TryGetVariableName(choiceBlock, ChoiceBlockNode.ChoiceTextPortName),
                                usePortValue = true,
                                value = GetPortValueSafe<string>(choiceBlock, ChoiceBlockNode.ChoiceTextPortName),
                            },
                            showIfConditionNotMet = new()
                            {
                                blackboardVariableName = TryGetVariableName(choiceBlock, ChoiceBlockNode.ShowIfConditionNotMetName),
                                usePortValue = true,
                                value = GetPortValueSafe<bool>(choiceBlock, ChoiceBlockNode.ShowIfConditionNotMetName),
                            },
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
            comparisonType = new()
            {
                blackboardVariableName = TryGetVariableName(compareNode, CompareBlockNode.ComparisonTypePortName),
                usePortValue = true,
                value = comparisonType,
            },
            equals = new()
            {
                blackboardVariableName = TryGetVariableName(compareNode, CompareBlockNode.EqualsPortName),
                usePortValue = true,
                value = equals,
            },
        };

        // Fill in the typed variable and value fields based on the chosen type
        switch (type)
        {
            case VariableType.Bool:
                comparer.boolVariable = new()
                {
                    blackboardVariableName = TryGetVariableName(compareNode, CompareBlockNode.VariablePortName),
                    usePortValue = true,
                    value = GetPortValueSafe<bool>(compareNode, CompareBlockNode.VariablePortName),
                };
                comparer.boolValue = new()
                {
                    blackboardVariableName = TryGetVariableName(compareNode, CompareBlockNode.ValuePortName),
                    usePortValue = true,
                    value = GetPortValueSafe<bool>(compareNode, CompareBlockNode.ValuePortName),
                };
                break;
            case VariableType.String:
                comparer.stringVariable = new()
                {
                    blackboardVariableName = TryGetVariableName(compareNode, CompareBlockNode.VariablePortName),
                    usePortValue = true,
                    value = GetPortValueSafe<string>(compareNode, CompareBlockNode.VariablePortName),
                };
                comparer.stringValue = new()
                {
                    blackboardVariableName = TryGetVariableName(compareNode, CompareBlockNode.ValuePortName),
                    usePortValue = true,
                    value = GetPortValueSafe<string>(compareNode, CompareBlockNode.ValuePortName),
                };
                break;
            case VariableType.Float:
                comparer.floatVariable = new()
                {
                    blackboardVariableName = TryGetVariableName(compareNode, CompareBlockNode.VariablePortName),
                    usePortValue = true,
                    value = GetPortValueSafe<float>(compareNode, CompareBlockNode.VariablePortName),
                };
                comparer.floatValue = new()
                {
                    blackboardVariableName = TryGetVariableName(compareNode, CompareBlockNode.ValuePortName),
                    usePortValue = true,
                    value = GetPortValueSafe<float>(compareNode, CompareBlockNode.ValuePortName),
                };
                break;
            case VariableType.Int:
                comparer.intVariable = new()
                {
                    blackboardVariableName = TryGetVariableName(compareNode, CompareBlockNode.VariablePortName),
                    usePortValue = true,
                    value = GetPortValueSafe<int>(compareNode, CompareBlockNode.VariablePortName),
                };
                comparer.intValue = new()
                {
                    blackboardVariableName = TryGetVariableName(compareNode, CompareBlockNode.ValuePortName),
                    usePortValue = true,
                    value = GetPortValueSafe<int>(compareNode, CompareBlockNode.ValuePortName),
                };
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

    private string TryGetVariableName(INode node, string portName)
    {
        var port = node.GetInputPorts().FirstOrDefault(p => p.name == portName);

        if (port == null || port.firstConnectedPort == null)
            return null;

        // Return the variable name if connected to a variable node
        INode connectedNode = port.firstConnectedPort.GetNode();
        if (connectedNode is IVariableNode variableNode)
            return variableNode.variable.name;

        // Otherwise, return null
        return null;
    }

    #endregion
}
