using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueBlackboard : MonoBehaviour
{
    [SerializeReference]
    public List<BlackBoardVariableBase> variables = new();

    private Dictionary<string, BlackBoardVariableBase> variableMap;

    private void Awake()
    {
        variableMap = new Dictionary<string, BlackBoardVariableBase>();
        foreach (var v in variables)
            variableMap[v.name] = v;
    }

    public bool TryGetBlackBoardValue<T>(string name, out T value)
    {
        if (variableMap.TryGetValue(name, out var bbBase) && bbBase is BlackBoardVariable<T> bbVar)
        {
            value = bbVar.GetValue();
            return true;
        }

        value = default;
        return false;
    }

    public void SetBlackBoardValue<T>(string name, T value)
    {
        if (variableMap.TryGetValue(name, out var bbBase) && bbBase is BlackBoardVariable<T> bbVar)
        {
            bbVar.SetValue(value);
        }
    }
}

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