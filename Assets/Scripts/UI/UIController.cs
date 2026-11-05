using System;
using System.Collections;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private InputButton input;
    [SerializeField] private Popup popup;
    [SerializeField] private CanvasGroup fade;
    [SerializeField] private float fadeDuration = 0.35f;

    public InputButton InputButton => input;

    public void ShowPopup(string text, Action onClick)
    {
        popup.Show(text, onClick);
    }

    public void Fade(Action onDark)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(onDark));
    }

    private IEnumerator FadeRoutine(Action onDark)
    {
        fade.blocksRaycasts = true;
        yield return FadeTo(1f);
        onDark?.Invoke();
        yield return FadeTo(0f);
        fade.blocksRaycasts = false;
    }

    private IEnumerator FadeTo(float target)
    {
        float from = fade.alpha;
        for (float t = 0f; t < 1f; t += Time.deltaTime / fadeDuration)
        {
            fade.alpha = Mathf.Lerp(from, target, t);
            yield return null;
        }
        fade.alpha = target;
    }
}
