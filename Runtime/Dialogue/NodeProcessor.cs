using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeProcessor : MonoBehaviour
{
    // References to external systems
    private DialogueManager DialogueManager => DialogueManager.Instance;
    private DialogueBlackboard Blackboard => DialogueBlackboard.Instance;
    private RuntimeDialogueGraph RuntimeGraph => DialogueManager.runtimeGraph;

    public RuntimeNode CurrentNode { get; private set; }

    private DialogueUIManager UIManager => DialogueManager.uiManager;
    private DialogueCharacterManager CharacterManager => DialogueManager.characterManager;
    private AudioManager AudioManager => DialogueManager.audioManager;

    private Coroutine delayNodeCoroutine;
    public bool IsDelayingNode => delayNodeCoroutine != null;

    private void Start()
    {
        // Continues dialogue when event is called
        DialogueEvents.OnContinueDialogue += GoToNextNode;
    }

    private void OnDestroy()
    {
        DialogueEvents.OnContinueDialogue -= GoToNextNode;
    }

    public void ResetController()
    {
        StopNodeDelay();
        CurrentNode = null;
    }

    public void HandleNode(string id)
    {
        RuntimeNode runtimeNode = RuntimeGraph.GetNode(id);

        // If node doesn't exist, end dialogue
        if (runtimeNode == null)
        {
            DialogueManager.EndDialogue();
            return;
        }

        // Handle the node
        CurrentNode = runtimeNode;
        switch (CurrentNode)
        {
            case RuntimeDialogueNode node:
                delayNodeCoroutine = StartCoroutine(DelayNextNode(node));
                break;
            case RuntimeSplitterNode node:
                SetupSplitterNode(node);
                break;
            case RuntimeInterruptNode:
                DialogueManager.InteruptDialogue();
                return;
        }

        UIManager.EnableDialoguePanel(true);
    }

    public void GoToNextNode()
    {
        AudioManager.StopAllSounds();

        if (CurrentNode != null && !string.IsNullOrEmpty(CurrentNode.nextNodeID))
            HandleNode(CurrentNode.nextNodeID);
        else
            DialogueManager.EndDialogue();
    }

    public void StopNodeDelay()
    {
        if (!IsDelayingNode) return;
        StopCoroutine(delayNodeCoroutine);
        delayNodeCoroutine = null;
    }

    private IEnumerator DelayNextNode(RuntimeDialogueNode node)
    {
        bool keepPrevious = node.dialogueSettings.keepPreviousText;
        if (!keepPrevious)
            UIManager.ClearDialogueText();

        float delay = node.dialogueSettings.delayText.GetValue(Blackboard);
        if (UIManager.fastForward)
            delay = 0f;
        yield return new WaitForSeconds(delay);

        delayNodeCoroutine = null;
        SetupDialogueNode(node);
    }

    public void SetupDialogueNode(RuntimeDialogueNode node)
    {
        DialogueManager.awaitContinueEvent = node.dialogueSettings.awaitContinueEvent;

        DialogueEvents.RaiseDialogueStarted(node.nodeID);

        // Send string via action
        if (node.dialogueSettings.broadcastString.GetValue(Blackboard, out string text))
            DialogueEvents.RaiseStringBroadcast(text);

        int charactersTalking = CharacterManager.HandleCharacters(node);

        UIManager.HandleBackground(node);
        UIManager.HandleDialogueBox(node);
        UIManager.HandleDialogueText(node, charactersTalking);
        UIManager.CreateChoices(node);

        if (node.dialogueSettings.musicQueue.GetValue(Blackboard, out List<AudioClip> newQueue))
        {
            bool loop = node.dialogueSettings.loop.GetValue(Blackboard);
            bool shuffle = node.dialogueSettings.shuffle.GetValue(Blackboard);
            AudioManager.SetMusicQueue(newQueue, loop, shuffle);
        }

        if (node.dialogueSettings.audioList.GetValue(Blackboard, out List<AudioClip> audioList))
            AudioManager.PlayAllSounds(audioList);
    }

    private void SetupSplitterNode(RuntimeSplitterNode node)
    {
        foreach (RuntimeSplitterOutput output in node.outputs)
        {
            bool valid = true;
            foreach (ValueComparer comparison in output.comparisons)
            {
                if (!comparison.Evaluate(Blackboard)) { valid = false; break; }
            }

            if (valid)
            {
                HandleNode(output.nextNodeID);
                return;
            }
        }

        if (!string.IsNullOrEmpty(node.defaultOutputNodeID))
            HandleNode(node.defaultOutputNodeID);
    }
}
