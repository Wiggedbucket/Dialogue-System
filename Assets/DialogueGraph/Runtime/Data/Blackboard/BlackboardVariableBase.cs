using System;
using UnityEngine;

[Serializable]
public abstract class BlackBoardVariableBase
{
    [HideInInspector]
    public string name;
}

[Serializable]
public class BlackBoardVariable<T> : BlackBoardVariableBase
{
    public T Value;

    public void SetValue(T value) => Value = value;
    public T GetValue() => Value;
}