using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public AudioManager audioManager;
    public DialogueCharacterManager characterManager;
    public DialogueUIManager uiManager;

    private void Awake()
    {
        // Set up singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    #region Variables
    public RuntimeDialogueGraph runtimeGraph;
    public DialogueBlackboard blackboard;

    public RuntimeNode currentNode;
    
    public bool dialogueRunning = false;

    [Header("Settings")]
    public bool awaitContinueEvent = false;
    public bool onHold = false;
    public bool allowEscape = false;
    
    private Coroutine delayNodeCoroutine;
    public bool IsDelayingNode => delayNodeCoroutine != null;
    #endregion

    #region Start
    private void Start()
    {
        CreateRuntimeBlackboard(runtimeGraph);

        // Continues dialogue when event is called
        DialogueEvents.OnContinueDialogue += GoToNextNode;
    }

    private void CreateRuntimeBlackboard(RuntimeDialogueGraph graph)
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
        blackboard = bb;
    }
    #endregion

    private void Update()
    {
        // Start the dialogue, resets previous dialogue
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartDialogue();
        }
        // Resumes dialogue
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ResumeDialogue();
        }
        // Continues the dialogue when awaitContinueEvent is true
        if (Input.GetKeyDown(KeyCode.P))
        {
            DialogueEvents.Continue();
        }

        if (onHold || awaitContinueEvent)
            return;

        if (allowEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            EndDialogue();
        }
    }

    #region Control
    public void StartDialogue()
    {
        ResetDialogue(true);

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

    public void InteruptDialogue()
    {
        if (onHold)
            return;
        uiManager.EnableDialoguePanel(false);
        audioManager.MusicState(MusicCommand.Pause);
        onHold = true;
        uiManager.StopPrinting();
        StopNodeDelay();
    }

    public void ResumeDialogue()
    {
        if (!onHold)
            return;
        onHold = false;
        if (currentNode != null)
            HandleNode(currentNode.nextNodeID);
        audioManager.MusicState(MusicCommand.Resume);
    }

    public void EndDialogue()
    {
        ResetDialogue(false);
    }

    private void ResetDialogue(bool setActive)
    {
        allowEscape = runtimeGraph.allowEscape;
        characterManager.notTalkingType = runtimeGraph.notTalkingType;

        characterManager.StopAllCharacterCoroutines();
        characterManager.RemoveAllCharacters();

        audioManager.ResetController();
        uiManager.ResetController();

        uiManager.EnableDialoguePanel(setActive);
        dialogueRunning = setActive;

        StopNodeDelay();

        currentNode = null;
    }
    #endregion

    #region Nodes
    public void HandleNode(string id)
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
                delayNodeCoroutine = StartCoroutine(DelayNextNode(node));
                break;
            case RuntimeSplitterNode node:
                SetupSplitterNode(node);
                break;
            case RuntimeInterruptNode:
                InteruptDialogue();
                return;
        }

        uiManager.EnableDialoguePanel(true);
    }

    public void GoToNextNode()
    {
        audioManager.StopAllSounds();

        if (currentNode != null && !string.IsNullOrEmpty(currentNode.nextNodeID))
            HandleNode(currentNode.nextNodeID);
        else
            EndDialogue();
    }

    public void StopNodeDelay()
    {
        if (!IsDelayingNode)
            return;

        StopCoroutine(delayNodeCoroutine);
        delayNodeCoroutine = null;
    }

    private IEnumerator DelayNextNode(RuntimeDialogueNode node)
    {
        bool keepPrevious = node.dialogueSettings.keepPreviousText;
        if (!keepPrevious)
        {
            uiManager.ClearDialogueText();
        }

        float delay = node.dialogueSettings.delayText.GetValue(blackboard);
        yield return new WaitForSeconds(delay);

        delayNodeCoroutine = null;
        SetupDialogueNode(node);
    }

    public void SetupDialogueNode(RuntimeDialogueNode node)
    {
        awaitContinueEvent = node.dialogueSettings.awaitContinueEvent;

        DialogueEvents.RaiseDialogueStarted(node.nodeID);

        // Send string via action
        if (node.dialogueSettings.broadcastString.GetValue(blackboard, out string text))
        {
            DialogueEvents.RaiseStringBroadcast(text);
        }

        int charactersTalking = characterManager.HandleCharacters(node);

        uiManager.HandleBackground(node);

        uiManager.HandleDialogueBox(node);

        uiManager.HandleDialogueText(node, charactersTalking);

        uiManager.CreateChoices(node);

        if (node.dialogueSettings.musicQueue.GetValue(blackboard, out List<AudioClip> newQueue))
        {
            bool loop = node.dialogueSettings.loop.GetValue(blackboard);
            bool shuffle = node.dialogueSettings.shuffle.GetValue(blackboard);
            audioManager.SetMusicQueue(newQueue, loop, shuffle);
        }
        
        if (node.dialogueSettings.audioList.GetValue(blackboard, out List<AudioClip> audioList))
        {
            audioManager.PlayAllSounds(audioList);
        }
    }

    private void SetupSplitterNode(RuntimeSplitterNode node)
    {
        foreach (RuntimeSplitterOutput output in node.outputs)
        {
            bool valid = true;
            foreach (ValueComparer comparison in output.comparisons)
            {
                if (!comparison.Evaluate(blackboard))
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
    #endregion
}