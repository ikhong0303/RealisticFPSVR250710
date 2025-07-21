using UnityEngine;

public class VRPlayerController : MonoBehaviour
{
    public int hp = 100;
    public int maxHp = 100;

    /// <summary>
    /// damage 값을 hp에 더하고 0~maxHp로 클램프
    /// </summary>
    public void CalculateHP(int damage)
    {
        hp += damage;
        hp = Mathf.Clamp(hp, 0, maxHp);
    }
}
