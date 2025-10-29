using System.Collections;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("System References")]
    public NodeProcessor processor;
    public AudioManager audioManager;
    public DialogueCharacterManager characterManager;
    public DialogueUIManager uiManager;
    public DialogueBoxTransitionController dialogueBoxTransitionController;

    [Header("Dialogue Data")]
    public RuntimeDialogueGraph runtimeGraph;
    public DialogueBlackboard blackboard;

    [Header("Runtime flags")]
    public bool DialogueRunning { get; private set; } = false;
    public bool awaitContinueEvent = false;
    public bool onHold = false;
    public bool allowEscape = false;
    public DialogueBoxTransition dialogueBoxTransition = DialogueBoxTransition.None;

    public bool IsDelayingNode => processor != null && processor.IsDelayingNode;
    public bool InTransition => inTransitionCoroutine != null;

    private Coroutine inTransitionCoroutine;

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

    private void Start()
    {
        if (runtimeGraph == null)
            return;

        blackboard = DialogueBlackboard.CreateRuntimeBlackboard(runtimeGraph, gameObject);
        dialogueBoxTransition = runtimeGraph.dialogueBoxTransition;
        dialogueBoxTransitionController.transitionDuration = runtimeGraph.transitionDuration;
    }

    private void Update()
    {
        if (onHold || awaitContinueEvent || InTransition) return;

        if (allowEscape && Input.GetKeyDown(KeyCode.Escape))
            EndDialogue();
    }

    private void OnDestroy()
    {
        // Avoid leaking singleton
        if (Instance == this) Instance = null;
    }

    #region Control
    public void StartDialogue()
    {
        inTransitionCoroutine = StartCoroutine(StartDialogueRoutine());
    }

    private IEnumerator StartDialogueRoutine()
    {
        uiManager.EnableDialoguePanel(true);

        if (dialogueBoxTransitionController != null)
            yield return dialogueBoxTransitionController.PlayTransition(dialogueBoxTransition, true);

        ResetDialogue(true);

        if (!string.IsNullOrEmpty(runtimeGraph.entryNodeID))
            processor.HandleNode(runtimeGraph.entryNodeID);
        else
            EndDialogue();
        inTransitionCoroutine = null;
    }

    public void InteruptDialogue()
    {
        inTransitionCoroutine = StartCoroutine(InteruptDialogueRoutine());
    }

    public IEnumerator InteruptDialogueRoutine()
    {
        if (onHold) yield break;

        if (dialogueBoxTransitionController != null)
            yield return dialogueBoxTransitionController.PlayTransition(dialogueBoxTransition, false);

        uiManager.EnableDialoguePanel(false);
        audioManager.MusicState(MusicCommand.Pause);
        onHold = true;

        uiManager.StopPrinting();
        processor.StopNodeDelay();
        inTransitionCoroutine = null;
    }

    public void ResumeDialogue()
    {
        inTransitionCoroutine = StartCoroutine(ResumeDialogueRoutine());
    }

    private IEnumerator ResumeDialogueRoutine()
    {
        if (!onHold) yield break;
        onHold = false;

        uiManager.EnableDialoguePanel(true);

        if (dialogueBoxTransitionController != null)
            yield return dialogueBoxTransitionController.PlayTransition(dialogueBoxTransition, true);

        if (processor.CurrentNode != null)
            processor.HandleNode(processor.CurrentNode.nextNodeID);

        audioManager.MusicState(MusicCommand.Resume);
        inTransitionCoroutine = null;
    }

    public void EndDialogue()
    {
        inTransitionCoroutine = StartCoroutine(EndDialogueRoutine());
    }

    private IEnumerator EndDialogueRoutine()
    {
        if (dialogueBoxTransitionController != null)
            yield return dialogueBoxTransitionController.PlayTransition(dialogueBoxTransition, false);

        ResetDialogue(false);
        inTransitionCoroutine = null;
    }


    private void ResetDialogue(bool setActive)
    {
        DialogueRunning = setActive;

        // Update runtime settings from graph
        if (runtimeGraph != null)
        {
            allowEscape = runtimeGraph.allowEscape;
            if (characterManager != null)
                characterManager.notTalkingType = runtimeGraph.notTalkingType;
        }

        // reset systems
        characterManager.StopAllCharacterCoroutines();
        characterManager.RemoveAllCharacters();

        audioManager.ResetController();
        audioManager.musicQueue.Clear();
        audioManager.handleMusicQueue = setActive;
        uiManager.ResetController();
        uiManager.EnableDialoguePanel(setActive);

        // clear processor state
        processor.ResetController();
    }
    #endregion
}