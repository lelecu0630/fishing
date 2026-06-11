using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameStartUI : MonoBehaviour
{
    [Header("UI 참조")]
    public GameObject startCanvas;
    public Button startButton;

    [Header("연동")]
    public FishingCasting fishingCasting;
    public TutorialManager tutorialManager; // 추가

    [Header("UI 위치 세팅")]
    public float distanceFromPlayer = 1.5f;
    public float heightOffset = 0.0f;
    public float followSpeed = 3f;

    private Transform cameraTransform;
    private bool isShowing = true;

    void Start()
    {
        cameraTransform = Camera.main.transform;

        // 시작 전 낚시 비활성화
        if (fishingCasting != null) fishingCasting.enabled = false;

        // 튜토리얼 매니저도 비활성화 (버튼 누를 때까지 대기)
        //if (tutorialManager != null) tutorialManager.enabled = false;

        if (startButton != null) startButton.onClick.AddListener(OnStartGame);

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
        isShowing = false;
        if (startCanvas != null) startCanvas.SetActive(false);

        // 튜토리얼 시작
        if (tutorialManager != null)
        {
            tutorialManager.enabled = true;
            tutorialManager.StartTutorial();
        }

        Debug.Log("Game Started!");
    }
}