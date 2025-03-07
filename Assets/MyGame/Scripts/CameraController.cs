using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public enum CameraState
{
    FullView,   // ���������� �� ������� ����
    FreeMovement,       // ��������� ����������� ������
    Zoomed,      // ���������� � ���������� �������
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

    [Header("��������� ���� ������")]
    [SerializeField] private float zoomDuration = 0.25f;
    [SerializeField] private const float zoomedSize = 5.25f;

    [Header("��������� ���������� �����������")]
    [SerializeField] private float zoomScrollFactor = 10f;  // ������ ��������� zoom ��� ���������
    [SerializeField] private float minZoom = 5.25f;
    private float maxZoom;
    [SerializeField] private float zoomSpeed = 10f;         // �������� ������������ zoom

    private CinemachineVirtualCamera virtualCamera;
    private float originalCameraSize;
    private Vector3 originalCameraPosition;

    private Vector3 currentZoomTarget;

    // ��������� ��� ���������� ����������� (���������������)
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
            Debug.LogError("CinemachineVirtualCamera �� ������ �� ������� " + gameObject.name);
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

        // ������� F ��������� ������ � ����� FullView (���������� �������� ���������)
        if (Input.GetKeyDown(KeyCode.F))
        {
            PushCurrentState();
            SetFullView();
        }

        // Undo �� Ctrl+Z
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
        // ���������������: ������� ������� ������ ���� � �������� ����
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

        // ��������� zoom � ������� ��������� ����
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float currentSize = virtualCamera.m_Lens.OrthographicSize;
            float multiplier = Mathf.InverseLerp(minZoom, maxZoom, currentSize);
            if (scroll < 0) multiplier = Mathf.Max(0.05f ,multiplier);
            // ��������� ����� ������ ������
            float newSize = Mathf.Clamp(virtualCamera.m_Lens.OrthographicSize - (scroll > 0 ? 1: -1) * zoomScrollFactor * multiplier, minZoom, maxZoom);
            virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(virtualCamera.m_Lens.OrthographicSize, newSize, Time.deltaTime * zoomSpeed);

            if (scroll > 0)
            {
                // �������� ������� ������� ���� �� �������� ������
                Vector3 mainCamPos = Camera.main.transform.position;
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = virtualCamera.transform.position.z; // ��������� z ����������� ������

                // ������ ���������� ����������� ������ �� ������� �������� ������ � ������� ����
                Vector3 newCamPos = Vector3.Lerp(mainCamPos, mouseWorldPos, Time.deltaTime * zoomSpeed);
                virtualCamera.transform.position = newCamPos;
            }
        }
    }

    private void PushCurrentState()
    {
        // ���� ��������� �����, ������� ����� ������ �������
        if (stateHistory.Count >= MaxHistory)
        {
            stateHistory.RemoveAt(0);
        }
        // ��������� ����� ��������� � ����� ������ (����� �������� ��� ����)
        stateHistory.Add(new CameraStateRecord(CurrentState, virtualCamera.m_Lens.OrthographicSize, virtualCamera.transform.position));
    }

    public void UndoLastAction()
    {
        if (stateHistory.Count > 0 && CurrentStateInteraction == CameraStateInteraction.Free)
        {
            // ��������� ��������� ���������� ���������
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
            if (Vector3.Distance(currentZoomTarget, newTarget).Equals(0)) // ���� ������ �� �� ���� � ������������ � ���������� ���������� ����� (Undo)
            {
                UndoLastAction();
            }
            else // ������ ������ ����� � ������ ������������ � ����� ����
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
        // ���������� ������ ���������� �����������, ���� �����������.
        CurrentState = CameraState.FreeMovement;
    }
}