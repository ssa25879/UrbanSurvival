using UnityEngine;
using UnityEngine.EventSystems;

// 플로팅 가상 스틱. 이 오브젝트(투명 터치 영역) 안을 처음 누른 위치가 스틱 중심이 되고, 손을 떼면 숨는다
// 포인터마다 독립된 이벤트라 다른 버튼과 동시에 눌러도 서로 영향이 없다
public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler {
    public float radius = 110f;     // 캔버스 단위(px)
    public float deadZone = 0.2f;
    public RectTransform baseImage; // 스틱 바탕(원)
    public RectTransform knob;      // 스틱 손잡이
    public System.Action<Vector2> onValue; // 데드존 적용 후 값(크기 1 이하)

    private RectTransform zone;
    private int activePointerId = int.MinValue;
    private Vector2 centerLocal;

    private void Awake() {
        zone = (RectTransform)transform;
        SetVisible(false);
    }

    private void OnDisable() {
        ResetStick();
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (activePointerId != int.MinValue)
        {
            return; // 이미 다른 손가락이 잡고 있음
        }

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, eventData.position, eventData.pressEventCamera, out local))
        {
            return;
        }

        activePointerId = eventData.pointerId;
        centerLocal = local;
        baseImage.anchoredPosition = ZoneLocalToAnchored(local);
        knob.anchoredPosition = Vector2.zero;
        SetVisible(true);
        Emit(Vector2.zero);
    }

    public void OnDrag(PointerEventData eventData) {
        if (eventData.pointerId != activePointerId)
        {
            return;
        }

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, eventData.position, eventData.pressEventCamera, out local))
        {
            return;
        }

        Vector2 offset = local - centerLocal;
        knob.anchoredPosition = Vector2.ClampMagnitude(offset, radius);
        Emit(JoystickMath.Evaluate(offset, radius, deadZone));
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (eventData.pointerId != activePointerId)
        {
            return;
        }

        ResetStick();
    }

    public void ResetStick() {
        activePointerId = int.MinValue;
        if (baseImage != null)
        {
            SetVisible(false);
        }
        Emit(Vector2.zero);
    }

    private void Emit(Vector2 value) {
        onValue?.Invoke(value);
    }

    private void SetVisible(bool visible) {
        if (baseImage != null)
        {
            baseImage.gameObject.SetActive(visible);
        }
    }

    // 영역 로컬 좌표를 baseImage 부모 기준 anchoredPosition으로 변환(영역 중앙 피벗·중앙 앵커 가정)
    private Vector2 ZoneLocalToAnchored(Vector2 local) {
        return local;
    }
}
