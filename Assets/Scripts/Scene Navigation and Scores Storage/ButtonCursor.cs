using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonCursor : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public GameObject cursorImage;

    void Awake()
    {
        if (cursorImage != null)
            cursorImage.SetActive(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!IsControllerConnected())
            return;

        if (cursorImage != null)
            cursorImage.SetActive(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (cursorImage != null)
            cursorImage.SetActive(false);
    }

    public bool IsControllerConnected()
    {
        return InputMode.IsControllerConnected();
    }
}
