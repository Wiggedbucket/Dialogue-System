using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public RuntimeDialogueGraph runtimeGraph;
    public DialogueBlackboard dialogueBlackboard;

    public bool dialogueRunning = false;

    [Header("UI Components")]
    public GameObject dialoguePanel;
    public Image primaryImage;
    public Image secondaryImage;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI dialogueTextShadow;

    [Header("Choice Button UI")]
    public Button choiceButtonPrefab;
    public Transform choiceButtonContainer;

    private RuntimeNode currentNode;

    [Header("Settings")]
    public bool onHold = false;
    public bool allowEscape = false;
    public bool autoAdvance = false;
    public bool enableSkipping = false;
    public bool textShadowOnMultipleCharactersTalking = false;

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
            StartDialogue();
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ResumeDialogue();
        }

        if (onHold)
            return;

        if (currentNode is RuntimeDialogueNode node && node != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && node.choices.Count == 0)
            {
                // If next node is present, show it, if not, end dialogue
                if (!string.IsNullOrEmpty(currentNode.nextNodeID))
                {
                    HandleNode(currentNode.nextNodeID);
                }
                else
                {
                    EndDialogue();
                }
            }
        }
    }

    private void StartDialogue()
    {
        dialogueRunning = true;
        allowEscape = runtimeGraph.allowEscape;
        textShadowOnMultipleCharactersTalking = runtimeGraph.textShadowOnMultipleCharactersTalking;

        // If first node is present, show it, if not, end dialogue
        if (!string.IsNullOrEmpty(runtimeGraph.entryNodeID))
        {
            HandleNode(runtimeGraph.entryNodeID);
        }
        else
        {
            EndDialogue();
        }
    }

    private void ResumeDialogue()
    {
        onHold = false;
        if (currentNode != null)
            HandleNode(currentNode.nextNodeID);
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        dialogueRunning = false;
        currentNode = null;

        // Clear all choice buttons
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void HandleNode(string id)
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
            case RuntimeInteruptNode node:
                dialoguePanel.SetActive(false);
                onHold = true;
                return;
        }

        dialoguePanel.SetActive(true);
    }

    private void SetupDialogueNode(RuntimeDialogueNode node)
    {
        // Handle character data
        List<string> speakerNames = new();
        foreach (CharacterData character in node.characters)
        {
            if (character.isTalking.GetValue(dialogueBlackboard))
            {
                speakerNames.Add(character.hideName.GetValue(dialogueBlackboard) ? "???" : character.name.GetValue(dialogueBlackboard));
            }
        }

        // Setup speaker names text
        for (int i = 0; i < speakerNames.Count; i++)
        {
            if (i == 0)
            {
                speakerNameText.text = speakerNames[i];
            }
            else if (i == speakerNames.Count - 1)
            {
                speakerNameText.text += " and " + speakerNames[i];
            }
            else
            {
                speakerNameText.text += ", " + speakerNames[i];
            }
        }

        SetDialogueText(node.dialogueText, speakerNames.Count > 1);

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
                        HandleNode(choice.nextNodeID);
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

    private void SetDialogueText(string text, bool multiCharacters)
    {
        dialogueText.text = text;

        if (multiCharacters && textShadowOnMultipleCharactersTalking)
        {
            dialogueTextShadow.text = text;
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
                HandleNode(output.nextNodeID);
                return;
            }
        }

        if (node.defaultOutputNodeID != null)
            HandleNode(node.defaultOutputNodeID);
    }
}