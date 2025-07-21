using System;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 5f;
        private float currentHealth;
        private IEnemy enemy;
        public event Action<float> OnTakeDamage;

        [SerializeField] private float damageCooldown = 0.1f;
        private float lastDamageTime;

        public float MaxHealth => maxHealth;

        private void Awake()
        {
            currentHealth = maxHealth;
            enemy = GetComponent<IEnemy>();
            lastDamageTime = -damageCooldown;
        }

        public void TakeDamage(float damage, GameObject damager)
        {
            if (Time.time - lastDamageTime < damageCooldown) return;
            lastDamageTime = Time.time;
            OnTakeDamage?.Invoke(damage);

            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            if (currentHealth <= 0f)
            {
                if (enemy != null) enemy.Die();
                else Destroy(gameObject);
            }
        }

        // ← 여기에 추가 ↓

        public void SetMaxHealth(float newMaxHealth)
        {
            maxHealth = Mathf.Max(0f, newMaxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        public void SetCurrentHealth(float newHealth)
        {
            currentHealth = Mathf.Clamp(newHealth, 0f, maxHealth);
        }

        public void AddHealth(float amount)
        {
            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        }
    }
}
