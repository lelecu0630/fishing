using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class TutorialManager : MonoBehaviour
{
    public enum TutorialStep
    {
        GrabRod,
        WaitForCast,
        WaitForBobber,
        WaitForBite,
        WaitForBucket,
        Complete
    }

    [Header("UI 참조")]
    public GameObject tutorialCanvas;
    public TextMeshProUGUI guideText;
    public TextMeshProUGUI stepText;

    [Header("연동")]
    public FishingCasting fishingCasting;
    public Bucket bucket;
    public XRGrabInteractable rodGrabInteractable;

    [Header("UI 위치 세팅")]
    public float distanceFromPlayer = 1.5f;
    public float heightOffset = 0.1f;
    public float followSpeed = 3f;

    private Transform cameraTransform;
    private TutorialStep currentStep = TutorialStep.GrabRod;
    private bool isTutorialActive = false;
    private int bucketScoreAtStart = 0;

    private readonly string[] guideMessages = new string[]
    {
        "낚싯대를 잡으세요!",
        "낚싯대를 몸쪽으로 당겼다가\n앞으로 던지세요!",
        "찌가 물에 닿았어요!\n물고기가 물기를 기다리세요...",
        "물고기가 미끼를 물었어요!\n낚싯대를 위로 들어올리세요!",
        "물고기를 잡아서\n양동이에 넣으세요!",
        "튜토리얼 완료!\n이제 낚시를 즐겨보세요!"
    };

    void Start()
    {
        cameraTransform = Camera.main.transform;

        if (rodGrabInteractable != null)
            rodGrabInteractable.selectEntered.AddListener(OnRodGrabbed);

        // 튜토리얼 Canvas 숨기기 (GameStartUI에서 StartTutorial 호출 전까지 대기)
        if (tutorialCanvas != null) tutorialCanvas.SetActive(false);
    }

    void Update()
    {
        if (!isTutorialActive) return;

        if (tutorialCanvas != null && tutorialCanvas.activeSelf)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 targetPos = cameraTransform.position
                                + forward * distanceFromPlayer
                                + Vector3.up * heightOffset;

            tutorialCanvas.transform.position = Vector3.Lerp(
                tutorialCanvas.transform.position,
                targetPos,
                Time.deltaTime * followSpeed
            );

            tutorialCanvas.transform.LookAt(cameraTransform.position);
            tutorialCanvas.transform.Rotate(0, 180f, 0);
        }

        switch (currentStep)
        {
            case TutorialStep.WaitForCast:
                if (fishingCasting != null && fishingCasting.IsBobberInWater())
                    GoToStep(TutorialStep.WaitForBobber);
                break;
            case TutorialStep.WaitForBobber:
                if (fishingCasting != null && fishingCasting.IsFishBiting())
                    GoToStep(TutorialStep.WaitForBite);
                break;
            case TutorialStep.WaitForBite:
                if (fishingCasting != null && fishingCasting.IsFishSpawned())
                    GoToStep(TutorialStep.WaitForBucket);
                break;
            case TutorialStep.WaitForBucket:
                if (bucket != null && bucket.totalScore > bucketScoreAtStart)
                    GoToStep(TutorialStep.Complete);
                break;
        }
    }

    // GameStartUI에서 호출
    public void StartTutorial()
    {
        isTutorialActive = true;
        bucketScoreAtStart = bucket != null ? bucket.totalScore : 0;
        currentStep = TutorialStep.GrabRod;

        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(true);
            PlaceInFrontOfCamera();
        }

        UpdateUI();
        Debug.Log("Tutorial Started!");
    }

    void OnRodGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (currentStep != TutorialStep.GrabRod) return;
        if (fishingCasting != null) fishingCasting.enabled = true;
        GoToStep(TutorialStep.WaitForCast);
        Debug.Log("Rod grabbed! Tutorial Step 1.");
    }

    void GoToStep(TutorialStep step)
    {
        currentStep = step;
        UpdateUI();
        if (step == TutorialStep.Complete)
            StartCoroutine(CompleteTutorial());
    }

    void UpdateUI()
    {
        int stepIndex = (int)currentStep;
        if (guideText != null) guideText.text = guideMessages[stepIndex];
        if (stepText != null)
        {
            if (currentStep == TutorialStep.GrabRod) stepText.text = "";
            else if (currentStep == TutorialStep.Complete) stepText.text = "Complete!";
            else stepText.text = $"{stepIndex} / 4";
        }
    }

    IEnumerator CompleteTutorial()
    {
        yield return new WaitForSeconds(3f);
        isTutorialActive = false;
        if (tutorialCanvas != null) tutorialCanvas.SetActive(false);
        Debug.Log("Tutorial Complete!");
    }

    void PlaceInFrontOfCamera()
    {
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();
        tutorialCanvas.transform.position = cameraTransform.position
                                            + forward * distanceFromPlayer
                                            + Vector3.up * heightOffset;
    }
}