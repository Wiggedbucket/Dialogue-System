using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    #region Variables
    public RuntimeDialogueGraph runtimeGraph;
    public DialogueBlackboard dialogueBlackboard;

    private RuntimeNode currentNode;
    
    public bool dialogueRunning = false;

    public static event Action<string> OnStringBroadcast;

    [Header("Dialogue Panel")]
    public GameObject dialoguePanel;
    public Transform characterHolder;
    public TextMeshProUGUI speakerNameText;
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

    [Header("Audio Sources")]
    public AudioSource musicSource;

    [Header("Settings")]
    public bool onHold = false;
    public bool allowEscape = false; // TODO
    public bool allowFastAdvance = true;
    public bool autoAdvance = false; // TODO
    public bool enableSkipping = false; // TODO
    public bool textShadowOnMultipleCharactersTalking = false;
    public NotTalkingType notTalkingType = NotTalkingType.None;

    public float printSpeed = 0.02f;

    [Header("Character Variables")]
    public Dictionary<string, CharacterObject> characterObjects = new();
    private Coroutine positionMovementCoroutine;
    private Coroutine rotationMovementCoroutine;
    private Coroutine scaleMovementCoroutine;

    [Header("Dialogue Variables")]
    public bool delayNextWithClick = false;
    private string currentFullText = "";
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Coroutine delayNodeCoroutine;
    private TextAlignmentOptions defaultAlignment;
    private TextWrappingModes defaultWrapping;

    [Header("Sound Variables")]
    public List<AudioClip> musicQueue = new();
    public int currentTrackIndex = -1;
    public bool loop = true;
    public bool shuffle = false;

    public List<AudioSource> activeSources = new();
    private List<Coroutine> soundCoroutines = new();
    #endregion

    #region Start
    private void Start()
    {
        CreateRuntimeBlackboard(runtimeGraph);

        SetDefaultValues();
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
    #endregion

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
            if (!isTyping && !delayNextWithClick && delayNodeCoroutine == null && node.choices.Count == 0)
            {
                GoToNextNode();
            }
            else if (Mouse.current.leftButton.wasPressedThisFrame && node.choices.Count == 0)
            {
                // If still typing, skip text printing
                if (allowFastAdvance && isTyping && typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                    dialogueText.text = currentFullText;
                    isTyping = false;
                    typingCoroutine = null;
                    return;
                }
                else if (allowFastAdvance && !isTyping && delayNodeCoroutine != null)
                {
                    StopCoroutine(delayNodeCoroutine);
                    delayNodeCoroutine = null;
                    SetupDialogueNode(node);
                }
                else if (delayNextWithClick && !isTyping)
                {
                    GoToNextNode();
                }
            }

            HandleMusicQueue();
        }
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
        dialoguePanel.SetActive(false);
        musicSource.Pause();
        onHold = true;
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        if (delayNodeCoroutine != null)
            StopCoroutine(delayNodeCoroutine);
    }

    private void ResumeDialogue()
    {
        onHold = false;
        if (currentNode != null)
            HandleNode(currentNode.nextNodeID);
        musicSource.UnPause();
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
        notTalkingType = runtimeGraph.notTalkingType;
        loop = true;
        shuffle = false;

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

        StopAllSounds();
        musicSource.Stop();
        currentNode = null;

        // Clear all characters
        foreach (KeyValuePair<string, CharacterObject> character in characterObjects)
        {
            Destroy(character.Value.gameObject);
        }
        characterObjects.Clear();

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
            case RuntimeInteruptNode node:
                InteruptDialogue();
                return;
        }

        dialoguePanel.SetActive(true);
    }

    private void GoToNextNode()
    {
        if (!string.IsNullOrEmpty(currentNode.nextNodeID))
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
        // Send string via action
        if (node.dialogueSettings.broadcastString.GetValue(dialogueBlackboard, out string text))
        {
            OnStringBroadcast?.Invoke(text);
        }

        int charactersTalking = HandleCharacters(node);

        HandleBackground(node);

        HandleDialogueBox(node);

        dialogueTextShadow.gameObject.SetActive(charactersTalking > 1);

        HandleDialogueText(node);

        CreateChoices(node);

        ChangeMusicQueue(node);

        PlayAllSounds(node);
    }
    #endregion

    #region Characters
    public class CharacterObject
    {
        public CharacterData characterData = new();
        public GameObject gameObject;
        public RectTransform rectTransform;
        public Image image;
    }

    private int HandleCharacters(RuntimeDialogueNode node)
    {
        List<string> speakerNames = new();

        foreach (CharacterData data in node.characters)
        {
            string name = data.name.GetValue(dialogueBlackboard);
            if (string.IsNullOrEmpty(name))
                continue;

            // Add a new character object to the list if it didn't yet exist in the scene
            if (!characterObjects.TryGetValue(name, out CharacterObject character))
            {
                character = new CharacterObject
                {
                    characterData = new CharacterData(),
                    gameObject = new GameObject(name, typeof(RectTransform), typeof(Image)),
                };
                character.rectTransform = character.gameObject.GetComponent<RectTransform>();
                character.image = character.gameObject.GetComponent<Image>();
                character.rectTransform.SetParent(characterHolder, false);
                characterObjects.Add(name, character);
            }

            // Skip currently active transitions and instantly complete the movement
            if (positionMovementCoroutine != null)
                StopCoroutine(positionMovementCoroutine);
            if (rotationMovementCoroutine != null)
                StopCoroutine(rotationMovementCoroutine);
            if (scaleMovementCoroutine != null)
                StopCoroutine(scaleMovementCoroutine);
            character.rectTransform.localPosition = character.characterData.characterPosition.GetValue(dialogueBlackboard);
            character.rectTransform.localEulerAngles = new Vector3(0, 0, character.characterData.characterRotation.GetValue(dialogueBlackboard));
            character.rectTransform.localScale = character.characterData.characterScale.GetValue(dialogueBlackboard);

            // Merge node data into persistent character data
            ApplyCharacterData(character, data);

            CharacterData merged = character.characterData;

            // Apply final values to GameObject
            if (merged.characterSprite.GetValue(dialogueBlackboard, out Sprite sprite))
                character.image.sprite = sprite;

            if (merged.isVisible.GetValue(dialogueBlackboard, out bool visible))
                character.gameObject.SetActive(visible);

            if (merged.minAnchor.GetValue(dialogueBlackboard, out Vector2 minAnchor))
                character.rectTransform.anchorMin = minAnchor;

            if (merged.maxAnchor.GetValue(dialogueBlackboard, out Vector2 maxAnchor))
                character.rectTransform.anchorMax = maxAnchor;

            if (merged.pivot.GetValue(dialogueBlackboard, out Vector2 pivot))
                character.rectTransform.pivot = pivot;

            if (merged.widthAndHeight.GetValue(dialogueBlackboard, out Vector2 widthAndHeight))
                character.rectTransform.sizeDelta = widthAndHeight;

            if (merged.preserveAspect.GetValue(dialogueBlackboard, out bool preserveAspect))
                character.image.preserveAspect = preserveAspect;

            // Set position, rotation and scale
            HandleCharacterMovement(character, merged);

            // Talking state
            bool talking = merged.isTalking.GetValue(dialogueBlackboard);
            bool hideName = merged.hideName.GetValue(dialogueBlackboard);

            if (talking)
            {
                speakerNames.Add(hideName ? "???" : name);

                character.image.color = Color.white;
            } else
            {
                switch (notTalkingType)
                {
                    case NotTalkingType.GreyOut:
                        character.image.color = Color.grey;
                        break;
                }
            }
        }

        // Speaker nameplate
        speakerNameText.text = string.Join(
            speakerNames.Count > 2 ? ", " : " and ",
            speakerNames
        );

        return speakerNames.Count;
    }

    private void ApplyCharacterData(CharacterObject target, CharacterData incoming)
    {
        CharacterData stored = target.characterData;
        DialogueBlackboard bb = dialogueBlackboard;

        stored.name = incoming.name;

        // Apply only if the node actually sets these
        CopyIfChanged(stored.characterSprite, incoming.characterSprite, bb);
        CopyIfChanged(stored.isVisible, incoming.isVisible, bb);
        CopyIfChanged(stored.isTalking, incoming.isTalking, bb);
        CopyIfChanged(stored.hideName, incoming.hideName, bb);
        CopyIfChanged(stored.transitionDuration, incoming.transitionDuration, bb);
        CopyIfChanged(stored.positionMovementType, incoming.positionMovementType, bb);
        CopyIfChanged(stored.rotationMovementType, incoming.rotationMovementType, bb);
        CopyIfChanged(stored.scaleMovementType, incoming.scaleMovementType, bb);
        CopyIfChanged(stored.characterPosition, incoming.characterPosition, bb);
        CopyIfChanged(stored.minAnchor, incoming.minAnchor, bb);
        CopyIfChanged(stored.maxAnchor, incoming.maxAnchor, bb);
        CopyIfChanged(stored.pivot, incoming.pivot, bb);
        CopyIfChanged(stored.characterRotation, incoming.characterRotation, bb);
        CopyIfChanged(stored.widthAndHeight, incoming.widthAndHeight, bb);
        CopyIfChanged(stored.characterScale, incoming.characterScale, bb);

        target.characterData = stored;
    }

    private void CopyIfChanged<T>(PortValue<T> target, PortValue<T> source, DialogueBlackboard bb)
    {
        if (source.GetValue(bb, out T value))
        {
            target.usePortValue = source.usePortValue;
            target.blackboardVariableName = source.blackboardVariableName;
            target.value = source.value;
        }
    }

    private void HandleCharacterMovement(CharacterObject character, CharacterData merged)
    {
        float duration = merged.transitionDuration.GetValue(dialogueBlackboard, out float d) ? d : 0.4f;

        if (merged.characterPosition.GetValue(dialogueBlackboard, out Vector2 targetPos))
        {
            MovementType posType = merged.positionMovementType.GetValue(dialogueBlackboard, out MovementType pType)
                ? pType
                : MovementType.Instant;

            switch (posType)
            {
                case MovementType.Instant:
                    character.rectTransform.anchoredPosition = targetPos;
                    break;

                case MovementType.Linear:
                    positionMovementCoroutine = StartCoroutine(SmoothMoveCharacter(character.rectTransform, targetPos, duration, false));
                    break;

                case MovementType.Smooth:
                    positionMovementCoroutine = StartCoroutine(SmoothMoveCharacter(character.rectTransform, targetPos, duration, true));
                    break;
            }
        }

        if (merged.characterRotation.GetValue(dialogueBlackboard, out float targetRot))
        {
            MovementType rotType = merged.rotationMovementType.GetValue(dialogueBlackboard, out MovementType rType)
                ? rType
                : MovementType.Instant;

            switch (rotType)
            {
                case MovementType.Instant:
                    character.rectTransform.localEulerAngles = new Vector3(0, 0, targetRot);
                    break;

                case MovementType.Linear:
                    rotationMovementCoroutine = StartCoroutine(SmoothRotateCharacter(character.rectTransform, targetRot, duration, false));
                    break;

                case MovementType.Smooth:
                    rotationMovementCoroutine = StartCoroutine(SmoothRotateCharacter(character.rectTransform, targetRot, duration, true));
                    break;
            }
        }

        if (merged.characterScale.GetValue(dialogueBlackboard, out Vector2 targetScale))
        {
            MovementType scaleType = merged.scaleMovementType.GetValue(dialogueBlackboard, out MovementType sType)
                ? sType
                : MovementType.Instant;

            switch (scaleType)
            {
                case MovementType.Instant:
                    character.rectTransform.localScale = targetScale;
                    break;

                case MovementType.Linear:
                    scaleMovementCoroutine = StartCoroutine(SmoothScaleCharacter(character.rectTransform, targetScale, duration, false));
                    break;

                case MovementType.Smooth:
                    scaleMovementCoroutine = StartCoroutine(SmoothScaleCharacter(character.rectTransform, targetScale, duration, true));
                    break;
            }
        }
    }

    private IEnumerator SmoothMoveCharacter(RectTransform rect, Vector2 target, float duration, bool eased)
    {
        Vector2 start = rect.anchoredPosition;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            if (eased)
                progress = EaseInOut(progress);
            rect.anchoredPosition = Vector2.Lerp(start, target, progress);
            yield return null;
        }

        rect.anchoredPosition = target;
    }

    private IEnumerator SmoothRotateCharacter(RectTransform rect, float targetRot, float duration, bool linear)
    {
        float t = 0f;
        float start = rect.localEulerAngles.z;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            if (!linear)
                progress = EaseInOut(progress);

            float rot = Mathf.LerpAngle(start, targetRot, progress);
            rect.localEulerAngles = new Vector3(0, 0, rot);
            yield return null;
        }

        rect.localEulerAngles = new Vector3(0, 0, targetRot);
    }

    private IEnumerator SmoothScaleCharacter(RectTransform rect, Vector2 targetScale, float duration, bool linear)
    {
        float t = 0f;
        Vector2 start = rect.localScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            if (!linear)
                progress = EaseInOut(progress);

            rect.localScale = Vector2.Lerp(start, targetScale, progress);
            yield return null;
        }

        rect.localScale = targetScale;
    }

    private float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t); // classic smoothstep
    }
    #endregion

    #region Dialogue
    private void HandleBackground(RuntimeDialogueNode node)
    {
        bool useValue = node.dialogueSettings.backgroundImage.GetValue(dialogueBlackboard, out Sprite backgroundImage);
        if (useValue)
        {
            primaryImage.sprite = backgroundImage;
            primaryImage.enabled = backgroundImage != null;
        }
    }

    private void HandleDialogueBox(RuntimeDialogueNode node)
    {
        bool useValue = false;

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
        float _printSpeed = printSpeed;
        if (node.dialogueSettings.printSpeed.GetValue(dialogueBlackboard, out float speedValue))
        {
            printSpeed = speedValue;
            _printSpeed = speedValue;
        }

        bool useValue = false;

        useValue = node.dialogueSettings.textAlign.GetValue(dialogueBlackboard, out TextAlignmentOptions options);
        dialogueText.alignment = useValue ? options : defaultAlignment;

        useValue = node.dialogueSettings.wrapText.GetValue(dialogueBlackboard, out TextWrappingModes wrap);
        dialogueText.textWrappingMode = useValue ? wrap : defaultWrapping;

        // Build styled text
        string styledText = BuildStyledText(node);

        // Start typing
        typingCoroutine = StartCoroutine(TypeText(styledText, _printSpeed, keepPrevious));
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

    private IEnumerator TypeText(string newText, float speed, bool keepPrevious)
    {
        isTyping = true;

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

            yield return new WaitForSeconds(speed);
            i++;
        }

        // Ensure full text is displayed at the end
        dialogueText.text = visibleText;

        isTyping = false;
        typingCoroutine = null;

        RuntimeDialogueNode node = currentNode as RuntimeDialogueNode;
        if (node == null)
            yield break;

        if (node.choices.Count > 0)
            yield break;

        if (!delayNextWithClick)
            GoToNextNode();
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

    #region Music
    private void HandleMusicQueue()
    {
        if (musicSource.isPlaying || musicQueue.Count == 0)
            return;

        if (loop)
            if (currentTrackIndex >= musicQueue.Count)
                currentTrackIndex = 0;

        PlayNextTrack();
    }

    private void ChangeMusicQueue(RuntimeDialogueNode node)
    {
        if (node.dialogueSettings.musicQueue.GetValue(dialogueBlackboard, out List<AudioClip> newQueue))
        {
            musicSource.Stop();
            musicQueue.Clear();
            musicQueue.AddRange(newQueue);

            loop = node.dialogueSettings.loop.GetValue(dialogueBlackboard);
            shuffle = node.dialogueSettings.shuffle.GetValue(dialogueBlackboard);

            if (shuffle)
                Shuffle(musicQueue);

            currentTrackIndex = -1;
            PlayNextTrack();
        }
    }

    public void Shuffle<T>(IList<T> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }

    private void PlayNextTrack()
    {
        currentTrackIndex++;

        if (currentTrackIndex >= musicQueue.Count)
        {
            // End of queue
            Debug.Log("Music queue finished.");
            currentTrackIndex = -1;
            return;
        }

        AudioClip nextTrack = musicQueue[currentTrackIndex];
        if (nextTrack == null || nextTrack == null)
        {
            Debug.LogWarning("Missing AudioResource or AudioClip at index " + currentTrackIndex);
            PlayNextTrack(); // skip invalid entries
            return;
        }

        musicSource.clip = nextTrack;
        musicSource.Play();
        Debug.Log("Now playing: " + nextTrack.name);
    }
    #endregion

    #region Sounds
    private void PlayAllSounds(RuntimeDialogueNode node)
    {
        StopAllSounds();

        if (node.dialogueSettings.audioList.GetValue(dialogueBlackboard, out List<AudioClip> audioList))
        {
            foreach (AudioClip clip in audioList)
            {
                if (clip == null)
                    continue;

                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.clip = clip;
                //src.volume = volume;
                src.Play();
                activeSources.Add(src);

                soundCoroutines.Add(StartCoroutine(DestroyAfterPlaying(src)));
            }
        }
    }

    private IEnumerator DestroyAfterPlaying(AudioSource src)
    {
        yield return new WaitForSeconds(src.clip.length);
        activeSources.Remove(src);
        Destroy(src);
    }

    public void StopAllSounds()
    {
        foreach (AudioSource src in activeSources)
        {
            if (src != null)
                Destroy(src);
        }
        foreach (Coroutine coroutine in soundCoroutines)
        {
            StopCoroutine(coroutine);
        }
        activeSources.Clear();
    }
    #endregion

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