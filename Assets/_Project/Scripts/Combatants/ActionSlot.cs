using UnityEngine;
using UnityEngine.UI;

public class ActionSlot : MonoBehaviour
{
    [SerializeField] private Image _cooldownProgress;

    public ActionProperties Action;
    public float RemainingTicks { get; private set; }
    public bool IsReady => RemainingTicks <= 0;

    public void Initialize(ActionProperties action)
    {
        Action = action;
    }

    public void StartCooldown() => RemainingTicks = Action.cooldown;

    public void Tick()
    {
        if (RemainingTicks > 0) RemainingTicks--;
        _cooldownProgress.fillAmount = RemainingTicks / Action.cooldown;
    }
}