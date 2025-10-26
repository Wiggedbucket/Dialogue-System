using System;

public static class DialogueEvents
{
    // External systems can call Continue() to progress when waiting for an external signal.
    public static event Action OnContinueDialogue;

    // Fired when a dialogue node starts
    public static event Action<string> OnDialogueStarted;

    // Fired by UI when text typing completed for a dialogue node.
    public static event Action<string> OnDialogueTextComplete;

    // Fired by UI when player selects a choice; The string is the option text
    public static event Action<string> OnChoiceSelected;

    // Broadcast string from nodes; The string can be anything, e.g., a command or message
    public static event Action<string> OnStringBroadcast;

    public static void Continue() => OnContinueDialogue?.Invoke();

    internal static void RaiseDialogueStarted(string nodeID) => OnDialogueStarted?.Invoke(nodeID);
    internal static void RaiseDialogueTextComplete(string nodeID) => OnDialogueTextComplete?.Invoke(nodeID);
    internal static void RaiseChoiceSelected(string optionText) => OnChoiceSelected?.Invoke(optionText);
    internal static void RaiseStringBroadcast(string s) => OnStringBroadcast?.Invoke(s);
}