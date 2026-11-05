using System;
using UnityEngine;
using UnityEngine.UI;

public class Popup : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private Button button;

    private Action _onClick;

    private void Awake()
    {
        button.onClick.AddListener(OnClick);
    }

    public void Show(string text, Action onClick)
    {
        label.text = text;
        _onClick = onClick;
        gameObject.SetActive(true);
    }

    private void OnClick()
    {
        gameObject.SetActive(false);
        _onClick?.Invoke();
    }
}
