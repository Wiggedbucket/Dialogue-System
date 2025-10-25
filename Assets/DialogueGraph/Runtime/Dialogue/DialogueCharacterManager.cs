using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueCharacterManager : MonoBehaviour
{
    private DialogueBlackboard Blackboard = DialogueBlackboard.Instance;

    public Transform characterHolder;
    public TextMeshProUGUI speakerNameText;
    public NotTalkingType notTalkingType = NotTalkingType.None;

    private readonly Dictionary<string, CharacterObject> characterObjects = new();
    private readonly Dictionary<string, Coroutine> positionCoroutines = new();
    private readonly Dictionary<string, Coroutine> rotationCoroutines = new();
    private readonly Dictionary<string, Coroutine> scaleCoroutines = new();

    public int HandleCharacters(RuntimeDialogueNode node)
    {
        List<string> speakerNames = new();

        foreach (CharacterData nodeData in node.characters)
        {
            string name = nodeData.name.GetValue(Blackboard);
            if (string.IsNullOrEmpty(name)) continue;

            CharacterObject character = GetOrCreateCharacter(name);

            StopCharacterCoroutines(name);
            ApplyCharacterBaseTransform(character);
            ApplyCharacterData(character, nodeData);
            ApplyCharacterVisuals(character);
            HandleCharacterMovement(character, character.Data);

            bool talking = character.Data.isTalking.GetValue(Blackboard);
            bool hideName = character.Data.hideName.GetValue(Blackboard);

            if (talking)
            {
                speakerNames.Add(hideName ? "???" : name);
                character.Image.color = Color.white;
            }
            else
            {
                ApplyNotTalkingEffect(character);
            }
        }

        UpdateSpeakerNameplate(speakerNames);
        return speakerNames.Count;
    }

    #region Initialization & Cleanup
    private CharacterObject GetOrCreateCharacter(string name)
    {
        if (characterObjects.TryGetValue(name, out var existing))
            return existing;

        GameObject go = new(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        Image image = go.GetComponent<Image>();

        rect.SetParent(characterHolder, false);

        var newCharacter = new CharacterObject
        {
            Data = new CharacterData(),
            Name = name,
            GameObject = go,
            RectTransform = rect,
            Image = image
        };

        characterObjects[name] = newCharacter;
        return newCharacter;
    }

    public void RemoveAllCharacters()
    {
        foreach (var character in characterObjects.Values)
        {
            Destroy(character.GameObject);
        }
        characterObjects.Clear();
    }

    public void RemoveCharacter(string name)
    {
        if (characterObjects.TryGetValue(name, out var character))
        {
            Destroy(character.GameObject);
            characterObjects.Remove(name);
        }
    }

    public void StopAllCharacterCoroutines()
    {
        foreach (var name in characterObjects.Keys)
        {
            StopCharacterCoroutines(name);
        }
    }

    public void StopCharacterCoroutines(string name)
    {
        if (positionCoroutines.TryGetValue(name, out var pos))
        {
            StopCoroutine(pos);
            positionCoroutines.Remove(name);
        }
        if (rotationCoroutines.TryGetValue(name, out var rot))
        {
            StopCoroutine(rot);
            rotationCoroutines.Remove(name);
        }
        if (scaleCoroutines.TryGetValue(name, out var scale))
        {
            StopCoroutine(scale);
            scaleCoroutines.Remove(name);
        }
    }

    private void ApplyCharacterBaseTransform(CharacterObject character)
    {
        var data = character.Data;
        var bb = Blackboard;

        character.RectTransform.localPosition = data.characterPosition.GetValue(bb);
        character.RectTransform.localEulerAngles = new Vector3(0, 0, data.characterRotation.GetValue(bb));
        character.RectTransform.localScale = data.characterScale.GetValue(bb);
    }
    #endregion

    #region Data Merge
    private void ApplyCharacterData(CharacterObject target, CharacterData incoming)
    {
        DialogueBlackboard bb = Blackboard;
        CharacterData stored = target.Data;

        stored.name = incoming.name;

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

        target.Data = stored;
    }

    private void CopyIfChanged<T>(PortValue<T> target, PortValue<T> source, DialogueBlackboard bb)
    {
        if (!source.GetValue(bb, out _)) return;

        target.usePortValue = source.usePortValue;
        target.blackboardVariableName = source.blackboardVariableName;
        target.value = source.value;
    }
    #endregion

    #region Visuals
    private void ApplyCharacterVisuals(CharacterObject character)
    {
        DialogueBlackboard bb = Blackboard;
        CharacterData data = character.Data;

        if (data.characterSprite.GetValue(bb, out Sprite sprite))
            character.Image.sprite = sprite;

        if (data.isVisible.GetValue(bb, out bool visible))
            character.GameObject.SetActive(visible);

        if (data.minAnchor.GetValue(bb, out Vector2 minAnchor))
            character.RectTransform.anchorMin = minAnchor;

        if (data.maxAnchor.GetValue(bb, out Vector2 maxAnchor))
            character.RectTransform.anchorMax = maxAnchor;

        if (data.pivot.GetValue(bb, out Vector2 pivot))
            character.RectTransform.pivot = pivot;

        if (data.widthAndHeight.GetValue(bb, out Vector2 size))
            character.RectTransform.sizeDelta = size;

        if (data.preserveAspect.GetValue(bb, out bool preserve))
            character.Image.preserveAspect = preserve;
    }

    private void ApplyNotTalkingEffect(CharacterObject character)
    {
        switch (notTalkingType)
        {
            case NotTalkingType.GreyOut:
                character.Image.color = Color.grey;
                break;
            default:
                character.Image.color = Color.white;
                break;
        }
    }
    #endregion

    #region Name Plate
    private void UpdateSpeakerNameplate(List<string> speakerNames)
    {
        string joiner = speakerNames.Count > 2 ? ", " : " and ";
        speakerNameText.text = string.Join(joiner, speakerNames);
    }
    #endregion

    #region Character Movement
    private void HandleCharacterMovement(CharacterObject character, CharacterData data)
    {
        float duration = data.transitionDuration.GetValue(Blackboard, out float d) ? d : 0.4f;

        if (data.characterPosition.GetValue(Blackboard, out Vector2 targetPos))
            positionCoroutines[character.Name] = StartCoroutine(AnimatePosition(character.RectTransform, targetPos, data.positionMovementType, duration));

        if (data.characterRotation.GetValue(Blackboard, out float targetRot))
            rotationCoroutines[character.Name] = StartCoroutine(AnimateRotation(character.RectTransform, targetRot, data.rotationMovementType, duration));

        if (data.characterScale.GetValue(Blackboard, out Vector2 targetScale))
            scaleCoroutines[character.Name] = StartCoroutine(AnimateScale(character.RectTransform, targetScale, data.scaleMovementType, duration));
    }

    private IEnumerator AnimatePosition(RectTransform rect, Vector2 target, PortValue<MovementType> movementType, float duration)
    {
        MovementType type = movementType.GetValue(Blackboard);
        yield return AnimateVector(rect.anchoredPosition, target, duration, type, v => rect.anchoredPosition = v);
    }

    private IEnumerator AnimateRotation(RectTransform rect, float targetRot, PortValue<MovementType> movementType, float duration)
    {
        MovementType type = movementType.GetValue(Blackboard);
        float start = rect.localEulerAngles.z;

        yield return AnimateFloat(start, targetRot, duration, type, r => rect.localEulerAngles = new Vector3(0, 0, r));
    }

    private IEnumerator AnimateScale(RectTransform rect, Vector2 targetScale, PortValue<MovementType> movementType, float duration)
    {
        MovementType type = movementType.GetValue(Blackboard);
        yield return AnimateVector(rect.localScale, targetScale, duration, type, v => rect.localScale = v);
    }

    private IEnumerator AnimateVector(Vector2 start, Vector2 target, float duration, MovementType type, Action<Vector2> setter)
    {
        if (type == MovementType.Instant)
        {
            setter(target);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            if (type == MovementType.Smooth)
                progress = EaseInOut(progress);

            setter(Vector2.Lerp(start, target, progress));
            yield return null;
        }

        setter(target);
    }

    private IEnumerator AnimateFloat(float start, float target, float duration, MovementType type, Action<float> setter)
    {
        if (type == MovementType.Instant)
        {
            setter(target);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            if (type == MovementType.Smooth)
                progress = EaseInOut(progress);

            setter(Mathf.LerpAngle(start, target, progress));
            yield return null;
        }

        setter(target);
    }

    private float EaseInOut(float t) => t * t * (3f - 2f * t);
    #endregion
}

public class CharacterObject
{
    public CharacterData Data = new();
    public string Name;
    public GameObject GameObject;
    public RectTransform RectTransform;
    public Image Image;
}