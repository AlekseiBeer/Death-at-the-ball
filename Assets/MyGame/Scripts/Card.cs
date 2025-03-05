using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("��������� �����")]
    public bool IsHidden = false;
    public bool IsOpen = false;
    protected bool currentIsOpen = false;
    [SerializeField] protected bool SpecialRotation = false;

    [Header("������� �����")]
    [SerializeField] protected Sprite frontSprite;
    [SerializeField] protected Sprite backSprite;

    [Header("��������� �������� ����������")]
    [SerializeField] protected float flipDuration = 0.5f;
    protected bool isFlipping = false;

    // ������� ������� � �������� ������
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

    // ��������� ����� (�������� �� 180� �� ��� Y)
    protected void FlipCard()
    {
        if (!isFlipping)
        {
            StartCoroutine(FlipAnimation());
        }
    }

    // �������� ��� ������� �������� ����������
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
        // ����� ���������� ��������� ���� outline � ����������� �� ��������� �����
        UpdateVisibility();
        isFlipping = false;
    }

    public virtual void UpdateVisibility()
    {
        if (IsHidden)
        {
            // "��������": ������� ����� ��� ������� � �������� ������
            SetSpriteAlpha(frontRenderer, 0.3f);
            SetSpriteAlpha(backRenderer, 0.3f);
            // ��������� Outline, ����� ��� ��������� ����������
            SetSpriteAlpha(outlineRenderer, 0f);
        }
        else
        {
            // � ���������� ��������� � ����� = 1
            SetSpriteAlpha(frontRenderer, 1f);
            SetSpriteAlpha(backRenderer, 1f);
            // Outline: �������, ���� �������, ������� ���� �������
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

    // ��������� ������
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (IsHidden)
            return;

        // ����� ���� => ��� ������ (��������� ������� � �������)
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            CameraController.Instance.ToggleZoom(transform.position);
        }
        // ������� ���� (��������) => ��������� �����
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            FlipCard();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsHidden)
        {
            // ��� ��������� �������� ������ Outline �����
            SetSpriteAlpha(outlineRenderer, 1f);
            outlineRenderer.color = Color.white;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsHidden)
        {
            // ��� ����� ������� ��������������� ���� Outline
            outlineRenderer.color = currentIsOpen ? Color.green : Color.red;
        }
    }

    protected void UpdateColorOutLine() => outlineRenderer.color = currentIsOpen ? Color.green : Color.red;

}