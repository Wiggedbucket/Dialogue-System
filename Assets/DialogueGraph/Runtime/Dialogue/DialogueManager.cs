using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public AudioManager audioManager;
    public CharacterManager characterManager;

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
    public DialogueBlackboard dialogueBlackboard;

    private RuntimeNode currentNode;
    
    public bool dialogueRunning = false;

    [Header("Dialogue Panel")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI dialogueTextShadow;
    public Image namePlateBackground;
    public Image dialogueTextBackground;

    private DialogueBoxTransition defaultDialogueBoxTransition = DialogueBoxTransition.None;
    public DialogueBoxTransition dialogueBoxTransition;
    private Color defaultDialogueBoxColor;
    private Sprite defaultDialogueBoxImage;
    private Color defaultNamePlateColor;
    private Sprite defaultNamePlateImage;

    [Header("Background")]
    public Image primaryImage;
    public Image secondaryImage;

    [Header("Choice Button UI")]
    public Button choiceButtonPrefab;
    public Transform choiceButtonContainer;

    [Header("Dialogue Option Buttons")]
    public GameObject dialogueOptionsContainer;
    public Button autoAdvanceButton;
    public Button fastForwardButton;

    public Color toggleButtonActiveTextColor = Color.lightBlue;
    public Color toggleButtonDisabledTextColor = Color.black;

    [Header("Settings")]
    public bool awaitContinueEvent = false;
    public bool onHold = false;
    public bool allowEscape = false;
    public bool allowFastAdvance = true;
    public bool autoAdvance = false;
    public float autoAdvanceDelay = 1f;
    public bool fastForward = false;
    public float fastForwardSpeed = 0.008f;
    public bool textShadowOnMultipleCharactersTalking = false;

    public float printSpeed = 0.02f;

    [Header("Dialogue Variables")]
    public bool delayNextWithClick = false;
    private string currentFullText = "";
    private bool IsTyping => typingCoroutine != null;
    private Coroutine typingCoroutine;
    private Coroutine delayNodeCoroutine;
    private TextAlignmentOptions defaultAlignment;
    private TextWrappingModes defaultWrapping;
    #endregion

    #region Start
    private void Start()
    {
        CreateRuntimeBlackboard(runtimeGraph);

        SetDefaultValues();

        SetButtonListeners();

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
        dialogueBlackboard = bb;
    }

    private void SetDefaultValues()
    {
        defaultAlignment = dialogueText.alignment;
        defaultWrapping = dialogueText.textWrappingMode;

        defaultDialogueBoxColor = dialogueTextBackground.color;
        defaultDialogueBoxImage = dialogueTextBackground.sprite;

        defaultNamePlateColor = namePlateBackground.color;
        defaultNamePlateImage = namePlateBackground.sprite;
    }

    private void SetButtonListeners()
    {
        if (autoAdvanceButton == null || fastForwardButton == null)
        {
            Debug.LogError("One or more dialogue control buttons are not assigned!");
            return;
        }

        autoAdvanceButton.onClick.AddListener(delegate { ToggleAutoAdvance(!autoAdvance); });
        fastForwardButton.onClick.AddListener(delegate { ToggleSkipButton(!fastForward); });
    }

    public void ToggleAutoAdvance(bool active)
    {
        autoAdvanceButton.GetComponentInChildren<TextMeshProUGUI>().color = active ? toggleButtonActiveTextColor : toggleButtonDisabledTextColor;
        autoAdvance = active;
        if (active == true)
            ToggleSkipButton(false);
    }

    public void ToggleSkipButton(bool active)
    {
        fastForwardButton.GetComponentInChildren<TextMeshProUGUI>().color = active ? toggleButtonActiveTextColor : toggleButtonDisabledTextColor;
        fastForward = active;
        if (active == true)
            ToggleAutoAdvance(false);
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

        if (onHold)
            return;

        if (awaitContinueEvent)
            return;

        if (allowEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            EndDialogue();
        }

        if (currentNode is RuntimeDialogueNode node && node != null)
        {
            if (!IsTyping && !delayNextWithClick && delayNodeCoroutine == null && node.choices.Count == 0)
            {
                GoToNextNode();
            }
            else if ((Mouse.current.leftButton.wasPressedThisFrame || fastForward) && node.choices.Count == 0 && IsPointerOverDialogueBox())
            {
                // If still typing, skip text printing
                if (allowFastAdvance && IsTyping && typingCoroutine != null && !fastForward)
                {
                    StopCoroutine(typingCoroutine);
                    dialogueText.text = currentFullText;
                    DialogueEvents.RaiseDialogueTextComplete(node);
                    typingCoroutine = null;
                    return;
                }
                // Else if fast advance is on and the delay is still ongoing, go to the next node
                else if (allowFastAdvance && !IsTyping && delayNodeCoroutine != null)
                {
                    StopCoroutine(delayNodeCoroutine);
                    delayNodeCoroutine = null;
                    SetupDialogueNode(node);
                }
                // Else just go to the next node
                else if (delayNextWithClick && !IsTyping)
                {
                    GoToNextNode();
                }
            }
        }
    }

    private bool IsPointerOverDialogueBox()
    {
        if (EventSystem.current == null || dialoguePanel == null)
            return false;

        PointerEventData pointerData = new(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(pointerData, results);

        // No UI hit -> definitely not over the dialogue
        if (results.Count == 0)
            return false;

        // Check if the first object hit is the dialogue box
        if (results[0].gameObject.transform == dialogueText.transform)
            return true;

        // If not, then it was probably a button
        return false;
    }

    #region Control
    private void StartDialogue()
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

    private void InteruptDialogue()
    {
        if (onHold)
            return;
        dialoguePanel.SetActive(false);
        audioManager.MusicState(MusicCommand.Pause);
        onHold = true;
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        if (delayNodeCoroutine != null)
            StopCoroutine(delayNodeCoroutine);
    }

    private void ResumeDialogue()
    {
        if (!onHold)
            return;
        onHold = false;
        if (currentNode != null)
            HandleNode(currentNode.nextNodeID);
        audioManager.MusicState(MusicCommand.Resume);
    }

    private void EndDialogue()
    {
        ResetDialogue(false);
    }

    private void ResetDialogue(bool setActive)
    {
        allowEscape = runtimeGraph.allowEscape;
        allowFastAdvance = runtimeGraph.allowFastAdvance;
        textShadowOnMultipleCharactersTalking = runtimeGraph.textShadowOnMultipleCharactersTalking;
        characterManager.notTalkingType = runtimeGraph.notTalkingType;
        
        audioManager.ResetController();

        ToggleAutoAdvance(false);
        ToggleSkipButton(false);

        dialoguePanel.SetActive(setActive);
        dialogueRunning = setActive;

        primaryImage.sprite = null;
        primaryImage.enabled = false;
        secondaryImage.sprite = null;
        secondaryImage.enabled = false;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        if (delayNodeCoroutine != null)
            StopCoroutine(delayNodeCoroutine);

        characterManager.StopAllCharacterCoroutines();
        characterManager.RemoveAllCharacters();

        currentNode = null;

        // Clear all choice buttons
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }
    #endregion

    #region Nodes
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
                delayNodeCoroutine = StartCoroutine(DelayNextNode(node));
                break;
            case RuntimeSplitterNode node:
                SetupSplitterNode(node);
                break;
            case RuntimeInterruptNode:
                InteruptDialogue();
                return;
        }

        dialoguePanel.SetActive(true);
    }

    private void GoToNextNode()
    {
        audioManager.StopAllSounds();

        if (currentNode != null && !string.IsNullOrEmpty(currentNode.nextNodeID))
            HandleNode(currentNode.nextNodeID);
        else
            EndDialogue();
    }

    private IEnumerator DelayNextNode(RuntimeDialogueNode node)
    {
        bool keepPrevious = node.dialogueSettings.keepPreviousText;
        if (!keepPrevious)
        {
            currentFullText = "";
            dialogueText.text = "";
        }

        float delay = node.dialogueSettings.delayText.GetValue(dialogueBlackboard);
        yield return new WaitForSeconds(delay);

        delayNodeCoroutine = null;
        SetupDialogueNode(node);
    }

    private void SetupDialogueNode(RuntimeDialogueNode node)
    {
        awaitContinueEvent = node.dialogueSettings.awaitContinueEvent;

        DialogueEvents.RaiseDialogueStarted(node);

        // Send string via action
        if (node.dialogueSettings.broadcastString.GetValue(dialogueBlackboard, out string text))
        {
            DialogueEvents.RaiseStringBroadcast(text);
        }

        int charactersTalking = characterManager.HandleCharacters(node);

        HandleBackground(node);

        HandleDialogueBox(node);

        dialogueTextShadow.gameObject.SetActive(charactersTalking > 1);

        HandleDialogueText(node);

        CreateChoices(node);

        if (node.dialogueSettings.musicQueue.GetValue(dialogueBlackboard, out List<AudioClip> newQueue))
        {
            bool loop = node.dialogueSettings.loop.GetValue(dialogueBlackboard);
            bool shuffle = node.dialogueSettings.shuffle.GetValue(dialogueBlackboard);
            audioManager.SetMusicQueue(newQueue, loop, shuffle);
        }
        
        if (node.dialogueSettings.audioList.GetValue(dialogueBlackboard, out List<AudioClip> audioList))
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
    #endregion

    #region Dialogue
    private void HandleBackground(RuntimeDialogueNode node)
    {
        // Add background transitions
        bool useValue = node.dialogueSettings.backgroundImage.GetValue(dialogueBlackboard, out Sprite backgroundImage);
        if (useValue)
        {
            primaryImage.sprite = backgroundImage;
            primaryImage.enabled = backgroundImage != null;
        }
    }

    private void HandleDialogueBox(RuntimeDialogueNode node)
    {
        // Add dialogue box transitions
        bool useValue;

        useValue = node.dialogueSettings.dialogueBoxTransition.GetValue(dialogueBlackboard, out DialogueBoxTransition transition);
        dialogueBoxTransition = useValue ? transition : defaultDialogueBoxTransition;

        useValue = node.dialogueSettings.dialogueBoxColor.GetValue(dialogueBlackboard, out Color namePlateColor);
        namePlateBackground.color = useValue ? namePlateColor : defaultNamePlateColor;

        useValue = node.dialogueSettings.dialogueBoxImage.GetValue(dialogueBlackboard, out Sprite namePlateImage);
        namePlateBackground.sprite = useValue ? namePlateImage : defaultNamePlateImage;

        useValue = node.dialogueSettings.namePlateColor.GetValue(dialogueBlackboard, out Color dialogueBoxColor);
        dialogueTextBackground.color = useValue ? dialogueBoxColor : defaultDialogueBoxColor;

        useValue = node.dialogueSettings.namePlateImage.GetValue(dialogueBlackboard, out Sprite dialogueBoxImage);
        dialogueTextBackground.sprite = useValue ? dialogueBoxImage : defaultDialogueBoxImage;
    }

    private void HandleDialogueText(RuntimeDialogueNode node)
    {
        delayNextWithClick = node.dialogueSettings.delayNextWithClick;

        // Stop previous typing if any
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // Gather dialogue settings
        bool keepPrevious = node.dialogueSettings.keepPreviousText;
        bool useValue;

        useValue = node.dialogueSettings.textAlign.GetValue(dialogueBlackboard, out TextAlignmentOptions options);
        dialogueText.alignment = useValue ? options : defaultAlignment;

        useValue = node.dialogueSettings.wrapText.GetValue(dialogueBlackboard, out TextWrappingModes wrap);
        dialogueText.textWrappingMode = useValue ? wrap : defaultWrapping;

        // Build styled text
        string styledText = BuildStyledText(node);

        // Start typing
        typingCoroutine = StartCoroutine(TypeText(node, styledText, keepPrevious));
    }

    private string BuildStyledText(RuntimeDialogueNode node)
    {
        DialogueSettings s = node.dialogueSettings;

        string text = node.dialogueText;

        // Apply TMP Rich Text based on dialogue settings
        if (s.bold.GetValue(dialogueBlackboard))
            text = $"<b>{text}</b>";
        if (s.italic.GetValue(dialogueBlackboard))
            text = $"<i>{text}</i>";
        if (s.underline.GetValue(dialogueBlackboard))
            text = $"<u>{text}</u>";
        if (s.color.GetValue(dialogueBlackboard, out Color color))
            text = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        if (s.font.GetValue(dialogueBlackboard, out TMP_FontAsset font))
            text = $"<font=\"{font.name}\">{text}</font>";

        return text;
    }

    private IEnumerator TypeText(RuntimeDialogueNode node, string newText, bool keepPrevious)
    {
        int startIndex = currentFullText.Length;
        currentFullText += newText;

        string visibleText = currentFullText[..startIndex];
        string remaining = currentFullText[startIndex..];

        int i = 0;
        while (i < remaining.Length)
        {
            char c = remaining[i];

            // Instantly add entire rich text tags
            if (c == '<')
            {
                int closingIndex = remaining.IndexOf('>', i);
                if (closingIndex != -1)
                {
                    visibleText += remaining.Substring(i, closingIndex - i + 1);
                    i = closingIndex + 1;
                    continue; // no delay for tags
                }
            }

            // Otherwise, add a single visible character
            visibleText += c;
            dialogueText.text = visibleText;

            float speed = fastForward ? fastForwardSpeed : printSpeed;
            if (node.dialogueSettings.printSpeed.GetValue(dialogueBlackboard, out float speedValue))
            {
                printSpeed = speedValue;
                speed = fastForward ? fastForwardSpeed : speedValue;
            }

            yield return new WaitForSeconds(speed);
            i++;
        }

        // Ensure full text is displayed at the end
        dialogueText.text = visibleText;

        DialogueEvents.RaiseDialogueTextComplete(node);
        typingCoroutine = null;

        // Stop here if this node is waiting for a continue event
        if (awaitContinueEvent)
            yield break;

        // If there are choices, don't continue
        if (node.choices.Count > 0)
            yield break;

        if (autoAdvance)
        {
            RuntimeDialogueNode nextDialogue = FindNextDialogueNode(currentNode.nextNodeID);

            if (nextDialogue == null || (nextDialogue != null && !nextDialogue.dialogueSettings.keepPreviousText))
            {
                yield return new WaitForSeconds(autoAdvanceDelay);
            }

            GoToNextNode();
        }
        else if (!delayNextWithClick)
        {
            GoToNextNode();
        }
    }

    private RuntimeDialogueNode FindNextDialogueNode(string nodeId, int depth = 0)
    {
        // Infinite loop safety limit
        if (depth > 50)
            return null;

        // Sees if the next node exists
        if (string.IsNullOrEmpty(nodeId))
            return null;

        RuntimeNode node = runtimeGraph.GetNode(nodeId);
        if (node == null)
            return null;

        switch (node)
        {
            case RuntimeDialogueNode dialogueNode:
                return dialogueNode;

            case RuntimeSplitterNode splitterNode:
                // Try to find the first valid output that leads to a dialogue node
                foreach (RuntimeSplitterOutput output in splitterNode.outputs)
                {
                    bool valid = true;
                    foreach (ValueComparer comparison in output.comparisons)
                    {
                        if (!comparison.Evaluate(dialogueBlackboard))
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                    {
                        RuntimeDialogueNode result = FindNextDialogueNode(output.nextNodeID, depth + 1);
                        if (result != null)
                            return result;
                    }
                }

                // Try default output if nothing else matched
                if (!string.IsNullOrEmpty(splitterNode.defaultOutputNodeID))
                    return FindNextDialogueNode(splitterNode.defaultOutputNodeID, depth + 1);

                break;
        }

        return null;
    }

    private void CreateChoices(RuntimeDialogueNode node)
    {
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
                        DialogueEvents.RaiseChoiceSelected(choice.nextNodeID);
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
    #endregion
}