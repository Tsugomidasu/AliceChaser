using UnityEngine;

public class AliceAnimationEvents : MonoBehaviour
{
    private AliceCombat combat;

    private void Awake()
    {
        combat = GetComponentInParent<AliceCombat>();
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
}