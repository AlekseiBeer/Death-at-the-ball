using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum CardStatus
{
    Closed,    // Карта закрыта (лицо вниз)
    Closing,
    Opened,    // Карта открыта (лицо вверх)
    Opening
}

public enum CardInteractionState
{
    Active,    // Карта активна, можно взаимодействовать
    Inactive,   // Карта неактивна, клики игнорируются
    Hidden
}

public class Card : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Параметры карты")]
    [SerializeField] private CardStatus _initialStatus = CardStatus.Closed;
    [HideInInspector] public CardStatus status = CardStatus.Closed;

    [SerializeField] private CardInteractionState _interactionState = CardInteractionState.Active;
    public CardInteractionState InteractionState
    {
        get => _interactionState;
        set
        {
            if (_interactionState != value)
            {
                _interactionState = value;
                UpdateVisibility();
            }
        }
    }
    [SerializeField] protected bool SpecialRotation = false;

    // Новые поля для локационной карты
    [Header("Параметры локационной карты")]
    public bool isLocationCard = false;
    // Если isLocationCard == true, то при зуме целевая точка = transform.position + customZoomOffset
    public Vector3 customZoomOffset = Vector3.zero;
    public float customZoomSize = 14;

    [Header("Параметры очков")]
    public bool costsPoint = true;

    [Header("Спрайты карты")]
    [SerializeField] protected Sprite frontSprite;
    [SerializeField] protected Sprite backSprite;

    [Header("Параметры анимации переворота")]
    [SerializeField] protected float flipDuration = 0.5f;

    // Рендеры лицевой и обратной сторон
    protected SpriteRenderer frontRenderer;
    protected SpriteRenderer backRenderer;
    protected SpriteRenderer outlineRenderer;

    public List<Card> RelatedCardsHide = new List<Card>();
    public List<Card> RelatedCardsOpen = new List<Card>();
    public List<Card> CardActivator = new List<Card>();

    [HideInInspector] public int NumAddictions = 0;

    void Awake()
    {
        frontRenderer = transform.Find("Front").GetComponent<SpriteRenderer>();
        backRenderer = transform.Find("Back").GetComponent<SpriteRenderer>();
        outlineRenderer = transform.Find("Outline").GetComponent<SpriteRenderer>();

        frontRenderer.sprite = frontSprite;
        backRenderer.sprite = backSprite;
    }

    void Start()
    {
        UpdateVisibility();

        if (status != _initialStatus)
            FlipCard();

        UpdateRelatedCardsHide();
    }

    // Метод возвращает целевую точку зума. Если карта локационная – учитываем смещение.
    public Vector3 GetZoomPosTarget() => isLocationCard ? transform.position + new Vector3(customZoomOffset.x - 1f, 0, 0) : transform.position;
    private float GetZoomSizeTarget() => isLocationCard ? customZoomSize : 5.25f;

    void Update()
    {
        if (InteractionState != CardInteractionState.Hidden)
        {
            foreach (Card card in CardActivator)
            {
                if (card.status == CardStatus.Closed)
                    InteractionState = CardInteractionState.Inactive;
                else
                    InteractionState = CardInteractionState.Active;
            }
        }
    }

    protected void UpdateRelatedCardsHide()
    {
        foreach (Card card in RelatedCardsHide)
        {
            if (status == CardStatus.Closed)
                card.InteractionState = CardInteractionState.Hidden;
            if (status == CardStatus.Opened)
                card.InteractionState = CardInteractionState.Active;
        }
    }

    protected void UpdateRelatedCardsOpen()
    {
        foreach (Card card in RelatedCardsOpen)
        {
            if (status == CardStatus.Opened)
            {
                card.InteractionState = CardInteractionState.Active;
                if (card.status == CardStatus.Closed)
                {
                    card.FlipCard();
                }
                card.NumAddictions++;
            }
            else
            {
                if (card.status == CardStatus.Opened && card.NumAddictions <= 1)
                {
                    card.InteractionState = CardInteractionState.Hidden;
                    card.FlipCard();
                }
                card.NumAddictions--;
            }
        }
    }
    

    protected void FlipCard()
    {
        if (status == CardStatus.Opening || status == CardStatus.Closing)
            return;

        // Если карта должна открываться, а уже нет открытий, не разрешаем переворот
        if (status == CardStatus.Closed && costsPoint && !GameManager.Instance.CanOpenCard())
            return;

        status = (status == CardStatus.Closed) ? CardStatus.Opening : CardStatus.Closing;
        StartCoroutine(FlipAnimation());
    }

    // Корутина для плавной анимации переворота
    protected IEnumerator FlipAnimation()
    {
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
        status = (status == CardStatus.Opening) ? CardStatus.Opened : CardStatus.Closed;

        if (status == CardStatus.Opened && costsPoint)
        {
            GameManager.Instance.OnCardOpened(this);
        }
        if (status == CardStatus.Closed && costsPoint)
        {
            GameManager.Instance.OnCardClosed(this);
        }

        UpdateRelatedCardsHide();
        UpdateRelatedCardsOpen();
        UpdateVisibility();
    }

    public virtual void UpdateVisibility()
    {
        if (InteractionState == CardInteractionState.Hidden)
        {
            SetSpriteAlpha(frontRenderer, 0.2f);
            SetSpriteAlpha(backRenderer, 0.2f);
            outlineRenderer.color = Color.grey;
        }
        else
        {
            SetSpriteAlpha(frontRenderer, 1f);
            SetSpriteAlpha(backRenderer, 1f);
            outlineRenderer.color = (status == CardStatus.Opened) ? Color.green : Color.red;
        }
    }

    protected void SetSpriteAlpha(SpriteRenderer sr, float alpha)
    {
        var c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    // Обработка кликов
    public void OnPointerClick(PointerEventData eventData)
    {
        if (InteractionState == CardInteractionState.Hidden)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Используем GetZoomTarget(), чтобы для локационной карты учесть кастомное смещение
            CameraController.Instance.ToggleZoom(GetZoomPosTarget(), GetZoomSizeTarget());
        }
        else if (InteractionState == CardInteractionState.Active && eventData.button == PointerEventData.InputButton.Right)
        {
            FlipCard();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InteractionState != CardInteractionState.Hidden)
        {
            SetSpriteAlpha(outlineRenderer, 1f);
            outlineRenderer.color = (InteractionState == CardInteractionState.Active) ? Color.white : Color.yellow;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InteractionState != CardInteractionState.Hidden)
        {
            UpdateVisibility();
        }
    }
}