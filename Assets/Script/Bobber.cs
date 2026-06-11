using System.Collections;
using UnityEngine;

public class Bobber : MonoBehaviour
{
    private Rigidbody myRigidbody;
    private bool isBiting = false;
    private bool isInWater = false;

    public float biteDuration = 3f;
    private FishingCasting playerRod;

    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
        playerRod = Object.FindFirstObjectByType<FishingCasting>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water") && !isInWater)
        {
            isInWater = true;
            // 튜토리얼용 플래그 알림
            if (playerRod != null) playerRod.SetBobberInWater(true);
            Debug.Log("물 태그 인식 성공!");
            StartCoroutine(StartFishingLogic());
        }
    }

    IEnumerator StartFishingLogic()
    {
        if (myRigidbody != null)
        {
            myRigidbody.linearVelocity = Vector3.zero;
            myRigidbody.angularVelocity = Vector3.zero;
            myRigidbody.isKinematic = true;
        }

        Vector3 fixedPos = transform.position;
        float randomWaitTime = Random.Range(3f, 7f);
        float timer = 0f;

        while (timer < randomWaitTime)
        {
            transform.position = fixedPos;
            timer += Time.deltaTime;
            yield return null;
        }

        StartCoroutine(BiteRoutine(fixedPos));
    }

    IEnumerator BiteRoutine(Vector3 originalPos)
    {
        isBiting = true;
        if (playerRod != null) playerRod.SetBitingState(true);

        Vector3 dipPos = originalPos + Vector3.down * 0.15f;

        for (int i = 0; i < 3; i++)
        {
            float actionTimer = 0f;
            while (actionTimer < 0.15f)
            {
                transform.position = Vector3.Lerp(originalPos, dipPos, actionTimer / 0.15f);
                actionTimer += Time.deltaTime;
                yield return null;
            }
            actionTimer = 0f;
            while (actionTimer < 0.15f)
            {
                transform.position = Vector3.Lerp(dipPos, originalPos, actionTimer / 0.15f);
                actionTimer += Time.deltaTime;
                yield return null;
            }
        }

        float waitTimer = 0f;
        while (waitTimer < biteDuration)
        {
            if (!isBiting) yield break;
            transform.position = originalPos;
            waitTimer += Time.deltaTime;
            yield return null;
        }

        // 시간 초과 → 물고기 도망
        Debug.Log("물고기가 도망갔습니다! 다시 캐스팅하세요.");
        isBiting = false;
        if (playerRod != null)
        {
            playerRod.SetBitingState(false);
            playerRod.ResetFishingState();
        }
    }

    public void OnFishCaught()
    {
        isBiting = false;
    }
}