using UnityEngine;
using UnityEditor.AssetImporters;
using Unity.GraphToolkit.Editor;
using System;
using System.Collections.Generic;
using System.Linq;

[ScriptedImporter(1, DialogueGraph.AssetExtension)]
public class DialogueGraphImporter : ScriptedImporter
{
    // This method is ran when an asset is created or saved (The dialogue graph)
    public override void OnImportAsset(AssetImportContext ctx)
    {
        // Get the asset which is being imported
        DialogueGraph editorGraph = GraphDatabase.LoadGraphForImporter<DialogueGraph>(ctx.assetPath);
        RuntimeDialogueGraph runtimeGraph = ScriptableObject.CreateInstance<RuntimeDialogueGraph>();
        var nodeIDMap = new Dictionary<INode, string>();

        // Assign Guid's to all nodes
        foreach (var node in editorGraph.GetNodes())
        {
            nodeIDMap[node] = Guid.NewGuid().ToString();
        }

        // Get the start node and store the connected dialogue node
        var startNode = editorGraph.GetNodes().OfType<StartNode>().FirstOrDefault();
        if (startNode != null)
        {
            var entryPort = startNode.GetOutputPorts().FirstOrDefault()?.firstConnectedPort;
            if (entryPort != null)
            {
                runtimeGraph.EntryNodeID = nodeIDMap[entryPort.GetNode()];
            }
        }

        // Processes dialogue nodes in order
        foreach (var iNode in editorGraph.GetNodes())
        {
            if (iNode is StartNode || iNode is EndNode)
                continue;

            var runtimeNode = new RuntimeDialogueNode { NodeID = nodeIDMap[iNode] };
            if (iNode is DialogueNode dialogueNode)
            {
                ProcessDialogueNode(dialogueNode, runtimeNode, nodeIDMap);
            }

            runtimeGraph.AllNodes.Add(runtimeNode);
        }

        // Attach runtime data to the graph itself so a single dialogue graph file can be dragged into an inspector field
        ctx.AddObjectToAsset("RuntimeData", runtimeGraph);
        ctx.SetMainObject(runtimeGraph);
    }

    private void ProcessDialogueNode(DialogueNode node, RuntimeDialogueNode runtimeNode, Dictionary<INode, string> nodeIDMap)
    {
        // Get all values from ports
        runtimeNode.SpeakerName = GetPortValue<string>(node.GetInputPortByName("speaker"));
        runtimeNode.DialogueText = GetPortValue<string>(node.GetInputPortByName("dialogue"));

        // Set the next node ID if available
        var nextNodePort = node.GetOutputPortByName("out")?.firstConnectedPort;
        if (nextNodePort != null)
        {
            runtimeNode.NextNodeID = nodeIDMap[nextNodePort.GetNode()];
        }
    }

    private T GetPortValue<T>(IPort port)
    {
        // If no port exists, return the default value (string = null, int = 0, etc.)
        if (port == null)
            return default;

        // If port is connected to a variable from the blackboard, get that value
        if (port.isConnected)
        {
            if (port.firstConnectedPort.GetNode() is IVariableNode variableNode)
            {
                variableNode.variable.TryGetDefaultValue(out T value);
                return value;
            }
        }

        // Get what is manually written in the node
        port.TryGetValue(out T fallbackValue);
        return fallbackValue;
    }
}
