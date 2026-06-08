using UnityEngine;

public class Bucket : MonoBehaviour
{
    public int totalScore = 0; // 플레이어의 현재 점수

    private void OnTriggerEnter(Collider other)
    {
        // 1. 들어온 물체가 'FishScore' 스크립트를 가지고 있는지 확인
        FishScore caughtFish = other.GetComponent<FishScore>();

        if (caughtFish != null)
        {
            // 2. 점수 합산
            totalScore += caughtFish.score;
            Debug.Log($" 물고기 획득! [{caughtFish.fishName}] 점수: {caughtFish.score}점 추가! (총 점수: {totalScore}점)");

            // 3. 물고기 제거
            Destroy(other.gameObject);

            // 여기서 효과음을 추가하면 아주 좋습니다!
            // AudioSource.PlayClipAtPoint(splashSound, transform.position);
        }
    }
}