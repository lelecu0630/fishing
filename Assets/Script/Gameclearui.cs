using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 4000점 이상 달성 시 플레이어 시점을 따라오는 클리어 UI를 표시합니다.
/// XR Origin (Camera Offset 또는 Main Camera)에 붙여주세요.
/// </summary>
public class GameClearUI : MonoBehaviour
{
    [Header("UI 참조")]
    public GameObject clearCanvas;          // 클리어 UI 캔버스
    public TextMeshProUGUI clearTitleText;  // "🎉 CLEAR!" 텍스트
    public TextMeshProUGUI scoreText;       // 최종 점수 텍스트
    public Button restartButton;            // 다시 시작 버튼
    public Button continueButton;           // 계속하기 버튼

    [Header("UI 위치 세팅")]
    [Tooltip("플레이어 앞 거리")]
    public float distanceFromPlayer = 1.5f;
    [Tooltip("플레이어 눈높이 오프셋")]
    public float heightOffset = 0.0f;
    [Tooltip("UI가 시점을 따라오는 속도")]
    public float followSpeed = 3f;

    [Header("게임 세팅")]
    public int clearScore = 4000;
    public Bucket bucket;                   // Bucket 스크립트 참조

    private Transform cameraTransform;
    private bool isShowing = false;
    private bool alreadyCleared = false;

    void Start()
    {
        cameraTransform = Camera.main.transform;

        // 시작 시 UI 숨기기
        if (clearCanvas != null) clearCanvas.SetActive(false);

        // 버튼 이벤트 연결
        if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
    }

    void Update()
    {
        // 점수 체크
        if (!alreadyCleared && bucket != null && bucket.totalScore >= clearScore)
        {
            ShowClearUI();
        }

        // UI가 표시 중일 때 시점 따라오기
        if (isShowing && clearCanvas != null)
        {
            // 목표 위치: 카메라 앞 distanceFromPlayer 거리
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 targetPos = cameraTransform.position
                                + forward * distanceFromPlayer
                                + Vector3.up * heightOffset;

            // 부드럽게 따라오기
            clearCanvas.transform.position = Vector3.Lerp(
                clearCanvas.transform.position,
                targetPos,
                Time.deltaTime * followSpeed
            );

            // UI가 항상 플레이어를 향하도록 회전
            clearCanvas.transform.LookAt(cameraTransform.position);
            clearCanvas.transform.Rotate(0, 180f, 0); // 뒤집힘 방지
        }
    }

    void ShowClearUI()
    {
        alreadyCleared = true;
        isShowing = true;

        if (clearCanvas != null) clearCanvas.SetActive(true);

        // 처음 위치를 카메라 앞으로 설정
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();
        clearCanvas.transform.position = cameraTransform.position
                                         + forward * distanceFromPlayer
                                         + Vector3.up * heightOffset;

        if (clearTitleText != null) clearTitleText.text = "CLEAR!";
        if (scoreText != null) scoreText.text = $"Final Score: {bucket.totalScore}";

        Debug.Log($"Game Clear! Score: {bucket.totalScore}");
    }

    // 다시 시작: 점수 초기화 + UI 닫기
    void OnRestart()
    {
        if (bucket != null) bucket.totalScore = 0;
        alreadyCleared = false;
        isShowing = false;
        if (clearCanvas != null) clearCanvas.SetActive(false);
        if (bucket != null) bucket.UpdateScoreUIPublic();
        Debug.Log("Restart! Score reset.");
    }

    // 계속하기: 점수 유지 + UI 닫기
    void OnContinue()
    {
        alreadyCleared = false;
        isShowing = false;
        if (clearCanvas != null) clearCanvas.SetActive(false);
        Debug.Log($"Continue! Current score: {bucket.totalScore}");
    }
}