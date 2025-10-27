using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueUIManager : MonoBehaviour
{
    // References to external systems
    private DialogueManager DialogueManager => DialogueManager.Instance;
    private DialogueBlackboard Blackboard => DialogueBlackboard.Instance;

    [SerializeField] private BackgroundTransitionController backgroundController;
    private NodeProcessor NodeProcessor => DialogueManager.processor;
    private RuntimeDialogueGraph RuntimeGraph => DialogueManager.runtimeGraph;
    private RuntimeNode CurrentNode => NodeProcessor.CurrentNode;

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image namePlateBackground;
    [SerializeField] private Image dialogueTextBackground;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI dialogueTextShadow;

    [Header("Choice Button UI")]
    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private Transform choiceButtonContainer;
    
    [Header("Dialogue Option Buttons")]
    public Button autoAdvanceButton;
    public Button fastForwardButton;
    public Color toggleButtonActiveTextColor = Color.lightBlue;
    public Color toggleButtonDisabledTextColor = Color.black;

    [Header("Dialogue Colors / Defaults")]
    private Color defaultDialogueBoxColor;
    private Sprite defaultDialogueBoxImage;
    private Color defaultNamePlateColor;
    private Sprite defaultNamePlateImage;
    private TextAlignmentOptions defaultAlignment;
    private TextWrappingModes defaultWrapping;

    [Header("Typing Settings")]
    public bool allowFastAdvance = true;
    public bool textShadowOnMultipleCharactersTalking = false;
    public bool autoAdvance = false;
    public float autoAdvanceDelay = 1f;
    public bool fastForward = false;
    public float fastForwardSpeed = 0.008f;
    public float basePrintSpeed = 0.02f;

    public bool delayNextWithClick = false;

    private string currentFullText = "";
    public bool IsTyping => typingCoroutine != null;
    private Coroutine typingCoroutine;

    #region Setup
    private void Start()
    {
        SetDefaultValues();
        SetButtonListeners();
    }

    private void OnDestroy()
    {
        // remove listeners to avoid leaks
        if (autoAdvanceButton != null) autoAdvanceButton.onClick.RemoveAllListeners();
        if (fastForwardButton != null) fastForwardButton.onClick.RemoveAllListeners();
    }

    private void SetDefaultValues()
    {
        if (dialogueText == null) return;

        defaultAlignment = dialogueText.alignment;
        defaultWrapping = dialogueText.textWrappingMode;

        defaultDialogueBoxColor = dialogueTextBackground.color;
        defaultDialogueBoxImage = dialogueTextBackground.sprite;

        defaultNamePlateColor = namePlateBackground.color;
        defaultNamePlateImage = namePlateBackground.sprite;
    }

    private void SetButtonListeners()
    {
        if (autoAdvanceButton != null)
            autoAdvanceButton.onClick.AddListener(() => ToggleAutoAdvance(!autoAdvance));

        if (fastForwardButton != null)
            fastForwardButton.onClick.AddListener(() => ToggleSkipButton(!fastForward));
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

    public void ResetController()
    {
        if (RuntimeGraph != null)
        {
            allowFastAdvance = RuntimeGraph.allowFastAdvance;
        }

        StopPrinting();
        ClearDialogueText();

        // Reset text box
        dialogueText.alignment = defaultAlignment;
        dialogueText.textWrappingMode = defaultWrapping;

        dialogueTextBackground.color = defaultDialogueBoxColor;
        dialogueTextBackground.sprite = defaultDialogueBoxImage;

        namePlateBackground.color = defaultNamePlateColor;
        namePlateBackground.sprite = defaultNamePlateImage;

        backgroundController.ResetController();

        // Reset toggles
        ToggleAutoAdvance(false);
        ToggleSkipButton(false);

        // Clear choice buttons
        foreach (Transform child in choiceButtonContainer)
            Destroy(child.gameObject);
    }
    #endregion

    private void Update()
    {
        if (DialogueManager.onHold || DialogueManager.awaitContinueEvent)
            return;

        if (CurrentNode is RuntimeDialogueNode node && node != null)
        {
            bool pointerOverDialogue = IsPointerOverObjects(new List<Transform> { dialogueTextBackground.transform, namePlateBackground.transform });

            if (!IsTyping && !delayNextWithClick && !DialogueManager.IsDelayingNode && node.choices.Count == 0)
            {
                NodeProcessor.GoToNextNode();
            }
            else if ((Mouse.current.leftButton.wasPressedThisFrame || fastForward) && node.choices.Count == 0 && pointerOverDialogue)
            {
                // If still typing, skip text printing
                if (allowFastAdvance && IsTyping && IsTyping && !fastForward)
                {
                    SkipPrinting();
                    return;
                }
                // Else if fast advance is on and the delay is still ongoing, go to the next node
                else if (allowFastAdvance && !IsTyping && DialogueManager.IsDelayingNode)
                {
                    NodeProcessor.StopNodeDelay();
                    NodeProcessor.SetupDialogueNode(node);
                }
                // Else just go to the next node
                else if (delayNextWithClick && !IsTyping)
                {
                    NodeProcessor.GoToNextNode();
                }
            }
        }
    }

    #region Public Entry Points
    public void EnableDialoguePanel(bool enable)
    {
        dialoguePanel.SetActive(enable);
    }

    public void HandleBackground(RuntimeDialogueNode node)
    {
        if (node.dialogueSettings.backgroundImage.GetValue(Blackboard, out Sprite newBackground))
        {
            node.dialogueSettings.backgroundTransition.GetValue(Blackboard, out BackgroundTransition transition);

            if (transition == BackgroundTransition.None)
                backgroundController.SetImmediate(newBackground);
            else
                backgroundController.TransitionTo(newBackground, transition);
        }
    }

    public void HandleDialogueBox(RuntimeDialogueNode node)
    {
        DialogueSettings s = node.dialogueSettings;
        bool useValue;

        useValue = s.dialogueBoxColor.GetValue(Blackboard, out Color namePlateColor);
        namePlateBackground.color = useValue ? namePlateColor : defaultNamePlateColor;

        useValue = s.dialogueBoxImage.GetValue(Blackboard, out Sprite namePlateImage);
        namePlateBackground.sprite = useValue ? namePlateImage : defaultNamePlateImage;

        useValue = s.namePlateColor.GetValue(Blackboard, out Color dialogueBoxColor);
        dialogueTextBackground.color = useValue ? dialogueBoxColor : defaultDialogueBoxColor;

        useValue = s.namePlateImage.GetValue(Blackboard, out Sprite dialogueBoxImage);
        dialogueTextBackground.sprite = useValue ? dialogueBoxImage : defaultDialogueBoxImage;
    }

    public void HandleDialogueText(RuntimeDialogueNode node, int charactersTalking)
    {
        delayNextWithClick = node.dialogueSettings.delayNextWithClick;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        // Apply text layout settings
        DialogueSettings s = node.dialogueSettings;
        bool useValue;

        useValue = s.textAlign.GetValue(Blackboard, out TextAlignmentOptions options);
        dialogueText.alignment = useValue ? options : defaultAlignment;

        useValue = s.wrapText.GetValue(Blackboard, out TextWrappingModes wrap);
        dialogueText.textWrappingMode = useValue ? wrap : defaultWrapping;

        // Add tags to dialogue text based on settings
        string styledText = BuildStyledText(node);

        // Handle shadow text
        textShadowOnMultipleCharactersTalking = RuntimeGraph.textShadowOnMultipleCharactersTalking;
        if (dialogueTextShadow != null)
            dialogueTextShadow.gameObject.SetActive(charactersTalking > 1 && textShadowOnMultipleCharactersTalking);

        // Start printing text
        typingCoroutine = StartCoroutine(PrintText(node, styledText));
    }

    public void StopPrinting()
    {
        if (!IsTyping) return;

        StopCoroutine(typingCoroutine);
        typingCoroutine = null;
    }

    public void SkipPrinting()
    {
        if (!IsTyping) return;

        StopCoroutine(typingCoroutine);
        dialogueText.text = currentFullText;
        if (dialogueTextShadow != null)
            dialogueTextShadow.text = StripColorTags(currentFullText);

        DialogueEvents.RaiseDialogueTextComplete(CurrentNode.nodeID);
        typingCoroutine = null;
    }

    public void ClearDialogueText()
    {
        currentFullText = "";
        if (dialogueText != null) dialogueText.text = "";
        if (dialogueTextShadow != null) dialogueTextShadow.text = "";
    }

    public void CreateChoices(RuntimeDialogueNode node)
    {
        foreach (Transform child in choiceButtonContainer)
            Destroy(child.gameObject);

        if (node.choices.Count == 0) return;

        foreach (RuntimeChoice choice in node.choices)
        {
            Button button = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.SetText(choice.choiceText.value);

            bool valid = true;
            foreach (ValueComparer comparison in choice.comparisons)
                if (!comparison.Evaluate(Blackboard)) { valid = false; break; }

            button.interactable = valid;
            button.gameObject.SetActive(valid || choice.showIfConditionNotMet.GetValue(Blackboard));

            button.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(choice.nextNodeID))
                {
                    DialogueEvents.RaiseChoiceSelected(choice.nextNodeID);
                    NodeProcessor.HandleNode(choice.nextNodeID);
                }
                else
                {
                    DialogueManager.EndDialogue();
                }
            });
        }
    }
    #endregion

    #region Private Helpers
    private string BuildStyledText(RuntimeDialogueNode node)
    {
        DialogueSettings s = node.dialogueSettings;
        string text = node.dialogueText ?? "";

        if (s.bold.GetValue(Blackboard)) text = $"<b>{text}</b>";
        if (s.italic.GetValue(Blackboard)) text = $"<i>{text}</i>";
        if (s.underline.GetValue(Blackboard)) text = $"<u>{text}</u>";
        if (s.color.GetValue(Blackboard, out Color color)) text = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        if (s.font.GetValue(Blackboard, out TMP_FontAsset font)) text = $"<font=\"{font.name}\">{text}</font>";

        return text;
    }

    private IEnumerator PrintText(RuntimeDialogueNode node, string newText)
    {
        int startIndex = currentFullText.Length;
        currentFullText += newText;

        string visibleText = currentFullText.Substring(0, startIndex);
        string remaining = currentFullText.Substring(startIndex);

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
            // Append shadow text without color tags
            if (dialogueTextShadow != null)
                dialogueTextShadow.text = StripColorTags(visibleText);

            float speed = fastForward ? fastForwardSpeed : basePrintSpeed;
            if (node.dialogueSettings.printSpeed.GetValue(Blackboard, out float speedValue))
            {
                basePrintSpeed = speedValue;
                speed = fastForward ? fastForwardSpeed : speedValue;
            }

            yield return new WaitForSeconds(speed);
            i++;
        }

        // Ensure full text is displayed at the end
        dialogueText.text = visibleText;
        if (dialogueTextShadow != null)
            dialogueTextShadow.text = StripColorTags(visibleText);

        DialogueEvents.RaiseDialogueTextComplete(node.nodeID);
        typingCoroutine = null;

        // Stop here if waiting for external continue event or if there are choices
        if (DialogueManager.awaitContinueEvent) yield break;
        if (node.choices.Count > 0) yield break;

        if (autoAdvance)
        {
            RuntimeDialogueNode next = FindNextDialogueNode(CurrentNode.nextNodeID);
            if (next == null || !next.dialogueSettings.keepPreviousText)
                yield return new WaitForSeconds(autoAdvanceDelay);

            NodeProcessor.GoToNextNode();
        }
        else if (!delayNextWithClick)
        {
            NodeProcessor.GoToNextNode();
        }
    }

    private RuntimeDialogueNode FindNextDialogueNode(string nodeId, int depth = 0)
    {
        // Infinite loop safety limit
        if (depth > 50 || string.IsNullOrEmpty(nodeId)) return null;

        RuntimeNode node = RuntimeGraph.GetNode(nodeId);
        if (node == null) return null;

        // Return if it's a dialogue node
        if (node is RuntimeDialogueNode rd) return rd;

        if (node is RuntimeSplitterNode sn)
        {
            foreach (RuntimeSplitterOutput output in sn.outputs)
            {
                bool valid = true;
                foreach (ValueComparer comp in output.comparisons)
                {
                    if (!comp.Evaluate(Blackboard)) { valid = false; break; }
                }

                if (valid)
                {
                    RuntimeDialogueNode found = FindNextDialogueNode(output.nextNodeID, depth + 1);
                    if (found != null) return found;
                }
            }

            if (!string.IsNullOrEmpty(sn.defaultOutputNodeID))
                return FindNextDialogueNode(sn.defaultOutputNodeID, depth + 1);
        }

        return null;
    }

    // checks if the mouse pointer is over a list of UI objects
    private bool IsPointerOverObjects(List<Transform> uiElements, string ignoreTag = null, int? ignoreLayer = null) // int? is a nullable int
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

        foreach (RaycastResult result in results)
        {
            GameObject hitObject = result.gameObject;

            // Skip ignored layer
            if (ignoreLayer.HasValue && hitObject.layer == ignoreLayer.Value)
                continue;

            // Skip ignored tag
            if (!string.IsNullOrEmpty(ignoreTag) && hitObject.CompareTag(ignoreTag))
                continue;

            // Check if the hit object (or its parent) matches one of the tracked UI elements
            foreach (Transform element in uiElements)
            {
                if (hitObject.transform == element || hitObject.transform.IsChildOf(element))
                    return true;
            }

            return false;
        }

        // If not, then it was probably a button
        return false;
    }

    private static string StripColorTags(string input)
    {
        // Removes tags like <color=...> and </color>
        return System.Text.RegularExpressions.Regex.Replace(
                    input,
                    @"</?color(=[^>]+)?>",
                    string.Empty,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
    }
    #endregion
}
