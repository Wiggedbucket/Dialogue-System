using TMPro;
using UnityEngine;

public class TextAutoSizeHorizontally : MonoBehaviour
{
    public TextMeshProUGUI text;

    private void LateUpdate()
    {
        if (text == null)
            return;

        // Force the TextMeshProUGUI component to update its layout
        text.ForceMeshUpdate();

        // Get the preferred height based on the current text
        float preferredHeight = text.textBounds.size.y;

        // Adjust the RectTransform height
        if (text.TryGetComponent<RectTransform>(out var rectTransform))
        {
            Vector2 sizeDelta = rectTransform.sizeDelta;
            sizeDelta.y = preferredHeight;
            rectTransform.sizeDelta = sizeDelta;
        }
    }
}
