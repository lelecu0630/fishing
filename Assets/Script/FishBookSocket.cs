using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class FishBookSocket : MonoBehaviour
{
    private XRSocketInteractor socket;

    [Header("도감 UI - 물고기 이미지들")]
    // 유니티 인스펙터에서 각 물고기별 '정상 이미지' 오브젝트를 연결하세요.
    public GameObject UI_붕어;
    public GameObject UI_잉어;
    public GameObject UI_참치;

    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
        // 소켓에 물고기가 장착되는 순간 감지
        socket.selectEntered.AddListener(OnFishRegistered);
    }

    void OnFishRegistered(SelectEnterEventArgs args)
    {
        // 1. 소켓에 들어온 물고기 정보 가져오기
        GameObject fishObj = args.interactableObject.transform.gameObject;
        FishScore fishScore = fishObj.GetComponent<FishScore>();

        if (fishScore != null)
        {
            Debug.Log($"📖 도감 등록: {fishScore.fishName}");

            // 2. 물고기 이름에 따라 해당 UI 활성화 (점수 부여 로직은 없음!)
            switch (fishScore.fishName)
            {
                case "붕어":
                    if (UI_붕어 != null) UI_붕어.SetActive(true);
                    break;
                case "황금 잉어":
                    if (UI_잉어 != null) UI_잉어.SetActive(true);
                    break;
                case "참치":
                    if (UI_참치 != null) UI_참치.SetActive(true);
                    break;
            }

            // 3. 물고기를 삭제하여 '등록 후 소멸' 로직 완성
            Destroy(fishObj);
        }
    }
}