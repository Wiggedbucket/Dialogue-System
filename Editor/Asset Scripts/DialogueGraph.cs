using UnityEngine;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using System;

[Serializable]
[Graph(AssetExtension)]
public class DialogueGraph : Graph
{
    // Makes the dialogue graph have this extension
    public const string AssetExtension = "dialoguegraph";

    // Creates the dialogue graph file when the menu item is clicked
    [MenuItem("Assets/Create/Dialogue Graph", false)]
    private static void CreateAssetFile()
    {
        GraphDatabase.PromptInProjectBrowserToCreateNewAsset<DialogueGraph>();
    }
}
