using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 누름/뗌을 구분하는 터치 버튼(발사·재장전·일시정지·무기 슬롯 공용). 기본은 손가락이 버튼 밖으로 나가면 뗀 것으로 보고, releaseOnExit를 끄면 손가락을 뗄 때까지 유지한다
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
    private readonly TouchPointerTracker pointer = new TouchPointerTracker();

    // false이면 손가락이 버튼 밖으로 나가도 눌림을 유지한다(연사하는 FIRE 버튼용, 2026-10-20)
    public bool releaseOnExit = true;

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

        pointer.TryPress(eventData.pointerId);
        Pressed = true;
        Refresh();
        onDown?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (pointer.TryRelease(eventData.pointerId))
        {
            Release();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (pointer.TryExit(eventData.pointerId, releaseOnExit))
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
        pointer.Clear();
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