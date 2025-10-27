using System;

[Serializable]
public enum NotTalkingType
{
    None,
    GreyOut,
    //ScaleDown,
}

[Serializable]
public enum PredefinedPosition
{
    None,
    Left,
    MiddleLeft,
    MiddleRight,
    Right,
}

[Serializable]
public enum MovementType
{
    Instant,
    Linear,
    Smooth
}

[Serializable]
public enum BackgroundTransition
{
    None,
    Fade,
}

[Serializable]
public enum DialogueBoxTransition
{
    None,
    FadeIn,
    SlideUp,
    SlideDown,
    SlideLeft,
    SlideRight,
}

[Serializable]
public enum ComparisonType
{
    Equal,
    NotEqual,
    Greater,
    Less,
    GreaterOrEqual,
    LessOrEqual,
}

[Serializable]
public enum VariableType
{
    Float,
    Int,
    Bool,
    String,
}