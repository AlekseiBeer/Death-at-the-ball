using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Параметры карты")]
    public bool IsHidden = false;
    public bool IsOpen = false;
    protected bool currentIsOpen = false;
    [SerializeField] protected bool SpecialRotation = false;

    [Header("Спрайты карты")]
    [SerializeField] protected Sprite frontSprite;
    [SerializeField] protected Sprite backSprite;

    [Header("Параметры анимации переворота")]
    [SerializeField] protected float flipDuration = 0.5f;
    protected bool isFlipping = false;

    // Рендеры лицевой и обратной сторон
    protected SpriteRenderer frontRenderer;
    protected SpriteRenderer backRenderer;
    protected SpriteRenderer outlineRenderer;

    protected virtual void Awake()
    {
        frontRenderer = transform.Find("Front").GetComponent<SpriteRenderer>();
        backRenderer = transform.Find("Back").GetComponent<SpriteRenderer>();
        outlineRenderer = transform.Find("Outline").GetComponent<SpriteRenderer>();

        frontRenderer.sprite = frontSprite;
        backRenderer.sprite = backSprite;

        UpdateVisibility();

        if (currentIsOpen != IsOpen)
        {
            FlipCard();
        }
    }

    // Переворот карты (вращение на 180° по оси Y)
    protected void FlipCard()
    {
        if (!isFlipping)
        {
            StartCoroutine(FlipAnimation());
        }
    }

    // Корутина для плавной анимации переворота
    protected virtual IEnumerator FlipAnimation()
    {
        isFlipping = true;

        float elapsedTime = 0f;
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, 180f, 0);

        if (SpecialRotation)
        {
            endRotation = endRotation * Quaternion.Euler(0, 0, 90f);
        }

        while (elapsedTime < flipDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / flipDuration);
            transform.rotation = Quaternion.Lerp(startRotation, endRotation, t);
            yield return null;
        }

        transform.rotation = endRotation;
        currentIsOpen = !currentIsOpen;
        IsOpen = currentIsOpen;
        // После переворота обновляем цвет outline в зависимости от состояния карты
        UpdateVisibility();
        isFlipping = false;
    }

    public virtual void UpdateVisibility()
    {
        if (IsHidden)
        {
            // "Размытие": снижаем альфа для лицевой и обратной сторон
            SetSpriteAlpha(frontRenderer, 0.3f);
            SetSpriteAlpha(backRenderer, 0.3f);
            // Отключаем Outline, делая его полностью прозрачным
            SetSpriteAlpha(outlineRenderer, 0f);
        }
        else
        {
            // В нормальном состоянии – альфа = 1
            SetSpriteAlpha(frontRenderer, 1f);
            SetSpriteAlpha(backRenderer, 1f);
            // Outline: зеленый, если открыта, красный если закрыта
            outlineRenderer.color = currentIsOpen ? Color.green : Color.red;
            SetSpriteAlpha(outlineRenderer, 1f);
        }
    }

    protected void SetSpriteAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    // Обработка кликов
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (IsHidden)
            return;

        // Левый клик => зум камеры (повторное нажатие — возврат)
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            CameraController.Instance.ToggleZoom(transform.position);
        }
        // Средний клик (колесико) => переворот карты
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            FlipCard();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsHidden)
        {
            // При наведении временно делаем Outline белым
            SetSpriteAlpha(outlineRenderer, 1f);
            outlineRenderer.color = Color.white;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsHidden)
        {
            // При уходе курсора восстанавливаем цвет Outline
            outlineRenderer.color = currentIsOpen ? Color.green : Color.red;
        }
    }

    protected void UpdateColorOutLine() => outlineRenderer.color = currentIsOpen ? Color.green : Color.red;

}