using UnityEngine;

public class DialogueControls : MonoBehaviour
{
    void Update()
    {
        if (DialogueManager.Instance.InTransition) return;

        if (Input.GetKeyDown(KeyCode.RightShift)) DialogueManager.Instance.StartDialogue();
        if (Input.GetKeyDown(KeyCode.Return)) DialogueManager.Instance.ResumeDialogue();
        if (Input.GetKeyDown(KeyCode.P)) DialogueEvents.Continue();
    }
}
