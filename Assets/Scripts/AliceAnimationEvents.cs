using UnityEngine;

public class AliceAnimationEvents : MonoBehaviour
{
    private AliceCombat combat;
    private AliceAttackVFX attackVFX;

    private void Awake()
    {
        combat = GetComponentInParent<AliceCombat>();
        attackVFX = GetComponentInParent<AliceAttackVFX>();
    }

    public void AttackHit()
    {
        // Apply attack damage.
        combat.AttackHit();
    }

    public void AttackFinished()
    {
        // Finish or continue the combo.
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
}