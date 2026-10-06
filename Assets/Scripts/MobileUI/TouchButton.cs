using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 누름/뗌을 구분하는 터치 버튼(발사·재장전·일시정지·무기 슬롯 공용). 손가락이 버튼 밖으로 나가면 뗀 것으로 본다
public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public System.Action onDown;
    public System.Action onUp;
    public Image background;
    public Color normalColor = MobileUIFactory.Panel;
    public Color pressedColor = MobileUIFactory.Amber;
    public Color disabledColor = MobileUIFactory.Dim;

    // 선택 사항: 지정하면 누르는 동안 글자색을 바꾼다(앰버 채움 위에서 어두운 글자로 읽히게)
    public Text label;
    public Color labelNormalColor = MobileUIFactory.Light;
    public Color labelPressedColor = new Color(0.07f, 0.08f, 0.09f, 1f);

    public bool Pressed { get; private set; }
    private bool interactable = true;
    private int activePointerId = int.MinValue;

    private void OnDisable()
    {
        Release();
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        if (!value)
        {
            Release();
        }

        Refresh();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactable || Pressed)
        {
            return;
        }

        activePointerId = eventData.pointerId;
        Pressed = true;
        Refresh();
        onDown?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
        {
            Release();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
        {
            Release();
        }
    }

    public void Release()
    {
        if (!Pressed)
        {
            return;
        }

        Pressed = false;
        activePointerId = int.MinValue;
        Refresh();
        onUp?.Invoke();
    }

    private void Refresh()
    {
        if (background != null)
        {
            background.color = !interactable ? disabledColor : (Pressed ? pressedColor : normalColor);
        }

        if (label != null)
        {
            label.color = Pressed ? labelPressedColor : labelNormalColor;
        }
    }
}