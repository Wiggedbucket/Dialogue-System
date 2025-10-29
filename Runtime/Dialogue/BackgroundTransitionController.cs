using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundTransitionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image blackBackground; // For when a background is set, if the background doesn't cover the screen it will be black.
    [SerializeField] private Image primaryImage;   // current visible background
    [SerializeField] private Image secondaryImage; // new image used for transitions

    [Header("Transition Settings")]
    public BackgroundTransition backgroundTransition = BackgroundTransition.None;
    public float transitionDuration = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine transitionCoroutine;

    private Sprite currentBackground = null;
    private Sprite newBackground = null;

    public void ResetController()
    {
        blackBackground.enabled = false;
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

        blackBackground.enabled = newSprite != null;
        SetColorAlpha(blackBackground, 1f);
    }

    public void TransitionTo(Sprite newSprite)
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        SetImmediate(currentBackground); // Ensure we start from current background
        newBackground = newSprite;
        transitionCoroutine = StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        if (currentBackground == null)
            SetColorAlpha(blackBackground, 0f);
        if (newBackground != null)
            blackBackground.enabled = true;

        // Prepare secondary image
        secondaryImage.sprite = newBackground;
        secondaryImage.enabled = true;
        SetColorAlpha(secondaryImage, 0f);

        float timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = transitionCurve.Evaluate(timer / transitionDuration);

            switch (backgroundTransition)
            {
                case BackgroundTransition.Fade:
                    float halfT = t * 2f;
                    if (halfT <= 1f)
                    {
                        // Fade out primary
                        if (currentBackground != null)
                            SetColorAlpha(primaryImage, 1f - halfT);
                        if (newBackground == null && currentBackground != null)
                            SetColorAlpha(blackBackground, 1f - halfT);
                    }
                    else
                    {
                        // Fade in secondary
                        if (newBackground != null)
                            SetColorAlpha(secondaryImage, halfT - 1f);
                        if (newBackground != null && currentBackground == null)
                            SetColorAlpha(blackBackground, halfT - 1f);
                    }
                    break;
            }

            yield return null;
        }

        // Finalize transition
        blackBackground.enabled = newBackground != null;

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
        Color imageColor = image.color;
        imageColor.a = alpha;
        image.color = imageColor;
        return imageColor;
    }
}
