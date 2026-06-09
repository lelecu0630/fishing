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

    [Header("🚀 캐스팅 세팅")]
    public GameObject bobberPrefab;
    public Transform castPoint;
    public float castForceMultiplier = 5f;
    public LineRenderer lineRenderer;

    [Header("🐟 물고기 확률 및 점수 세팅")]
    public List<FishData> fishList;

    [Header("🎣 챔질 후 매달기 세팅")]
    [Tooltip("castPoint 아래로 찌가 매달리는 거리")]
    public float hangOffset = 0.3f;
    [Tooltip("찌 아래 물고기가 매달리는 거리")]
    public float fishBelowBobber = 0.2f;

    private GameObject currentBobber;
    private bool isPreparingCast = false;
    private Vector3 lastPosition;
    private Vector3 currentVelocity;
    private float castLockTimer = 0f;
    private Transform playerTransform;
    private bool isFishBiting = false;

    private GameObject hangingBobber;
    private GameObject hangingFish;

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

        // 물고기가 매달려 있는 동안 위치를 castPoint 기준으로 매 프레임 강제 고정
        // (isKinematic=false여도 중력에 안 떨어지도록)
        if (hangingFish != null)
        {
            hangingFish.transform.position = castPoint.position + Vector3.down * (hangOffset + fishBelowBobber);
        }

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
        if (isBiting) Debug.Log("🐟 물고기가 미끼를 물었다! 낚싯대를 위로 채세요!");
    }

    void DetectHookingMotion()
    {
        if (currentVelocity.y > 3.0f)
        {
            if (isFishBiting) { Debug.Log("💥 챔질 성공!"); CatchFish(); }
            else { Debug.Log("🎣 헛챔질!"); ResetFishingState(); }
        }
    }

    void CatchFish()
    {
        isFishBiting = false;
        if (fishList == null || fishList.Count == 0) return;

        // 확률 로직
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

        if (currentBobber != null) { Destroy(currentBobber); currentBobber = null; }

        // ── 찌를 낚싯대 끝에 매달기 ──
        hangingBobber = Instantiate(bobberPrefab, castPoint.position, Quaternion.identity);
        Bobber bobberScript = hangingBobber.GetComponent<Bobber>();
        if (bobberScript != null) bobberScript.enabled = false;

        Rigidbody bobberRb = hangingBobber.GetComponent<Rigidbody>();
        if (bobberRb != null)
        {
            // ★ isKinematic을 먼저 false로 해서 velocity 초기화 후 다시 true
            bobberRb.isKinematic = false;
            bobberRb.linearVelocity = Vector3.zero;
            bobberRb.angularVelocity = Vector3.zero;
            bobberRb.isKinematic = true;
        }
        hangingBobber.transform.SetParent(castPoint);
        hangingBobber.transform.localPosition = Vector3.down * hangOffset;
        hangingBobber.transform.localRotation = Quaternion.identity;
        hangingBobber.transform.localScale = Vector3.one; // 부모 Scale 영향 차단

        // ── 물고기 스폰 ──
        Vector3 fishPos = castPoint.position + Vector3.down * (hangOffset + fishBelowBobber);
        hangingFish = Instantiate(selectedFish.fishPrefab, fishPos, Quaternion.identity);

        SetupFishForGrab(hangingFish, selectedFish, finalScore);
        Debug.Log($"🎉 [{selectedFish.fishName}] 낚싯대에 매달림! 손으로 잡아보세요!");
    }

    void SetupFishForGrab(GameObject fish, FishData selectedFish, int finalScore)
    {
        // Scale은 프리팹 원본값 그대로 유지

        // ── 2. Rigidbody 세팅 ──
        Rigidbody fishRb = fish.GetComponent<Rigidbody>();
        if (fishRb == null) fishRb = fish.AddComponent<Rigidbody>();

        // isKinematic=false 유지 → RayInteractor로 Grab 가능
        // 위치는 Update()에서 매 프레임 castPoint 기준으로 강제 고정
        fishRb.isKinematic = false;
        fishRb.linearVelocity = Vector3.zero;
        fishRb.angularVelocity = Vector3.zero;
        fishRb.useGravity = false;
        fishRb.detectCollisions = true;

        // ── 3. 기존 Collider는 isTrigger로 변경, GrabCollider만 남김 ──
        // 프리팹에서 이름이 'GrabCollider'인 자식 오브젝트의 Collider만 isTrigger=false 유지
        Transform grabColTransform = fish.transform.Find("GrabCollider");
        foreach (Collider col in fish.GetComponentsInChildren<Collider>(true))
        {
            if (grabColTransform != null && col.transform == grabColTransform)
                col.isTrigger = false; // Grab 전용 큐브는 물리 Collider 유지
            else
                col.isTrigger = true;  // 나머지는 모두 트리거로 전환
        }

        if (grabColTransform == null)
            Debug.LogWarning("⚠️ 'GrabCollider' 이름의 자식 오브젝트를 찾지 못했습니다. 프리팹에서 꼬리 큐브 이름을 'GrabCollider'로 변경해 주세요.");

        // ── 4. 찌와 물고기 Collider 간 충돌 무시 (GrabCollider 포함) ──
        if (hangingBobber != null)
        {
            Collider[] bobberCols = hangingBobber.GetComponentsInChildren<Collider>(true);
            Collider[] fishCols = fish.GetComponentsInChildren<Collider>(true);
            foreach (var bobberCol in bobberCols)
                foreach (var fishCol in fishCols)
                    Physics.IgnoreCollision(bobberCol, fishCol, true);
        }

        // ── 5. XRGrabInteractable 세팅 ──
        XRGrabInteractable grab = fish.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = fish.AddComponent<XRGrabInteractable>();
        grab.enabled = true;
        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.throwOnDetach = true;

        // InteractionLayerMask를 Everything으로 설정
        grab.interactionLayers = new InteractionLayerMask { value = unchecked((int)0xFFFFFFFF) };

        // ── 5-1. GrabCollider를 XRGrabInteractable Colliders 리스트에 등록 ──
        // Colliders 리스트가 비어있으면 XRI가 루트 Collider만 탐색 → GrabCollider 인식 불가
        if (grabColTransform != null)
        {
            Collider grabCol = grabColTransform.GetComponent<Collider>();
            if (grabCol != null)
            {
                grab.colliders.Clear();
                grab.colliders.Add(grabCol);
                Debug.Log("✅ GrabCollider를 XRGrabInteractable에 등록 완료");
            }
        }

        // ── 6. 위치 설정 (부모 설정 없이 월드 좌표로 배치) ──
        // SetParent를 쓰면 Scale 영향 및 Collider 간섭 발생 → Update에서 위치 고정
        fish.transform.SetParent(null);
        fish.transform.position = castPoint.position + Vector3.down * (hangOffset + fishBelowBobber);
        fish.transform.rotation = Quaternion.identity;

        // ── 7. FishScore 부착 ──
        FishScore scoreScript = fish.GetComponent<FishScore>();
        if (scoreScript == null) scoreScript = fish.AddComponent<FishScore>();
        scoreScript.score = finalScore;
        scoreScript.fishName = selectedFish.fishName;

        // ── 8. Grab 이벤트 ──
        grab.selectEntered.AddListener((args) =>
        {
            Debug.Log($"✋ [{selectedFish.fishName}] 잡았습니다!");
            hangingFish = null; // Update 위치 고정 해제
            if (fishRb != null)
            {
                fishRb.useGravity = true; // 잡힌 후 중력 복원
                fishRb.linearVelocity = Vector3.zero;
                fishRb.angularVelocity = Vector3.zero;
            }
            if (lineRenderer != null) lineRenderer.enabled = false;
            if (hangingBobber != null) { Destroy(hangingBobber); hangingBobber = null; }
        });

        grab.selectExited.AddListener((args) =>
        {
            hangingFish = null;
            isPreparingCast = false;
            Debug.Log("🔄 물고기를 놓았습니다.");
        });
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
        if (hangingFish != null) { Destroy(hangingFish); hangingFish = null; }
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

    void ResetFishingState()
    {
        if (currentBobber != null) { Destroy(currentBobber); currentBobber = null; }
        if (hangingBobber != null) { Destroy(hangingBobber); hangingBobber = null; }
        if (hangingFish != null) { Destroy(hangingFish); hangingFish = null; }
        if (lineRenderer != null) lineRenderer.enabled = false;
        isFishBiting = false;
        isPreparingCast = false;
        Debug.Log("🔄 낚시 상태 초기화.");
    }
}

public class FishScore : MonoBehaviour
{
    public string fishName;
    public int score;
}