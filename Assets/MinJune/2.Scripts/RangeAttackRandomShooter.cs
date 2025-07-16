using UnityEngine;

public class RangeAttackRandomShooter : MonoBehaviour
{
    public RangeAttack[] attackScripts;

    public void TriggerRandomAttack()
    {
        int tries = 0;
        while (tries < attackScripts.Length)
        {
            int idx = Random.Range(0, attackScripts.Length);
            if (!attackScripts[idx].IsOnCooldown)
            {
                attackScripts[idx].TriggerFire();
                return;
            }
            tries++;
        }
    }
}
