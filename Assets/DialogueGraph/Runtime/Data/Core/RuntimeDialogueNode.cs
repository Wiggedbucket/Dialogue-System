using System;
using System.Collections.Generic;

[Serializable]
public class RuntimeDialogueNode : RuntimeNode
{
    public string dialogueText;
    public DialogueSettings dialogueSettings;

    public List<CharacterData> characters = new();
    public List<RuntimeChoice> choices = new();
}
