using System.Collections;
using TMPro;
using UnityEngine;

public class OriginToastController : MonoBehaviour
{
    public GameObject toastRoot;
    public CanvasGroup canvasGroup;
    public TMP_Text messageText;
    public float visibleSeconds = 1.6f;
    public float fadeSeconds = 0.2f;

    private Coroutine routine;

    private void Awake()
    {
        if (toastRoot != null) toastRoot.SetActive(false);
    }

    public void Show(string message)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        if (toastRoot == null) yield break;
        toastRoot.SetActive(true);
        if (messageText != null) messageText.text = message;
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeSeconds));
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(visibleSeconds);

        t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            if (canvasGroup != null) canvasGroup.alpha = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, fadeSeconds));
            yield return null;
        }

        toastRoot.SetActive(false);
        routine = null;
    }
}
