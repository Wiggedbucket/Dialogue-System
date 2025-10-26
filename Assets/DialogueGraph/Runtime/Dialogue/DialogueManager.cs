using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("System References")]
    public NodeProcessor processor;
    public AudioManager audioManager;
    public DialogueCharacterManager characterManager;
    public DialogueUIManager uiManager;

    [Header("Dialogue Data")]
    public RuntimeDialogueGraph runtimeGraph;
    public DialogueBlackboard blackboard;

    [Header("Runtime flags")]
    public bool dialogueRunning { get; private set; } = false;
    public bool awaitContinueEvent = false;
    public bool onHold = false;
    public bool allowEscape = false;

    public bool IsDelayingNode => processor != null && processor.IsDelayingNode;

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
        if (runtimeGraph != null)
            blackboard = DialogueBlackboard.CreateRuntimeBlackboard(runtimeGraph, gameObject);
    }

    private void Update()
    {
        // Debug input for testing
        if (Input.GetKeyDown(KeyCode.Space)) StartDialogue();
        if (Input.GetKeyDown(KeyCode.Return)) ResumeDialogue();
        if (Input.GetKeyDown(KeyCode.P)) DialogueEvents.Continue();

        if (onHold || awaitContinueEvent) return;

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
        ResetDialogue(true);

        // If first node is present, show it, if not, end dialogue
        if (!string.IsNullOrEmpty(runtimeGraph?.entryNodeID))
            processor.HandleNode(runtimeGraph.entryNodeID);
        else
            EndDialogue();
    }

    public void InteruptDialogue()
    {
        if (onHold) return;

        uiManager.EnableDialoguePanel(false);
        audioManager?.MusicState(MusicCommand.Pause);
        onHold = true;

        uiManager?.StopPrinting();
        processor?.StopNodeDelay();
    }

    public void ResumeDialogue()
    {
        if (!onHold) return;

        onHold = false;

        if (processor?.currentNode != null)
            processor.HandleNode(processor.currentNode.nextNodeID);

        audioManager?.MusicState(MusicCommand.Resume);
    }

    public void EndDialogue()
    {
        ResetDialogue(false);
    }

    private void ResetDialogue(bool setActive)
    {
        dialogueRunning = setActive;

        // Update runtime settings from graph
        if (runtimeGraph != null)
        {
            allowEscape = runtimeGraph.allowEscape;
            if (characterManager != null)
                characterManager.notTalkingType = runtimeGraph.notTalkingType;
        }

        // reset systems
        characterManager?.StopAllCharacterCoroutines();
        characterManager?.RemoveAllCharacters();

        audioManager?.ResetController();
        uiManager?.ResetController();
        uiManager?.EnableDialoguePanel(setActive);

        // clear processor state
        processor?.ResetController();
    }
    #endregion
}