using UnityEngine;
using UnityEngine.EventSystems;

public class OriginTitleButtonSelectionFx : MonoBehaviour,
    IPointerEnterHandler, ISelectHandler
{
    public OriginTitleSelectionManager manager;
    public int index;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (manager != null)
            manager.SelectIndex(index, true);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (manager != null)
            manager.SelectIndex(index, false);
    }
}
