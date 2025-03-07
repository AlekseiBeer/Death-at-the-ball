using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ������ ��� ���������� ������� RawImage �� ������� �� ������ (��������, QR-���)
/// � �������� �� ������� ������� Escape.
/// ���������� ���� ������ � ������� � QR-�����, � ������� ������ �� RawImage � ����������.
/// </summary>
public class QRCodeToggle : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject qrRawImage; // RawImage, ������� ����� ��������/������

    void Update()
    {
        // ��� ������� Escape �������� RawImage
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (qrRawImage != null)
            {
                qrRawImage.SetActive(false);
            }
        }
    }

    // ����� ���������� ��� ����� �� ������� � ���� ��������
    public void OnPointerClick(PointerEventData eventData)
    {
        if (qrRawImage != null)
        {
            qrRawImage.SetActive(true);
        }
    }
}
