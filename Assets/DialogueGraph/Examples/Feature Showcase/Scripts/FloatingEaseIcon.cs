using UnityEngine;

/// <summary>
/// Moves an image (e.g., a continue arrow) left and right with smooth easing.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FloatingEaseIcon : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveDistance = 10f;    // Distance from center
    [SerializeField] private float cycleDuration = 2f;    // Time to move from left -> right -> left
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private float timer;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;

        // Optional default curve if not assigned
        if (easeCurve == null || easeCurve.length == 0)
            easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    private void Update()
    {
        if (cycleDuration <= 0f)
            return;

        // 0 -> 1 -> 0 cycle
        timer += Time.unscaledDeltaTime;
        float normalized = (timer % cycleDuration) / cycleDuration;

        // Use a ping-pong motion
        float t = Mathf.PingPong(normalized * 2f, 1f); // goes 0->1->0
        float curveValue = easeCurve.Evaluate(t);

        // Apply horizontal movement using the curve value
        float offset = Mathf.Lerp(-moveDistance, moveDistance, curveValue);
        rectTransform.anchoredPosition = startPosition + new Vector2(offset, 0);
    }
}
