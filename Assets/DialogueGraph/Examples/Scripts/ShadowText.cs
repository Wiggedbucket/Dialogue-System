using TMPro;
using UnityEngine;

[ExecuteAlways]
public class ShadowText : MonoBehaviour
{
    public TextMeshProUGUI mainText;
    public TextMeshProUGUI shadowText;

    public float offsetX = 2f;
    public float offsetY = -2f;

    private void LateUpdate()
    {
        if (mainText == null || shadowText == null)
            return;

        // Copy all key properties
        //shadowText.text = mainText.text;
        shadowText.font = mainText.font;
        shadowText.fontSize = mainText.fontSize;
        //shadowText.color = mainText.color;
        shadowText.alignment = mainText.alignment;
        shadowText.lineSpacing = mainText.lineSpacing;
        shadowText.characterSpacing = mainText.characterSpacing;
        shadowText.wordSpacing = mainText.wordSpacing;
        shadowText.richText = mainText.richText;

        shadowText.rectTransform.anchoredPosition = mainText.rectTransform.anchoredPosition + new Vector2(offsetX, offsetY);
    }
}
