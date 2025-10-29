using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueBlackboard : MonoBehaviour
{
    public static DialogueBlackboard Instance { get; private set; }

    private void Awake()
    {
        // Set up singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    [SerializeReference]
    public List<BlackBoardVariableBase> variables = new();

    private Dictionary<string, BlackBoardVariableBase> variableMap;

    public static DialogueBlackboard CreateRuntimeBlackboard(RuntimeDialogueGraph graph, GameObject go)
    {
        DialogueBlackboard bb = go.AddComponent<DialogueBlackboard>();
        bb.variables = new List<BlackBoardVariableBase>();

        foreach (BlackBoardVariableBase v in graph.blackboardVariables)
        {
            // Copy the variable
            var type = v.GetType();
            var clone = Activator.CreateInstance(type) as BlackBoardVariableBase;

            clone.name = v.name;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BlackBoardVariable<>))
            {
                var valueField = type.GetField("Value");
                valueField.SetValue(clone, valueField.GetValue(v));
            }

            bb.variables.Add(clone);
        }

        bb.SetupVariableMap();
        return bb;
    }

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