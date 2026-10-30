using UnityEngine;

public class AliceAnimationEvents : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip footstep01Clip;
    [SerializeField] private AudioClip footstep02Clip;

    private AliceCombat combat;
    private AliceAttackVFX attackVFX;
    private bool useSecondFootstep;

    private void Awake()
    {
        combat = GetComponentInParent<AliceCombat>();
        attackVFX = GetComponentInParent<AliceAttackVFX>();
    }

    public void PlayFootstep()
    {
        // Alternate footstep clips between animation events.
        GameAudio.Play(
            useSecondFootstep
                ? footstep02Clip
                : footstep01Clip
        );

        useSecondFootstep = !useSecondFootstep;
    }

    public void AttackHit()
    {
        // Let the animation decide the exact active frame for damage.
        combat.AttackHit();
    }

    public void AttackFinished()
    {
        combat.AttackFinished();
    }

    public void ShowVerticalSlash()
    {
        if (attackVFX != null)
        {
            attackVFX.ShowVerticalSlash();
        }
    }

    public void ShowHorizontalSlash()
    {
        if (attackVFX != null)
        {
            attackVFX.ShowHorizontalSlash();
        }
    }

    public void ShowThrust()
    {
        if (attackVFX != null)
        {
            attackVFX.ShowThrust();
        }
    }
}
