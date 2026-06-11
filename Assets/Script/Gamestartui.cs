using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GameStartUI : MonoBehaviour
{
    [Header("UI ����")]
    public GameObject startCanvas;
    public Button startButton;

    [Header("����")]
    public FishingCasting fishingCasting;
    public TutorialManager tutorialManager;

    [Header("UI ��ġ ����")]
    public float distanceFromPlayer = 1.5f;
    public float heightOffset = 0.0f;
    public float followSpeed = 3f;

    private Transform cameraTransform;
    private bool isShowing = true;
    private bool gameStarted = false;

    void Start()
    {
        cameraTransform = Camera.main.transform;

        if (fishingCasting != null) fishingCasting.enabled = false;

        if (startCanvas != null)
        {
            // TrackedDeviceGraphicRaycaster: VR XR ������Ʈ�ÿ��μ� Canvas UI ����ۿ�
            if (startCanvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                startCanvas.AddComponent<TrackedDeviceGraphicRaycaster>();
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartGame);

            // XRSimpleInteractable: ���˴븦 ������ VR Ʈ��Ÿ ���Ͱ�(UI ��Ʈ�ö¹��۰���) ����
            if (startButton.gameObject.GetComponent<Collider>() == null)
            {
                var col = startButton.gameObject.AddComponent<BoxCollider>();
                var rect = startButton.GetComponent<RectTransform>();
                col.size = new Vector3(rect.rect.width, rect.rect.height, 10f);
                col.isTrigger = true;
            }

            if (startButton.gameObject.GetComponent<XRSimpleInteractable>() == null)
            {
                var xrInteractable = startButton.gameObject.AddComponent<XRSimpleInteractable>();
                xrInteractable.selectEntered.AddListener(_ => OnStartGame());
            }
        }

        if (startCanvas != null)
        {
            startCanvas.SetActive(true);
            PlaceInFrontOfCamera();
        }
    }

    void Update()
    {
        if (!isShowing) return;

        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 targetPos = cameraTransform.position
                            + forward * distanceFromPlayer
                            + Vector3.up * heightOffset;

        startCanvas.transform.position = Vector3.Lerp(
            startCanvas.transform.position,
            targetPos,
            Time.deltaTime * followSpeed
        );

        startCanvas.transform.LookAt(cameraTransform.position);
        startCanvas.transform.Rotate(0, 180f, 0);
    }

    void PlaceInFrontOfCamera()
    {
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        startCanvas.transform.position = cameraTransform.position
                                         + forward * distanceFromPlayer
                                         + Vector3.up * heightOffset;
    }

    void OnStartGame()
    {
        if (gameStarted) return;
        gameStarted = true;
        isShowing = false;

        if (startCanvas != null) startCanvas.SetActive(false);

        if (tutorialManager != null)
        {
            tutorialManager.enabled = true;
            tutorialManager.StartTutorial();
        }

        Debug.Log("Game Started!");
    }
}
