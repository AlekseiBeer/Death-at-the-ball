using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public enum CameraState
{
    FullView,   // Показывает всё игровое поле
    FreeMovement,       // Свободное перемещение камеры
    Zoomed,      // Приближена к выбранному объекту
    ZoomedGroup
}

public enum CameraStateInteraction
{
    Free = 0,
    Busy
}

public struct CameraStateRecord
{
    public CameraState state;
    public float orthographicSize;
    public Vector3 position;

    public CameraStateRecord(CameraState state, float size, Vector3 pos)
    {
        this.state = state;
        this.orthographicSize = size;
        this.position = pos;
    }
}

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [Header("Параметры зума камеры")]
    [SerializeField] private float zoomDuration = 0.25f;
    [SerializeField] private const float zoomedSize = 5.25f;

    [Header("Параметры свободного перемещения")]
    [SerializeField] private float zoomScrollFactor = 10f;  // Фактор изменения zoom при прокрутке
    [SerializeField] private float minZoom = 5.25f;
    private float maxZoom;
    [SerializeField] private float zoomSpeed = 10f;         // Скорость интерполяции zoom

    private CinemachineVirtualCamera virtualCamera;
    private float originalCameraSize;
    private Vector3 originalCameraPosition;

    private Vector3 currentZoomTarget;

    // Параметры для свободного перемещения (панорамирования)
    private bool isPanning = false;
    private Vector3 panOrigin;

    [HideInInspector] public CameraState CurrentState { get; private set; } = CameraState.FullView;
    [HideInInspector] public CameraStateInteraction CurrentStateInteraction { get; private set; } = CameraStateInteraction.Free;

    [SerializeField] private const int MaxHistory = 10;
    private List<CameraStateRecord> stateHistory = new List<CameraStateRecord>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        if (virtualCamera == null)
        {
            Debug.LogError("CinemachineVirtualCamera не найден на объекте " + gameObject.name);
            return;
        }
        originalCameraSize = maxZoom = virtualCamera.m_Lens.OrthographicSize;
        originalCameraPosition = virtualCamera.transform.position;
        SetFullView();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(2) || Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f)
            SetFreeView();

        // Клавиша F переводит камеру в режим FullView (сохранение текущего состояния)
        if (Input.GetKeyDown(KeyCode.F))
        {
            PushCurrentState();
            SetFullView();
        }

        // Undo по Ctrl+Z
        if (Input.GetKeyDown(KeyCode.Z))
        {
            UndoLastAction();
        }

        if (CurrentState == CameraState.FreeMovement)
        {
            UpdateFreeMovement();
        }
    }

    void UpdateFreeMovement()
    {
        // Панорамирование: зажмите среднюю кнопку мыши и двигайте мышь
        if (Input.GetMouseButtonDown(2))
        {
            isPanning = true;
            PushCurrentState();
            panOrigin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            CurrentStateInteraction = CameraStateInteraction.Busy;
        }
        if (Input.GetMouseButton(2) && isPanning)
        {
            Vector3 currentMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 diff = panOrigin - currentMousePos;
            virtualCamera.transform.position += diff;
        }
        if (Input.GetMouseButtonUp(2))
        {
            CurrentStateInteraction = CameraStateInteraction.Free;
            virtualCamera.transform.position = Camera.main.transform.position;
            isPanning = false;
        }

        // Изменение zoom с помощью прокрутки мыши
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float currentSize = virtualCamera.m_Lens.OrthographicSize;
            float multiplier = Mathf.InverseLerp(minZoom, maxZoom, currentSize);
            if (scroll < 0) multiplier = Mathf.Max(0.05f ,multiplier);
            // Вычисляем новый размер камеры
            float newSize = Mathf.Clamp(virtualCamera.m_Lens.OrthographicSize - (scroll > 0 ? 1: -1) * zoomScrollFactor * multiplier, minZoom, maxZoom);
            virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(virtualCamera.m_Lens.OrthographicSize, newSize, Time.deltaTime * zoomSpeed);

            if (scroll > 0)
            {
                // Получаем мировую позицию мыши из основной камеры
                Vector3 mainCamPos = Camera.main.transform.position;
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = virtualCamera.transform.position.z; // сохраняем z виртуальной камеры

                // Плавно перемещаем виртуальную камеру от позиции основной камеры к позиции мыши
                Vector3 newCamPos = Vector3.Lerp(mainCamPos, mouseWorldPos, Time.deltaTime * zoomSpeed);
                virtualCamera.transform.position = newCamPos;
            }
        }
    }

    private void PushCurrentState()
    {
        // Если достигнут лимит, удаляем самый старый элемент
        if (stateHistory.Count >= MaxHistory)
        {
            stateHistory.RemoveAt(0);
        }
        // Добавляем новое состояние в конец списка (будет работать как стек)
        stateHistory.Add(new CameraStateRecord(CurrentState, virtualCamera.m_Lens.OrthographicSize, virtualCamera.transform.position));
    }

    public void UndoLastAction()
    {
        if (stateHistory.Count > 0 && CurrentStateInteraction == CameraStateInteraction.Free)
        {
            // Извлекаем последнее сохранённое состояние
            CameraStateRecord previousState = stateHistory[stateHistory.Count - 1];
            stateHistory.RemoveAt(stateHistory.Count - 1);
            StartCoroutine(ZoomCameraCoroutine(previousState.orthographicSize, previousState.position, previousState.state, false));
        }
    }

    public void ToggleZoom(Vector3 targetPosition, float targetSize = zoomedSize)
    {
        if (CurrentStateInteraction == CameraStateInteraction.Busy)
            return;

        Vector3 newTarget = new Vector3(targetPosition.x, targetPosition.y, -10f);

        if (CurrentState != CameraState.Zoomed && CurrentState != CameraState.ZoomedGroup)
        {
            if (targetSize == zoomedSize)
            {
                StartCoroutine(ZoomCameraCoroutine(targetSize, newTarget, CameraState.Zoomed));
            }
            else
            {
                StartCoroutine(ZoomCameraCoroutine(targetSize, newTarget, CameraState.ZoomedGroup));
            }
        }
        else
        {
            if (Vector3.Distance(currentZoomTarget, newTarget).Equals(0)) // Если нажата та же цель — возвращаемся в предыдущий сохранённый режим (Undo)
            {
                UndoLastAction();
            }
            else // Нажата другая карта – плавно перемещаемся к новой цели
            {
                bool needSave = CurrentState == CameraState.ZoomedGroup;
                StartCoroutine(ZoomCameraCoroutine(targetSize, newTarget, CameraState.Zoomed, needSave));
            }
        }
    }

    private IEnumerator ZoomCameraCoroutine(float targetSize, Vector3 targetPos, CameraState newState, bool needSave = true)
    {
        if (needSave)
            PushCurrentState();

        CurrentStateInteraction = CameraStateInteraction.Busy;

        float startSize = virtualCamera.m_Lens.OrthographicSize;
        Vector3 startPos = virtualCamera.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < zoomDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / zoomDuration);

            virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(startSize, targetSize, t);
            virtualCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        virtualCamera.m_Lens.OrthographicSize = targetSize;
        virtualCamera.transform.position = currentZoomTarget = targetPos;

        CurrentState = newState;
        CurrentStateInteraction = CameraStateInteraction.Free;
    }


    public void SetFullView()
    {
        if (CurrentStateInteraction == CameraStateInteraction.Busy || CurrentState == CameraState.FullView)
            return;
        PushCurrentState();
        StartCoroutine(ZoomCameraCoroutine(originalCameraSize, originalCameraPosition, CameraState.FullView));
    }

    public void SetFreeView()
    {
        if (CurrentStateInteraction == CameraStateInteraction.Busy || CurrentState == CameraState.FreeMovement)
            return;
        // Реализуйте логику свободного перемещения, если понадобится.
        CurrentState = CameraState.FreeMovement;
    }
}