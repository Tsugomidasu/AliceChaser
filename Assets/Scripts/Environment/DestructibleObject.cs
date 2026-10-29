using UnityEngine;

public class DestructibleObject : MonoBehaviour
{
    [SerializeField] private int health = 4;

    [Header("Audio")]
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip breakClip;

    public void TakeDamage(int damage)
    {
        // Remove health.
        health -= damage;

        if (health <= 0)
        {
            GameAudio.Play(breakClip);
            Destroy(gameObject);
            return;
        }

        GameAudio.Play(hitClip);
    }
}