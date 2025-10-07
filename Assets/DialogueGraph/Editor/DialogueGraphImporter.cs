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
                IPort outPort = node.GetOutputPortByName("out");
                if (outPort?.firstConnectedPort != null)
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
        runtimeDialogueNode.dialogueText = GetPortValueSafe<DialogueData>(node, DialogueContextNode.DialogueName).text;

        // Settings
        runtimeDialogueNode.dialogueSettings = new DialogueSettings
        {
            nextDialogueText = GetNodeOptionValue(node, DialogueContextNode.NextDialogueTextName, true),
            delayWithClick = GetNodeOptionValue(node, DialogueContextNode.DelayWithClickName, false),
            keepPreviousText = GetNodeOptionValue(node, DialogueContextNode.KeepPreviousTextName, false),
            editSettings = GetNodeOptionValue(node, DialogueContextNode.EditSettingsName, false),
            editTextSettings = GetNodeOptionValue(node, DialogueContextNode.EditTextSettingsName, false),
            editEnvironmentSettings = GetNodeOptionValue(node, DialogueContextNode.EditEnvironmentSettingsName, false),

            printSpeed = GetPortValueSafe<float>(node, DialogueContextNode.PrintSpeedName),
            delayText = GetPortValueSafe<float>(node, DialogueContextNode.DelayTextName),
            broadcastString = GetPortValueSafe<string>(node, DialogueContextNode.BroadcastStringName),

            bold = GetPortValueSafe<bool>(node, DialogueContextNode.BoldName),
            italic = GetPortValueSafe<bool>(node, DialogueContextNode.ItalicName),
            underline = GetPortValueSafe<bool>(node, DialogueContextNode.UnderlineName),
            font = GetPortValueSafe<TMP_FontAsset>(node, DialogueContextNode.FontName),
            textAlign = GetPortValueSafe<TextAlignmentOptions>(node, DialogueContextNode.TextAlignName),
            wrapText = GetPortValueSafe<bool>(node, DialogueContextNode.WrapTextName),
            
            musicQueue = GetPortValueSafe<List<AudioResource>>(node, DialogueContextNode.MusicAudioQueueName),
            audioList = GetPortValueSafe<List<AudioResource>>(node, DialogueContextNode.PlayAudioName),
            backgroundImage = GetPortValueSafe<Sprite>(node, DialogueContextNode.BackgroundImageName),
            smoothTransition = GetPortValueSafe<bool>(node, DialogueContextNode.SmoothBackgroundTransitionName),
        };

        // Character block nodes data
        List<CharacterData> characters = new();
        foreach (BlockNode blockNode in node.blockNodes)
        {
            if (blockNode is not CharacterBlockNode)
                continue;

            characters.Add(new CharacterData
            {
                changePosition = GetNodeOptionValue(blockNode, CharacterBlockNode.ChangePositionName, false),

                characterSprite = GetPortValueSafe<Sprite>(blockNode, CharacterBlockNode.CharacterSpriteName),
                name = GetPortValueSafe<string>(blockNode, CharacterBlockNode.CharacterNameName),
                characterEmotion = GetPortValueSafe<CharacterEmotion>(blockNode, CharacterBlockNode.EmotionName),
                isVisible = GetPortValueSafe<bool>(blockNode, CharacterBlockNode.VisibleName),
                characterAppearanceDelay = GetPortValueSafe<float>(blockNode, CharacterBlockNode.AppearanceDelayName),
                isTalking = GetPortValueSafe<bool>(blockNode, CharacterBlockNode.IsTalkingName),
                hideName = GetPortValueSafe<bool>(blockNode, CharacterBlockNode.HideNameName),

                smoothMove = GetPortValueSafe<bool>(blockNode, CharacterBlockNode.SmoothMovementName),
                characterPosition = GetPortValueSafe<Vector2>(blockNode, CharacterBlockNode.PositionName),
                characterRotation = GetPortValueSafe<float>(blockNode, CharacterBlockNode.RotationName),
                characterScale = GetPortValueSafe<Vector2>(blockNode, CharacterBlockNode.ScaleName),
            });
        }
        runtimeDialogueNode.characters = characters;

        // First choice block node data
        // TODO: Split the choices up into their own block nodes instead of all in one because it would be way easier that way since it's with context nodes
        List<RuntimeChoice> choices = new();
        List<DialogueContextNode> contextNodes = new();
        ChoiceBlockNode choiceBlockNode = node.blockNodes
            .OfType<ChoiceBlockNode>()
            .First();

        if (choiceBlockNode != null)
        {
            // Gets all output ports for the choices
            IEnumerable<IPort> choiceOutputPorts = choiceBlockNode.GetOutputPorts().Where(p => p.name.StartsWith("choice "));
            foreach (IPort outputPort in choiceOutputPorts)
            {
                // Gets choice index and gets the text and conditions ports with it
                string index = outputPort.name.Substring("choice ".Length);
                IPort textPort = choiceBlockNode.GetInputPortByName($"choice text {index}");
                IPort conditionsPort = choiceBlockNode.GetInputPortByName($"conditions choice {index}");

                // Adds the choices to the list
                RuntimeChoice choiceData = new RuntimeChoice
                {
                    choiceText = GetPortValueSafe<string>(choiceBlockNode, $"choice text {index}"),
                    // Get the conditions from the conditions context node
                    nextNodeID = outputPort.firstConnectedPort != null ? idMap[outputPort.firstConnectedPort.GetNode()] : null,
                };
                choices.Add(choiceData);
            }

            runtimeDialogueNode.choices = choices;
        }

        return runtimeDialogueNode;
    }

    private RuntimeSplitterNode ProcessSplitterContextNode(SplitterContextNode node, Dictionary<INode, string> idMap)
    {
        var runtime = new RuntimeSplitterNode();

        //// Default output
        //var defaultOut = node.GetOutputPortByName("out")?.firstConnectedPort;
        //runtime.defaultOutputNodeID = defaultOut != null ? idMap[defaultOut.GetNode()] : null;

        //// Comparisons (if present)
        //var compPorts = node.GetOutputPorts().Where(p => p.name.StartsWith("if "));
        //foreach (var port in compPorts)
        //{
        //    var comp = new ValueComparer
        //    {
        //        variableName = GetPortValueSafe<string>(node, "variable"),
        //        comparisonType = GetPortValueSafe<ComparisonType>(node, "comparison type"),
        //        compareValue = GetPortValueSafe<object>(node, "value"),
        //        outputNodeID = port.firstConnectedPort != null ? idMap[port.firstConnectedPort.GetNode()] : null
        //    };
        //    runtime.comparisons.Add(comp);
        //}

        return runtime;
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
