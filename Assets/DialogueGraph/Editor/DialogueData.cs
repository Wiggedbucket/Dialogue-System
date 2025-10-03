using System;

[Serializable]
public struct CharacterData
{
    public string name;
    public string spritePath;
    public bool visible;
}

[Serializable]
public struct LocalizedText
{
    public string language;  // e.g. "en", "es"
    public string text;
    public string voiceFilePath;
}
