using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public RuntimeDialogueGraph runtimeGraph;
    public DialogueBlackboard dialogueBlackboard;

    [Header("UI Components")]
    public GameObject dialoguePanel;
    public Image primaryImage;
    public Image secondaryImage;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    [Header("Choice Button UI")]
    public Button choiceButtonPrefab;
    public Transform choiceButtonContainer;

    private RuntimeNode currentNode;

    private void Start()
    {
        CreateRuntimeBlackboard(runtimeGraph);
    }

    public void CreateRuntimeBlackboard(RuntimeDialogueGraph graph)
    {
        GameObject go = new("DialogueBlackboard");
        DialogueBlackboard bb = go.AddComponent<DialogueBlackboard>();
        bb.variables = new List<BlackBoardVariableBase>();

        foreach (BlackBoardVariableBase v in graph.blackboardVariables)
        {
            // Copy the variable
            var type = v.GetType();
            var clone = Activator.CreateInstance(type) as BlackBoardVariableBase;
            
            clone.name = v.name;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BlackBoardVariable<>))
            {
                var valueField = type.GetField("Value");
                valueField.SetValue(clone, valueField.GetValue(v));
            }

            bb.variables.Add(clone);
        }

        bb.SetupVariableMap();
        dialogueBlackboard = bb;
    }

    private void Update()
    {
        // Start the dialogue, resets previous dialogue
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetupNodes();
        }

        if (currentNode is RuntimeDialogueNode node && node != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && node.choices.Count == 0)
            {
                // If next node is present, show it, if not, end dialogue
                if (!string.IsNullOrEmpty(currentNode.nextNodeID))
                {
                    ShowNode(currentNode.nextNodeID);
                }
                else
                {
                    EndDialogue();
                }
            }
        }
    }

    private void SetupNodes()
    {
        // If first node is present, show it, if not, end dialogue
        if (!string.IsNullOrEmpty(runtimeGraph.entryNodeID))
        {
            ShowNode(runtimeGraph.entryNodeID);
        }
        else
        {
            EndDialogue();
        }
    }
    
    private void ShowNode(string id)
    {
        RuntimeNode runtimeNode = runtimeGraph.GetNode(id);

        // If node doesn't exist, end dialogue
        if (runtimeNode == null)
        {
            EndDialogue();
            return;
        }

        // Handle the node
        currentNode = runtimeNode;
        switch (currentNode)
        {
            case RuntimeDialogueNode node:
                SetupDialogueNode(node);
                break;
            case RuntimeSplitterNode node:
                SetupSplitterNode(node);
                break;
        }

        dialoguePanel.SetActive(true);
    }

    private void SetupDialogueNode(RuntimeDialogueNode node)
    {
        dialogueText.text = node.dialogueText;

        // Clear all choice buttons
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // Setup choice buttons
        if (node.choices.Count > 0)
        {
            foreach (RuntimeChoice choice in node.choices)
            {
                Button button = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

                buttonText.SetText(choice.choiceText.value);

                button.onClick.AddListener(() =>
                {
                    if (!string.IsNullOrEmpty(choice.nextNodeID))
                    {
                        ShowNode(choice.nextNodeID);
                    }
                    else
                    {
                        EndDialogue();
                    }
                });

                bool valid = true;
                foreach (ValueComparer comparison in choice.comparisons)
                {
                    if (!comparison.Evaluate(dialogueBlackboard))
                        valid = false;
                }

                if (!valid)
                {
                    button.interactable = false;
                    button.gameObject.SetActive(choice.showIfConditionNotMet.GetValue(dialogueBlackboard));
                }
            }
        }
    }

    private void SetupSplitterNode(RuntimeSplitterNode node)
    {
        foreach (RuntimeSplitterOutput output in node.outputs)
        {
            bool valid = true;
            foreach (ValueComparer comparison in output.comparisons)
            {
                if (!comparison.Evaluate(dialogueBlackboard))
                    valid = false;
            }

            if (valid)
            {
                ShowNode(output.nextNodeID);
                return;
            }
        }

        if (node.defaultOutputNodeID != null)
            ShowNode(node.defaultOutputNodeID);
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        currentNode = null;

        // Clear all choice buttons
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }
}