using UnityEngine;

public class BGMTriggerZone : MonoBehaviour
{
    public enum ZoneType { Train, Era }
    public ZoneType zoneType = ZoneType.Train;
    public int eraBGMIndex = 1; // 시대칸 BGM 인덱스만 필요

    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag) || AudioManager.Instance == null) return;

        if (zoneType == ZoneType.Train)
        {
            AudioManager.Instance.PlayBGM(0); // 0번이 항상 열차 BGM
        }
        else if (zoneType == ZoneType.Era)
        {
            AudioManager.Instance.PlayBGM(eraBGMIndex); // 각 시대 BGM 인덱스 다르게!
        }
    }
}