using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 4;

    [Header("Hit Reaction")]
    [SerializeField] private float hitStunDuration = 0.18f;

    [Header("Defeat")]
    [SerializeField] private float defeatPauseDuration = 0.08f;
    [SerializeField] private float defeatKnockbackSpeed = 2f;
    [SerializeField] private float defeatFadeDuration = 0.3f;
    [SerializeField] private bool fallOnDefeat;
    [SerializeField] private float defeatUpwardSpeed = 2f;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsHitStunned { get; private set; }

    private Rigidbody2D rb;
    private SpriteRenderer[] spriteRenderers;
    private Animator[] animators;
    private ContactDamage[] contactDamageSources;

    private Coroutine hitStunRoutine;
    private Coroutine defeatRoutine;

    private float defeatDirection;

    private void Awake()
    {
        // Cache components used by the shared hit and defeat reactions.
        rb = GetComponent<Rigidbody2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        animators = GetComponentsInChildren<Animator>(true);
        contactDamageSources = GetComponentsInChildren<ContactDamage>(true);

        // Initialize health.
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(int damage, Vector2 damageSource)
    {
        // Ignore invalid damage.
        if (IsDead || damage <= 0)
        {
            return;
        }

        // Store which side the hit came from for defeat knockback.
        defeatDirection = transform.position.x >= damageSource.x
            ? 1f
            : -1f;

        // Remove health.
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        // Start the shared defeat reaction at zero health.
        if (CurrentHealth <= 0)
        {
            StartDefeat();
            return;
        }

        // Briefly interrupt the enemy after a successful hit.
        StartHitStun();
    }

    private void StartHitStun()
    {
        // Skip hit stun when its duration is disabled.
        if (hitStunDuration <= 0f)
        {
            return;
        }

        // Restart the timer when another hit lands during hit stun.
        if (hitStunRoutine != null)
        {
            StopCoroutine(hitStunRoutine);
        }

        hitStunRoutine = StartCoroutine(HitStunRoutine());
    }

    private IEnumerator HitStunRoutine()
    {
        IsHitStunned = true;
        yield return new WaitForSeconds(hitStunDuration);
        IsHitStunned = false;
        hitStunRoutine = null;
    }

    private void StartDefeat()
    {
        IsDead = true;
        IsHitStunned = false;

        // Stop any normal hit reaction before the defeat reaction begins.
        if (hitStunRoutine != null)
        {
            StopCoroutine(hitStunRoutine);
            hitStunRoutine = null;
        }

        // Stop all contact damage immediately on the final hit.
        foreach (ContactDamage contactDamage in contactDamageSources)
        {
            if (contactDamage != null)
            {
                contactDamage.enabled = false;
            }
        }

        if (defeatRoutine != null)
        {
            StopCoroutine(defeatRoutine);
        }

        defeatRoutine = StartCoroutine(DefeatRoutine());
    }

    private IEnumerator DefeatRoutine()
    {
        // Freeze movement and animation briefly so the final hit reads clearly.
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        foreach (Animator enemyAnimator in animators)
        {
            if (enemyAnimator != null)
            {
                enemyAnimator.speed = 0f;
            }
        }

        if (defeatPauseDuration > 0f)
        {
            yield return new WaitForSeconds(defeatPauseDuration);
        }

        // Recoil away from the source of the final hit.
        if (rb != null)
        {
            float upwardSpeed = fallOnDefeat
                ? defeatUpwardSpeed
                : rb.linearVelocityY;

            rb.linearVelocity = new Vector2(
                defeatDirection * defeatKnockbackSpeed,
                upwardSpeed
            );
        }

        // Fade the enemy before removing it.
        if (defeatFadeDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < defeatFadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(
                    elapsed / defeatFadeDuration
                );

                foreach (SpriteRenderer spriteRenderer in spriteRenderers)
                {
                    if (spriteRenderer == null)
                    {
                        continue;
                    }

                    Color color = spriteRenderer.color;
                    color.a = alpha;
                    spriteRenderer.color = color;
                }

                yield return null;
            }
        }

        Destroy(gameObject);
    }

    private void OnDisable()
    {
        // Clear the temporary hit reaction state when disabled.
        if (hitStunRoutine != null)
        {
            StopCoroutine(hitStunRoutine);
            hitStunRoutine = null;
        }

        if (defeatRoutine != null)
        {
            StopCoroutine(defeatRoutine);
            defeatRoutine = null;
        }

        IsHitStunned = false;
    }
}
