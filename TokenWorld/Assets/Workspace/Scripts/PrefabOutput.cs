using UnityEngine;

// InteractionInput の座標を受け取り、指定した Prefab を生成する出力。
public class PrefabOutput : MonoBehaviour
{
    public GameObject reactionPrefab;
    [Min(0f)] public float interval = 0.4f;
    [Min(0.01f)] public float lifetime = 2f;
    public float heightAboveInput = 0.85f;

    private float nextReactionTime;

    // 引数は入力機器に依存しない、床面上のワールド座標。
    public void React(Vector3 worldPosition)
    {
        // 座標は毎フレーム届くため、生成頻度を抑える。複数点も同じ間隔を共有する。
        if (reactionPrefab == null || Time.time < nextReactionTime)
        {
            return;
        }

        nextReactionTime = Time.time + Mathf.Max(0f, interval);

        // 水面の波に隠れない高さへ。向きは Prefab の設定を使う。
        Vector3 spawnPosition = worldPosition + Vector3.up * heightAboveInput;
        GameObject reaction = Instantiate(
            reactionPrefab, spawnPosition, reactionPrefab.transform.rotation);

        Destroy(reaction, Mathf.Max(0.01f, lifetime));
    }
}
