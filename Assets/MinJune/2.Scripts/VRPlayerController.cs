using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

public class VRPlayerController : MonoBehaviour
{
    public int hp = 100;
    public int attackPower = 2;
    public GameObject weapon;

    public InputActionProperty attackAction; // ���� �׼�

    private XROrigin xrOrigin;

    private void Awake()
    {
        xrOrigin = GetComponent<XROrigin>();

        if (weapon != null)
            weapon.GetComponent<Collider>().enabled = false; // ���� �ݶ��̴� ��Ȱ��ȭ

        StartCoroutine(RegisterPlayer());
    }

    IEnumerator RegisterPlayer()
    {
        while (GameManager.instance == null)
            yield return null;

        GameManager.instance.player = this.gameObject;
    }

    private void Update()
    {
        if (attackAction.action.WasPressedThisFrame())
        {
            StartCoroutine(DelayedAttack());
        }
    }

    IEnumerator DelayedAttack()
    {
        if (weapon != null)
        {
            weapon.GetComponent<Collider>().enabled = true;
            yield return new WaitForSeconds(0.5f);
            weapon.GetComponent<Collider>().enabled = false;
        }
    }
    public int maxHp = 100;
    public bool isDamaged = false;
    public void CalculateHP(int damage)
    {
        hp += damage;
        hp = Mathf.Clamp(hp, 0, maxHp);
    }

    public void CalculateHP(int damage, float time)
    {
        if (!isDamaged)
        {
            StartCoroutine(DamageDelay(time));
            hp += damage;
            hp = Mathf.Clamp(hp, 0, maxHp);
        }

    }

    IEnumerator DamageDelay(float time)
    {
        isDamaged = true;
        yield return new WaitForSeconds(time);
        isDamaged = false;
    }

    public int currentAmmo = 0; // ���� ź�� �� �߰�


    public void AddAmmo(int amount)
    {
        currentAmmo += amount;
    }

}
