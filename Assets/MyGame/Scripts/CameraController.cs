using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CameraState
{
    FullView,   // Показывает всё игровое поле
    FreeMovement,       // Свободное перемещение камеры
    Zoomed      // Приближена к выбранному объекту  
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
    [SerializeField] private float zoomedSize = 5.25f;

    [Header("Параметры свободного перемещения")]
    [SerializeField] private float zoomScrollFactor = 10f;  // Фактор изменения zoom при прокрутке
    [SerializeField] private float minZoom = 5.25f;
    private float maxZoom;
    [SerializeField] private float zoomSpeed = 10f;         // Скорость интерполяции zoom


    private Camera mainCamera;
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

        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        originalCameraSize = maxZoom = mainCamera.orthographicSize;
        originalCameraPosition = mainCamera.transform.position;
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
            panOrigin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            CurrentStateInteraction = CameraStateInteraction.Busy;
        }
        if (Input.GetMouseButton(2) && isPanning)
        {
            Vector3 currentMousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 diff = panOrigin - currentMousePos;
            mainCamera.transform.position += diff;
        }
        if (Input.GetMouseButtonUp(2))
        {
            CurrentStateInteraction = CameraStateInteraction.Free;
            isPanning = false;
        }

        // Изменение zoom с помощью прокрутки мыши
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newSize = Mathf.Clamp(mainCamera.orthographicSize - scroll * zoomScrollFactor, minZoom, maxZoom);
            // Плавное изменение размера камеры
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, newSize, Time.deltaTime * zoomSpeed);
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
        stateHistory.Add(new CameraStateRecord(CurrentState, mainCamera.orthographicSize, mainCamera.transform.position));
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

    public void ToggleZoom(Vector3 targetPosition)
    {
        if (CurrentStateInteraction == CameraStateInteraction.Busy)
            return;
        
        Vector3 newTarget = new Vector3(targetPosition.x, targetPosition.y, -10f);
        
        if (CurrentState != CameraState.Zoomed)
        {
            StartCoroutine(ZoomCameraCoroutine(zoomedSize, newTarget, CameraState.Zoomed));
        }
        else
        {
            if (Vector3.Distance(currentZoomTarget, newTarget).Equals(0)) // Если нажата та же цель — возвращаемся в предыдущий сохранённый режим (Undo)
            {
                UndoLastAction();
            }
            else // Нажата другая карта – плавно перемещаемся к новой цели
            {
                StartCoroutine(ZoomCameraCoroutine(zoomedSize, newTarget, CameraState.Zoomed, false));
            }
        }
    }

    private IEnumerator ZoomCameraCoroutine(float targetSize, Vector3 targetPos, CameraState newState, bool needSave = true)
    {
        if (needSave)
            PushCurrentState();

        CurrentStateInteraction = CameraStateInteraction.Busy;

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
        mainCamera.transform.position = currentZoomTarget = targetPos;
        
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