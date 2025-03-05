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
    [SerializeField] private CardStatus status = CardStatus.Closed;

    [SerializeField] private CardInteractionState _interactionState = CardInteractionState.Active;
    public CardInteractionState InteractionState
    {
        get => _interactionState;
        set
        {
            _interactionState = value;
            UpdateVisibility();
        }
    }
    [SerializeField] protected bool SpecialRotation = false;

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

        UpdateRelatedCardsHide();
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
                card.FlipCard();
            }
            else
            {
                card.InteractionState = CardInteractionState.Hidden;
                card.FlipCard();
            }
        }
    }
    

    protected void FlipCard()
    {
        if (status == CardStatus.Opening || status == CardStatus.Closing)
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
            CameraController.Instance.ToggleZoom(transform.position);
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
            outlineRenderer.color = Color.white;
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