using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OriginTitleSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public GameObject button;
        public GameObject selectionFx;
        public CanvasGroup bakedStartDimmer;
    }

    public List<Entry> entries = new();
    public int defaultIndex = 0;

    private int currentIndex = -1;

    private void OnEnable()
    {
        SelectIndex(defaultIndex, true);
    }

    public void SelectIndex(int index, bool setEventSelection)
    {
        if (entries == null || entries.Count == 0)
            return;

        index = Mathf.Clamp(index, 0, entries.Count - 1);
        currentIndex = index;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null) continue;

            if (entry.selectionFx != null)
                entry.selectionFx.SetActive(i == currentIndex);

            if (entry.bakedStartDimmer != null)
                entry.bakedStartDimmer.alpha = (i == 0 && currentIndex != 0) ? 0.14f : 0f;
        }

        if (setEventSelection && EventSystem.current != null)
        {
            GameObject go = entries[currentIndex]?.button;
            if (go != null && EventSystem.current.currentSelectedGameObject != go)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(go);
            }
        }
    }
}
