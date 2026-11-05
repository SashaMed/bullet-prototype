using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Door : MonoBehaviour
{
    private static readonly int OpenTrigger = Animator.StringToHash("Open");
    private static readonly int ClosedState = Animator.StringToHash("Closed");

    private Animator animator;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Open()
    {
        if (IsOpen)
            return;

        IsOpen = true;
        animator.SetTrigger(OpenTrigger);
    }

    public void Close()
    {
        IsOpen = false;
        animator.ResetTrigger(OpenTrigger);
        animator.Play(ClosedState, 0, 0f);
    }
}
