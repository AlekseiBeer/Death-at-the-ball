using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Настройки открытий карт")]
    [SerializeField] private int totalOpenings = 24; // общее количество открытий
    private int remainingOpenings;

    [Header("UI")]
    [SerializeField] private TMP_Text openingsText; // текст для отображения оставшихся открытий

    [Header("Специальные карты (активируются, когда открытий больше нет)")]
    public List<Card> CardsToActivate = new List<Card>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        remainingOpenings = totalOpenings;
        UpdateUI();
    }

    /// <summary>
    /// Возвращает, можно ли ещё открывать карты.
    /// </summary>
    public bool CanOpenCard()
    {
        return remainingOpenings > 0;
    }

    /// <summary>
    /// Вызывается картой, когда она была открыта и если она отнимает очко.
    /// </summary>
    public void OnCardOpened(Card card)
    {
        if (card.costsPoint)
        {
            if (remainingOpenings > 0)
            {
                remainingOpenings--;
                UpdateUI();
                // Если очков больше нет, активируем специальные карты
                if (remainingOpenings == 0)
                {
                    ActivateSpecialCards();
                }
            }
        }
    }

    /// <summary>
    /// Вызывается, когда карта закрывается и возвращает очко.
    /// </summary>
    public void OnCardClosed(Card card)
    {
        if (card.costsPoint)
        {
            if (remainingOpenings < totalOpenings)
            {
                remainingOpenings++;
                UpdateUI();
            }
        }
    }

    private void UpdateUI()
    {
        if (openingsText != null)
            openingsText.text = $"{remainingOpenings}";
    }

    /// <summary>
    /// Активирует специальные карты, которые до этого были скрыты.
    /// </summary>
    private void ActivateSpecialCards()
    {
        foreach (Card card in CardsToActivate)
        {
            // Пример: делаем карту активной и обновляем её видимость
            card.InteractionState = CardInteractionState.Active;
            card.UpdateVisibility();
        }
    }

    public int GetRemainingOpenings()
    {
        return remainingOpenings;
    }
}
