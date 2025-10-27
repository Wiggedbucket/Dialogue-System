using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundTransitionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image primaryImage;   // current visible background
    [SerializeField] private Image secondaryImage; // new image used for transitions

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 1.0f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine transitionCoroutine;

    private Sprite currentBackground = null;
    private Sprite newBackground = null;

    public void ResetController()
    {
        primaryImage.sprite = null;
        primaryImage.enabled = false;
        secondaryImage.sprite = null;
        secondaryImage.enabled = false;

        currentBackground = null;
        newBackground = null;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }

    public void SetImmediate(Sprite newSprite)
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        primaryImage.sprite = newSprite;
        primaryImage.enabled = newSprite != null;
        secondaryImage.sprite = null;
        secondaryImage.enabled = false;

        currentBackground = newSprite;
        newBackground = null;

        SetColorAlpha(primaryImage, 1f);
        SetColorAlpha(secondaryImage, 0f);
    }

    public void TransitionTo(Sprite newSprite, BackgroundTransition transition)
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        SetImmediate(currentBackground); // Ensure we start from current background
        newBackground = newSprite;
        transitionCoroutine = StartCoroutine(TransitionRoutine(transition));
    }

    private IEnumerator TransitionRoutine(BackgroundTransition transition)
    {
        // Prepare secondary image
        secondaryImage.sprite = newBackground;
        secondaryImage.enabled = true;
        Color secondaryStartColor = SetColorAlpha(secondaryImage, 0f);

        float timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = transitionCurve.Evaluate(timer / transitionDuration);

            switch (transition)
            {
                case BackgroundTransition.Fade:
                    float halfT = t * 2f;
                    if (halfT <= 1f)
                    {
                        // Fade out primary
                        primaryImage.color = new Color(1f, 1f, 1f, 1f - halfT);
                    }
                    else
                    {
                        // Fade in secondary
                        secondaryImage.color = new Color(1f, 1f, 1f, halfT - 1f);
                    }
                    break;
            }

            yield return null;
        }

        // Finalize transition
        primaryImage.sprite = newBackground;
        primaryImage.enabled = newBackground != null;
        SetColorAlpha(primaryImage, 1f);

        secondaryImage.sprite = null;
        secondaryImage.enabled = false;
        SetColorAlpha(secondaryImage, 0f);

        currentBackground = newBackground;
        newBackground = null;

        transitionCoroutine = null;
    }

    private Color SetColorAlpha(Image image, float alpha)
    {
        Color imageColor = primaryImage.color;
        imageColor.a = alpha;
        image.color = imageColor;
        return imageColor;
    }
}
