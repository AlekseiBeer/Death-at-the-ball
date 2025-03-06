using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("��������� �������� ����")]
    [SerializeField] private int totalOpenings = 24; // ����� ���������� ��������
    private int remainingOpenings;

    [Header("UI")]
    [SerializeField] private TMP_Text openingsText; // ����� ��� ����������� ���������� ��������

    [Header("����������� ����� (������������, ����� �������� ������ ���)")]
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
    /// ����������, ����� �� ��� ��������� �����.
    /// </summary>
    public bool CanOpenCard()
    {
        return remainingOpenings > 0;
    }

    /// <summary>
    /// ���������� ������, ����� ��� ���� ������� � ���� ��� �������� ����.
    /// </summary>
    public void OnCardOpened(Card card)
    {
        if (card.costsPoint)
        {
            if (remainingOpenings > 0)
            {
                remainingOpenings--;
                UpdateUI();
                // ���� ����� ������ ���, ���������� ����������� �����
                if (remainingOpenings == 0)
                {
                    ActivateSpecialCards();
                }
            }
        }
    }

    /// <summary>
    /// ����������, ����� ����� ����������� � ���������� ����.
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
    /// ���������� ����������� �����, ������� �� ����� ���� ������.
    /// </summary>
    private void ActivateSpecialCards()
    {
        foreach (Card card in CardsToActivate)
        {
            // ������: ������ ����� �������� � ��������� � ���������
            card.InteractionState = CardInteractionState.Active;
            card.UpdateVisibility();
        }
    }

    public int GetRemainingOpenings()
    {
        return remainingOpenings;
    }
}
