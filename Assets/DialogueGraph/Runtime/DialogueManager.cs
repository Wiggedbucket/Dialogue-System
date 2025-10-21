using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public RuntimeDialogueGraph runtimeGraph;
    public DialogueBlackboard dialogueBlackboard;

    private RuntimeNode currentNode;
    
    public bool dialogueRunning = false;

    public static event Action<string> OnStringBroadcast;

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

    [Header("Audio Sources")]
    public AudioSource musicSource;

    [Header("Settings")]
    public bool onHold = false;
    public bool allowEscape = false;
    public bool allowFastAdvance = true;
    public bool autoAdvance = false;
    public bool enableSkipping = false;
    public bool textShadowOnMultipleCharactersTalking = false;

    public float printSpeed = 0.02f;

    [Header("Dialogue Variables")]
    private Coroutine typingCoroutine;
    private Coroutine delayNodeCoroutine;
    private string currentFullText = "";
    public bool delayNextWithClick = false;
    private bool isTyping = false;

    [Header("Sound Variables")]
    public List<AudioClip> musicQueue = new();
    public int currentTrackIndex = -1;
    public bool loop = true;
    public bool shuffle = false;

    public List<AudioSource> activeSources = new();
    private List<Coroutine> soundCoroutines = new();

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
            if (!isTyping && !delayNextWithClick && delayNodeCoroutine == null)
            {
                GoToNextNode();
            }
            else if (Mouse.current.leftButton.wasPressedThisFrame)
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
        dialogueRunning = true;
        allowEscape = runtimeGraph.allowEscape;
        allowFastAdvance = runtimeGraph.allowFastAdvance;
        textShadowOnMultipleCharactersTalking = runtimeGraph.textShadowOnMultipleCharactersTalking;
        loop = true;
        shuffle = false;
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

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
        dialoguePanel.SetActive(false);
        dialogueRunning = false;
        StopAllSounds();
        musicSource.Stop();
        currentNode = null;
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        if (delayNodeCoroutine != null)
            StopCoroutine(delayNodeCoroutine);

        // Clear all choice buttons
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }
    #endregion

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

        dialogueTextShadow.gameObject.SetActive(speakerNames.Count > 1);

        HandleDialogueText(node);

        CreateChoices(node);

        ChangeMusicQueue(node);

        PlayAllSounds(node);
    }

    #region Dialogue
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

        // Type only the new part
        for (int i = startIndex; i < currentFullText.Length; i++)
        {
            dialogueText.text = currentFullText.Substring(0, i + 1);
            yield return new WaitForSeconds(speed);
        }

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