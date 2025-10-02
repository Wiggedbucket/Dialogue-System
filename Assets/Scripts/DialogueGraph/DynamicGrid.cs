using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(GridLayoutGroup))]
public class DynamicGrid : MonoBehaviour
{
    private GridLayoutGroup grid;

    public float spacingVertical = 0f;
    public float spacingHorizontal = 0f;

    public float paddingVertical = 20f;
    public float paddingHorizontal = 20f;

    private void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
    }

    void Update()
    {
        float maxWidth = 0f;
        float maxHeight = 0f;

        foreach (Transform child in grid.transform)
        {
            var text = child.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                Vector2 size = text.GetPreferredValues();
                maxWidth = Mathf.Max(maxWidth, size.x + paddingHorizontal);
                maxHeight = Mathf.Max(maxHeight, size.y + paddingVertical);
            }
        }

        grid.cellSize = new Vector2(maxWidth, maxHeight);
        grid.spacing = new Vector2(spacingHorizontal, spacingVertical);
    }
}
