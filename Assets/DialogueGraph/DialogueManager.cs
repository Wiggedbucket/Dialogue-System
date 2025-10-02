using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public RuntimeDialogueGraph RuntimeGraph;

    [Header("UI Components")]
    public GameObject DialoguePanel;
    public TextMeshProUGUI SpeakerNameText;
    public TextMeshProUGUI DialogueText;

    [Header("Choice Button UI")]
    public Button ChoiceButtonPrefab;
    public Transform ChoiceButtonContainer;

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
        // If left mouse button is pressed and current node has no choices, go to next node or end dialogue
        if (Mouse.current.leftButton.wasPressedThisFrame && _currentNode != null && _currentNode.Choices.Count == 0)
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

        // Clear all existing choice buttons
        foreach (Transform child in ChoiceButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // If there are choices, create buttons for each choice and set up their listeners
        if (_currentNode.Choices.Count > 0)
        {
            foreach (var choice in _currentNode.Choices)
            {
                Button button = Instantiate(ChoiceButtonPrefab, ChoiceButtonContainer);

                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.SetText(choice.ChoiceText);
                }

                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        if (!string.IsNullOrEmpty(choice.DestinationNodeID))
                        {
                            ShowNode(choice.DestinationNodeID);
                        }
                        else
                        {
                            EndDialogue();
                        }
                    });
                }
            }
        }
    }

    private void EndDialogue()
    {
        DialoguePanel.SetActive(false);
        _currentNode = null;

        // Clear all choice buttons
        foreach (Transform child in ChoiceButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
