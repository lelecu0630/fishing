using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// 물고기 그랩 안되는 원인을 자동으로 진단하는 디버그 스크립트
/// 물고기 프리팹에 임시로 붙여서 Console 로그를 확인하세요.
/// 문제 해결 후 제거해도 됩니다.
/// </summary>
public class FishGrabDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log("====== 🔍 물고기 그랩 진단 시작 ======");

        // ── 1. Collider 검사 ──
        Collider[] cols = GetComponentsInChildren<Collider>(true);
        if (cols.Length == 0)
        {
            Debug.LogError("❌ [Collider 없음] Collider가 전혀 없습니다! XR 손이 물고기를 인식 못합니다. → Collider 추가 필요");
        }
        else
        {
            foreach (var col in cols)
            {
                if (col.isTrigger)
                    Debug.LogWarning($"⚠️ [Collider: {col.name}] isTrigger=true → XR Grab은 isTrigger=false인 Collider가 필요합니다!");
                else
                    Debug.Log($"✅ [Collider OK] {col.name} / {col.GetType().Name} / isTrigger=false");
            }
        }

        // ── 2. Rigidbody 검사 ──
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("❌ [Rigidbody 없음] XRGrabInteractable은 반드시 Rigidbody가 필요합니다! → Rigidbody 추가 필요");
        }
        else
        {
            Debug.Log($"✅ [Rigidbody 있음] isKinematic={rb.isKinematic} / detectCollisions={rb.detectCollisions}");
            if (rb.detectCollisions == false)
                Debug.LogError("❌ [detectCollisions=false] 이 설정이 그랩을 막고 있습니다! → true로 바꾸세요");
        }

        // ── 3. XRGrabInteractable 검사 ──
        XRGrabInteractable grab = GetComponent<XRGrabInteractable>();
        if (grab == null)
        {
            Debug.LogError("❌ [XRGrabInteractable 없음] 프리팹에 XRGrabInteractable 컴포넌트가 없습니다! → 추가 필요");
        }
        else
        {
            Debug.Log($"✅ [XRGrabInteractable 있음] enabled={grab.enabled}");
            if (!grab.enabled)
                Debug.LogError("❌ [XRGrabInteractable 비활성화] enabled=false → true로 바꾸세요");

            // InteractionLayerMask 검사
            Debug.Log($"  └ InteractionLayerMask: {grab.interactionLayers.value} (컨트롤러와 같은 레이어인지 확인!)");
        }

        // ── 4. XR Ray / Direct Interactor 존재 검사 ──
        XRDirectInteractor[] directs = Object.FindObjectsByType<XRDirectInteractor>(FindObjectsSortMode.None);
        XRRayInteractor[] rays = Object.FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
        if (directs.Length == 0 && rays.Length == 0)
        {
            Debug.LogError("❌ [Interactor 없음] 씬에 XRDirectInteractor / XRRayInteractor가 없습니다! → XR Origin 설정 확인");
        }
        else
        {
            foreach (var d in directs)
                Debug.Log($"✅ [DirectInteractor] {d.name} / InteractionLayer: {d.interactionLayers.value}");
            foreach (var r in rays)
                Debug.Log($"✅ [RayInteractor] {r.name} / InteractionLayer: {r.interactionLayers.value}");
        }

        // ── 5. Layer 충돌 매트릭스 경고 ──
        int fishLayer = gameObject.layer;
        Debug.Log($"ℹ️ [물고기 레이어] Layer={fishLayer} ({LayerMask.LayerToName(fishLayer)}) → Physics Matrix에서 컨트롤러 레이어와 충돌 허용됐는지 확인!");

        // ── 6. Scale 이상 검사 ──
        if (transform.localScale == Vector3.zero)
            Debug.LogError("❌ [Scale=0] 오브젝트 Scale이 0입니다! 보이지도 않고 잡히지도 않습니다.");
        else if (transform.localScale.magnitude < 0.01f)
            Debug.LogWarning("⚠️ [Scale 너무 작음] Scale이 매우 작습니다. 손이 닿지 않을 수 있습니다.");
        else
            Debug.Log($"✅ [Scale OK] {transform.localScale}");

        Debug.Log("====== 진단 완료. 위의 ❌ 항목을 순서대로 해결하세요 ======");
    }

    // 런타임에서 손이 닿는지 실시간 확인
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[진단] OnTriggerEnter: {other.name} (layer={other.gameObject.layer})");
    }
    void OnCollisionEnter(Collision col)
    {
        Debug.Log($"[진단] OnCollisionEnter: {col.gameObject.name} (layer={col.gameObject.layer})");
    }
}