using System.Collections;
using UnityEngine;

public class DialogueBoxTransitionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform dialogueBox; // the main panel (parent of text + nameplate)
    [SerializeField] private CanvasGroup canvasGroup;   // used for fading

    [Header("Settings")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector2 defaultPos;

    private void Awake()
    {
        if (dialogueBox == null)
            dialogueBox = GetComponent<RectTransform>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        defaultPos = dialogueBox.anchoredPosition;
    }

    public IEnumerator PlayTransition(DialogueBoxTransition transition, bool isIn)
    {
        if (transition == DialogueBoxTransition.None)
        {
            // Instantly show/hide
            canvasGroup.alpha = isIn ? 1 : 0;
            yield break;
        }

        Vector2 startPos = defaultPos;
        Vector2 endPos = defaultPos;

        float startAlpha = isIn ? 0f : 1f;
        float endAlpha = isIn ? 1f : 0f;

        canvasGroup.alpha = startAlpha;

        switch (transition)
        {
            case DialogueBoxTransition.SlideUp:
                startPos = defaultPos + new Vector2(0, -Screen.height * 0.3f);
                break;
            case DialogueBoxTransition.SlideDown:
                startPos = defaultPos + new Vector2(0, Screen.height * 0.3f);
                break;
            case DialogueBoxTransition.SlideLeft:
                startPos = defaultPos + new Vector2(Screen.width * 0.3f, 0);
                break;
            case DialogueBoxTransition.SlideRight:
                startPos = defaultPos + new Vector2(-Screen.width * 0.3f, 0);
                break;
        }

        // Initialize start
        dialogueBox.anchoredPosition = isIn ? startPos : defaultPos;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / transitionDuration);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            dialogueBox.anchoredPosition = Vector2.Lerp(isIn ? startPos : defaultPos, isIn ? defaultPos : startPos, t);

            yield return null;
        }

        // Clamp to end
        canvasGroup.alpha = endAlpha;
        dialogueBox.anchoredPosition = defaultPos;
    }
}
