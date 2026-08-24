using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class OriginUIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public float hoverScale = 1.035f;
    public float pressScale = 0.97f;
    public float response = 18f;
    public Color normalTint = Color.white;
    public Color hoverTint = new Color(1f, 0.82f, 0.9f, 1f);
    public Color pressedTint = new Color(1f, 0.82f, 0.48f, 1f);

    private RectTransform rect;
    private Graphic graphic;
    private Vector3 targetScale = Vector3.one;
    private Color targetTint;

    private void Awake()
    {
        rect = (RectTransform)transform;
        graphic = GetComponent<Graphic>();
        targetTint = normalTint;
    }

    private void Update()
    {
        if (rect != null)
            rect.localScale = Vector3.Lerp(rect.localScale, targetScale, 1f - Mathf.Exp(-response * Time.unscaledDeltaTime));
        if (graphic != null)
            graphic.color = Color.Lerp(graphic.color, targetTint, 1f - Mathf.Exp(-response * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData eventData) { targetScale = Vector3.one * hoverScale; targetTint = hoverTint; }
    public void OnPointerExit(PointerEventData eventData) { targetScale = Vector3.one; targetTint = normalTint; }
    public void OnPointerDown(PointerEventData eventData) { targetScale = Vector3.one * pressScale; targetTint = pressedTint; }
    public void OnPointerUp(PointerEventData eventData) { targetScale = Vector3.one * hoverScale; targetTint = hoverTint; }
    public void OnSelect(BaseEventData eventData) { targetScale = Vector3.one * hoverScale; targetTint = hoverTint; }
    public void OnDeselect(BaseEventData eventData) { targetScale = Vector3.one; targetTint = normalTint; }
}
