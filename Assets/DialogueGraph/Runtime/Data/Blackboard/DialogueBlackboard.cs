using System.Collections.Generic;
using UnityEngine;

public class DialogueBlackboard : MonoBehaviour
{
    [SerializeReference]
    public List<BlackBoardVariableBase> variables = new();

    private Dictionary<string, BlackBoardVariableBase> variableMap;

    public void SetupVariableMap()
    {
        variableMap = new Dictionary<string, BlackBoardVariableBase>();
        foreach (var v in variables)
            variableMap[v.name] = v;
    }

    public bool TryGetValue<T>(string name, out T value)
    {
        if (variableMap.TryGetValue(name, out var bbBase) && bbBase is BlackBoardVariable<T> bbVar)
        {
            value = bbVar.GetValue();
            return true;
        }

        value = default;
        return false;
    }

    public void SetValue<T>(string name, T value)
    {
        if (variableMap.TryGetValue(name, out var bbBase) && bbBase is BlackBoardVariable<T> bbVar)
        {
            bbVar.SetValue(value);
        }
    }
}