using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class FishingCasting : MonoBehaviour
{
    [System.Serializable]
    public struct FishData
    {
        public string fishName;
        public GameObject fishPrefab;
        [Range(0f, 100f)]
        public float spawnChance;
        public int minScore;
        public int maxScore;
    }

    [Header("캐스팅 세팅")]
    public GameObject bobberPrefab;
    public Transform castPoint;
    public float castForceMultiplier = 5f;
    public LineRenderer lineRenderer;

    [Header("물고기 확률 및 점수 세팅")]
    public List<FishData> fishList;

    [Header("물고기 스폰 세팅")]
    public Transform fishSpawnPoint;
    public float spawnRandomOffset = 0.3f;

    [Header("찌 매달기 세팅")]
    public float hangOffset = 0.3f;

    private GameObject currentBobber;
    private bool isPreparingCast = false;
    private Vector3 lastPosition;
    private Vector3 currentVelocity;
    private float castLockTimer = 0f;
    private Transform playerTransform;
    private bool isFishBiting = false;
    private GameObject hangingBobber;

    // 튜토리얼용 상태 플래그
    private bool bobberInWater = false;
    private bool fishSpawned = false;

    public bool IsBobberInWater() => bobberInWater;
    public bool IsFishBiting() => isFishBiting;
    public bool IsFishSpawned() => fishSpawned;

    void Start()
    {
        lastPosition = transform.position;
        if (lineRenderer != null) lineRenderer.enabled = false;
        if (Camera.main != null) playerTransform = Camera.main.transform;
    }

    void Update()
    {
        currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        if (currentBobber == null && hangingBobber == null)
            DetectCastingMotion();
        else if (isFishBiting)
            DetectHookingMotion();

        if (currentBobber != null && lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, castPoint.position);
            lineRenderer.SetPosition(1, currentBobber.transform.position);
        }
        else if (hangingBobber != null && lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, castPoint.position);
            lineRenderer.SetPosition(1, hangingBobber.transform.position);
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.cKey.wasPressedThisFrame) CastBobber(5.0f);
            if (Keyboard.current.fKey.wasPressedThisFrame && currentBobber != null) SetBitingState(true);
            if (Keyboard.current.rKey.wasPressedThisFrame && isFishBiting) CatchFish();
        }
    }

    public void SetBitingState(bool isBiting)
    {
        isFishBiting = isBiting;
        if (isBiting) Debug.Log("물고기가 미끼를 물었다! 낚싯대를 위로 채세요!");
    }

    // Bobber.cs에서 물에 닿았을 때 호출
    public void SetBobberInWater(bool value)
    {
        bobberInWater = value;
    }

    void DetectHookingMotion()
    {
        if (currentVelocity.y > 3.0f)
        {
            if (isFishBiting) { Debug.Log("챔질 성공!"); CatchFish(); }
            else { Debug.Log("헛챔질!"); ResetFishingState(); }
        }
    }

    void CatchFish()
    {
        isFishBiting = false;
        fishSpawned = false;
        if (fishList == null || fishList.Count == 0) return;

        float totalWeight = 0f;
        foreach (var fish in fishList) totalWeight += fish.spawnChance;
        float randomValue = Random.Range(0f, totalWeight);
        FishData selectedFish = fishList[0];
        float currentWeightSum = 0f;
        foreach (var fish in fishList)
        {
            currentWeightSum += fish.spawnChance;
            if (randomValue <= currentWeightSum) { selectedFish = fish; break; }
        }
        int finalScore = Random.Range(selectedFish.minScore, selectedFish.maxScore + 1);

        if (currentBobber != null)
        {
            Bobber bobberComp = currentBobber.GetComponent<Bobber>();
            if (bobberComp != null) bobberComp.OnFishCaught();
            Destroy(currentBobber);
            currentBobber = null;
        }

        bobberInWater = false;

        hangingBobber = Instantiate(bobberPrefab, castPoint.position, Quaternion.identity);
        Bobber bobberScript = hangingBobber.GetComponent<Bobber>();
        if (bobberScript != null) bobberScript.enabled = false;

        Rigidbody bobberRb = hangingBobber.GetComponent<Rigidbody>();
        if (bobberRb != null)
        {
            bobberRb.isKinematic = false;
            bobberRb.linearVelocity = Vector3.zero;
            bobberRb.angularVelocity = Vector3.zero;
            bobberRb.isKinematic = true;
        }
        hangingBobber.transform.SetParent(castPoint);
        hangingBobber.transform.localPosition = Vector3.down * hangOffset;
        hangingBobber.transform.localRotation = Quaternion.identity;
        hangingBobber.transform.localScale = Vector3.one;

        SpawnFishNearBucket(selectedFish, finalScore);
        fishSpawned = true;

        StartCoroutine(ResetAfterDelay(2f));
        Debug.Log($"{selectedFish.fishName} 잡았습니다! 양동이 옆에 스폰됐어요!");
    }

    void SpawnFishNearBucket(FishData selectedFish, int finalScore)
    {
        Vector3 spawnPos;
        if (fishSpawnPoint != null)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnRandomOffset, spawnRandomOffset),
                0f,
                Random.Range(-spawnRandomOffset, spawnRandomOffset)
            );
            spawnPos = fishSpawnPoint.position + randomOffset + Vector3.up * 1.5f;
        }
        else
        {
            Vector3 forward = playerTransform != null ? playerTransform.forward : Vector3.forward;
            forward.y = 0f;
            spawnPos = (playerTransform != null ? playerTransform.position : Vector3.zero)
                       + forward * 1.5f + Vector3.up * 1.5f;
            Debug.LogWarning("fishSpawnPoint가 설정되지 않았습니다.");
        }

        GameObject spawnedFish = Instantiate(selectedFish.fishPrefab, spawnPos, Quaternion.identity);

        Rigidbody fishRb = spawnedFish.GetComponent<Rigidbody>();
        if (fishRb == null) fishRb = spawnedFish.AddComponent<Rigidbody>();
        fishRb.isKinematic = false;
        fishRb.useGravity = true;
        fishRb.detectCollisions = true;

        foreach (Collider col in spawnedFish.GetComponentsInChildren<Collider>(true))
        {
            if (col.gameObject.name == "GrabCollider")
                col.isTrigger = false;
            else
                col.isTrigger = true;
        }

        XRGrabInteractable grab = spawnedFish.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = spawnedFish.AddComponent<XRGrabInteractable>();
        grab.enabled = true;
        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.throwOnDetach = true;
        grab.interactionLayers = new InteractionLayerMask { value = unchecked((int)0xFFFFFFFF) };

        Transform grabColTransform = null;
        foreach (Transform t in spawnedFish.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "GrabCollider") { grabColTransform = t; break; }
        }
        if (grabColTransform != null)
        {
            Collider grabCol = grabColTransform.GetComponent<Collider>();
            if (grabCol != null)
            {
                grab.colliders.Clear();
                grab.colliders.Add(grabCol);
            }
        }

        FishScore scoreScript = spawnedFish.GetComponent<FishScore>();
        if (scoreScript == null) scoreScript = spawnedFish.AddComponent<FishScore>();
        scoreScript.score = finalScore;
        scoreScript.fishName = selectedFish.fishName;
    }

    IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (hangingBobber != null) { Destroy(hangingBobber); hangingBobber = null; }
        if (lineRenderer != null) lineRenderer.enabled = false;
        isPreparingCast = false;
        Debug.Log("다시 낚시할 수 있습니다!");
    }

    void DetectCastingMotion()
    {
        if (castLockTimer > 0f) castLockTimer -= Time.deltaTime;
        Vector3 movementDir = currentVelocity.normalized;
        Vector3 playerForward = (playerTransform != null) ? playerTransform.forward : Vector3.forward;
        playerForward.y = 0f;
        playerForward.Normalize();

        float directionMatch = Vector3.Dot(movementDir, playerForward);
        float speed = currentVelocity.magnitude;

        if (directionMatch < -0.3f && speed > 1.5f && !isPreparingCast)
        { isPreparingCast = true; castLockTimer = 0.3f; }

        if (isPreparingCast && castLockTimer <= 0f && directionMatch > 0.5f && speed > 2.5f)
        { CastBobber(speed); isPreparingCast = false; }
    }

    void CastBobber(float forwardSpeed)
    {
        if (hangingBobber != null) { Destroy(hangingBobber); hangingBobber = null; }
        if (currentBobber != null) Destroy(currentBobber);

        GameObject newBobber = Instantiate(bobberPrefab, castPoint.position, castPoint.rotation);
        Rigidbody bobberRb = newBobber.GetComponent<Rigidbody>();
        if (bobberRb != null)
        {
            Vector3 forceDir = (Camera.main != null ? Camera.main.transform.forward : Vector3.forward) + Vector3.up * 0.5f;
            bobberRb.AddForce(forceDir.normalized * 7.0f, ForceMode.Impulse);
        }
        currentBobber = newBobber;
        if (lineRenderer != null) lineRenderer.enabled = true;
    }

    public void ResetFishingState()
    {
        if (currentBobber != null) { Destroy(currentBobber); currentBobber = null; }
        if (hangingBobber != null) { Destroy(hangingBobber); hangingBobber = null; }
        if (lineRenderer != null) lineRenderer.enabled = false;
        isFishBiting = false;
        isPreparingCast = false;
        bobberInWater = false;
        fishSpawned = false;
        Debug.Log("낚시 상태 초기화.");
    }
}

public class FishScore : MonoBehaviour
{
    public string fishName;
    public int score;
}