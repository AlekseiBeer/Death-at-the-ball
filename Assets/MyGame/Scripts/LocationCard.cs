using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationCard : Card
{
    // Список карт, относящихся к данной локации
    public List<Card> RelatedCards = new List<Card>();

    protected override void Awake()
    {
        base.Awake();
        SetActiveRelatedCards(IsOpen);
    }

    public void SetActiveRelatedCards(bool value)
    {
        bool hide = !IsOpen;
        foreach (Card card in RelatedCards)
        {
            card.IsHidden = hide;
            //card.gameObject.SetActive(value);
        }
    }

    protected override IEnumerator FlipAnimation()
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
        SetActiveRelatedCards(IsOpen);
        UpdateColorOutLine();
        isFlipping = false;
    }
}
