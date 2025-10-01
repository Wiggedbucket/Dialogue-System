using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public RuntimeDialogueGraph RuntimeGraph;

    [Header("UI Components")]
    public GameObject DialoguePanel;
    public TextMeshProUGUI SpeakerNameText;
    public TextMeshProUGUI DialogueText;

    private Dictionary<string, RuntimeDialogueNode> _nodeLookup = new Dictionary<string, RuntimeDialogueNode>();
    private RuntimeDialogueNode _currentNode;

    private void Start()
    {
        foreach (var node in RuntimeGraph.AllNodes)
        {
            _nodeLookup[node.NodeID] = node;
        }

        // If first node is present, show it, if not, end dialogue
        if (!string.IsNullOrEmpty(RuntimeGraph.EntryNodeID))
        {
            ShowNode(RuntimeGraph.EntryNodeID);
        } else
        {
            EndDialogue();
        }
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && _currentNode != null)
        {
            // If next node is present, show it, if not, end dialogue
            if (!string.IsNullOrEmpty(_currentNode.NextNodeID))
            {
                ShowNode(_currentNode.NextNodeID);
            }
            else
            {
                EndDialogue();
            }
        }
    }

    private void ShowNode(string nodeID)
    {
        // If node doesn't exist, end dialogue
        if (!_nodeLookup.ContainsKey(nodeID))
        {
            EndDialogue();
            return;
        }

        // Show current node in dialogue panel
        _currentNode = _nodeLookup[nodeID];

        DialoguePanel.SetActive(true);
        SpeakerNameText.SetText(_currentNode.SpeakerName);
        DialogueText.SetText(_currentNode.DialogueText);
    }

    private void EndDialogue()
    {
        DialoguePanel.SetActive(false);
        _currentNode = null;
    }
}
