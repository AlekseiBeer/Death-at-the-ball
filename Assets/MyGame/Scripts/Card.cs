using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("��������� �����")]
    [SerializeField] private bool isHidden = false;
    [SerializeField] private bool isOpen = false;

    

    [Header("������� �����")]
    public Sprite frontSprite;
    public Sprite backSprite;

    [Header("��������� �������� ����������")]
    [SerializeField] private float flipDuration = 0.5f;
    private bool isFlipping = false;

    [Header("��������� ���� ������")]
    [SerializeField] private float zoomDuration = 0.25f;
    [SerializeField] private float zoomedSize = 10f;      
    private bool isZoomed = false;
    private bool isZooming = false;

    private Camera mainCamera;
    private float originalCameraSize;
    private Vector3 originalCameraPosition;

    // ������� ������� � �������� ������
    private SpriteRenderer frontRenderer;
    private SpriteRenderer backRenderer;

    void Awake()
    {
        frontRenderer = transform.Find("Front").GetComponent<SpriteRenderer>();
        backRenderer = transform.Find("Back").GetComponent<SpriteRenderer>();

        frontRenderer.sprite = frontSprite;
        backRenderer.sprite = backSprite;

        mainCamera = Camera.main;
        originalCameraSize = mainCamera.orthographicSize;
        originalCameraPosition = mainCamera.transform.position;

        if (!isOpen)
        {
            FlipCard();
        }
    }

    // ��������� ����� (�������� �� 180� �� ��� Y)
    private void FlipCard()
    {
        if (!isFlipping)
        {
            StartCoroutine(FlipAnimation());
        }
    }

    // �������� ��� ������� �������� ����������
    private IEnumerator FlipAnimation()
    {
        isFlipping = true;

        float elapsedTime = 0f;
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0, 180f, 0);

        while (elapsedTime < flipDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / flipDuration);
            transform.rotation = Quaternion.Lerp(startRotation, endRotation, t);
            yield return null;
        }

        transform.rotation = endRotation;
        isOpen = !isOpen;
        isFlipping = false;
    }

    private IEnumerator ZoomCameraCoroutine(float targetSize, Vector3 targetPos)
    {
        isZooming = true;

        float startSize = mainCamera.orthographicSize;
        Vector3 startPos = mainCamera.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < zoomDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / zoomDuration);

            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        mainCamera.orthographicSize = targetSize;
        mainCamera.transform.position = targetPos;

        isZoomed = !isZoomed;
        isZooming = false;
    }

    // ������ ����/�������� ������
    private void ToggleZoom()
    {
        if (mainCamera == null || isZooming) return;

        if (!isZoomed)
        {
            // �������� ���������
            float targetSize = zoomedSize;
            Vector3 targetPos = new Vector3(transform.position.x, transform.position.y, -10f);

            StartCoroutine(ZoomCameraCoroutine(targetSize, targetPos));
        }
        else
        {
            // ���������� � �������� ���������
            float targetSize = originalCameraSize;
            Vector3 targetPos = originalCameraPosition;

            StartCoroutine(ZoomCameraCoroutine(targetSize, targetPos));
        }
    }

    // ��������� ������
    public void OnPointerClick(PointerEventData eventData)
    {
        // ����� ���� => ��� ������ (��������� ������� � �������)
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            ToggleZoom();
        }
        // ������� ���� (��������) => ��������� �����
        else if (eventData.button == PointerEventData.InputButton.Middle)
        {
            FlipCard();
        }
    }

    // ��������� ��� ��������� �������
    public void OnPointerEnter(PointerEventData eventData)
    {
        // ������: ����� �������� ����, �������� Outline � �.�.
        // frontRenderer.color = Color.yellow;
        // backRenderer.color = Color.yellow;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ������� ���������
        // frontRenderer.color = Color.white;
        // backRenderer.color = Color.white;
    }
}