using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Скрипт для управления показом RawImage по нажатию на объект (например, QR-код)
/// и скрытием по нажатию клавиши Escape.
/// Прикрепите этот скрипт к объекту с QR-кодом, и укажите ссылку на RawImage в инспекторе.
/// </summary>
public class QRCodeToggle : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject qrRawImage; // RawImage, который нужно показать/скрыть

    void Update()
    {
        // При нажатии Escape скрываем RawImage
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (qrRawImage != null)
            {
                qrRawImage.SetActive(false);
            }
        }
    }

    // Метод вызывается при клике по объекту с этим скриптом
    public void OnPointerClick(PointerEventData eventData)
    {
        if (qrRawImage != null)
        {
            qrRawImage.SetActive(true);
        }
    }
}
