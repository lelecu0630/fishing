using UnityEngine;
using TMPro;

public class Bucket : MonoBehaviour
{
    public int totalScore = 0;

    [Header("점수 UI")]
    public TextMeshProUGUI scoreText;

    private void OnTriggerEnter(Collider other)
    {
        GameObject root = other.transform.root.gameObject;
        FishScore caughtFish = root.GetComponentInChildren<FishScore>();

        if (caughtFish != null)
        {
            totalScore += caughtFish.score;
            Debug.Log($"🐟 [{caughtFish.fishName}] +{caughtFish.score}점 (총 점수: {totalScore}점)");
            UpdateScoreUIPublic();
            Destroy(root);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject root = collision.transform.root.gameObject;
        FishScore caughtFish = root.GetComponentInChildren<FishScore>();

        if (caughtFish != null)
        {
            totalScore += caughtFish.score;
            Debug.Log($"🐟 [{caughtFish.fishName}] +{caughtFish.score}점 (총 점수: {totalScore}점)");
            UpdateScoreUIPublic();
            Destroy(root);
        }
    }

    public void UpdateScoreUIPublic()
    {
        if (scoreText != null)
            scoreText.text = $"점수\n{totalScore}";
    }
}