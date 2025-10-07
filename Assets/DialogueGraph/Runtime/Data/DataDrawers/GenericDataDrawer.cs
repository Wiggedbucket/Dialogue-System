using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class GenericDataDrawer
{
    public static VisualElement Draw(SerializedProperty property)
    {
        var container = new VisualElement();
        container.style.paddingLeft = 4;

        var fold = new Foldout
        {
            text = property.displayName,
            value = true
        };

        // Enumerate all visible child properties
        var iterator = property.Copy();
        var end = iterator.GetEndProperty();

        iterator.NextVisible(true);
        while (!SerializedProperty.EqualContents(iterator, end))
        {
            var field = new PropertyField(iterator.Copy());
            fold.Add(field);
            iterator.NextVisible(false);
        }

        container.Add(fold);
        return container;
    }
}
