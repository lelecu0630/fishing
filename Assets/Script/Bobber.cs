using System.Collections;
using UnityEngine;

public class Bobber : MonoBehaviour
{
    // [변수 선언] 컴퓨터가 이름을 알아먹을 수 있도록 확실하게 정의합니다.
    private Rigidbody myRigidbody;
    private bool isBiting = false;
    private bool isInWater = false;

    public float biteDuration = 1.5f;
    private FishingCasting playerRod;

    void Start()
    {
        // 변수 방에 컴포넌트를 정확히 채워줍니다.
        myRigidbody = GetComponent<Rigidbody>();

        // 최신 규격으로 낚싯대 스크립트를 찾아옵니다.
        playerRod = Object.FindFirstObjectByType<FishingCasting>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 물에 닿았을 때 검사
        if (other.CompareTag("Water") && !isInWater)
        {
            isInWater = true;
            Debug.Log("★ 물 태그 인식 성공! 강제 고정 루틴을 시작합니다. ★");
            StartCoroutine(StartFishingLogic());
        }
    }

    IEnumerator StartFishingLogic()
    {
        // [수정된 부분] 최신 유니티 규격인 linearVelocity를 사용하고 중력을 완벽히 끕니다!
        if (myRigidbody != null)
        {
            myRigidbody.linearVelocity = Vector3.zero;
            myRigidbody.angularVelocity = Vector3.zero;
            myRigidbody.isKinematic = true; // 중력을 꺼서 절대 가라앉지 않게 만듭니다.
        }

        // 물에 닿은 '현재 위치'를 정확하게 박제합니다.
        Vector3 fixedPos = transform.position;
        float randomWaitTime = Random.Range(3f, 7f);
        float timer = 0f;

        while (timer < randomWaitTime)
        {
            transform.position = fixedPos; // 기다리는 동안 물 위에 둥둥 떠있게 강제 고정
            timer += Time.deltaTime;
            yield return null;
        }

        // 입질 시작할 때 이 고정된 물 위 위치(fixedPos)를 넘겨줍니다.
        StartCoroutine(BiteRoutine(fixedPos));
    }

    IEnumerator BiteRoutine(Vector3 originalPos)
    {
        isBiting = true;
        if (playerRod != null) playerRod.SetBitingState(true);

        // 아래로 들어갈 깊이
        Vector3 dipPos = originalPos + Vector3.down * 0.15f;

        for (int i = 0; i < 3; i++)
        {
            float actionTimer = 0f;
            // 1. 아래로 쑥 가라앉기
            while (actionTimer < 0.15f)
            {
                transform.position = Vector3.Lerp(originalPos, dipPos, actionTimer / 0.15f);
                actionTimer += Time.deltaTime;
                yield return null;
            }

            actionTimer = 0f;
            // 2. 다시 물 위 원래 자리(originalPos)로 완전히 올라오기!
            while (actionTimer < 0.15f)
            {
                transform.position = Vector3.Lerp(dipPos, originalPos, actionTimer / 0.15f);
                actionTimer += Time.deltaTime;
                yield return null;
            }
        }

        // 3. 춤추는 입질 3번이 끝난 후에도 미끼를 물고 있는 동안(biteDuration) 물 위 원래 자리에 가만히 유지시킵니다.
        float restTimer = 0f;
        while (restTimer < biteDuration)
        {
            transform.position = originalPos; // 완전히 물 위에 고정!
            restTimer += Time.deltaTime;
            yield return null;
        }

        isBiting = false;
        if (playerRod != null) playerRod.SetBitingState(false);

        // 안 낚여서 입질이 끝나도 계속 물 위에 둥둥 떠 있도록 유지합니다.
        transform.position = originalPos;
    }
}