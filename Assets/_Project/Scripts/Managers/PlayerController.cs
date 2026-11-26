using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private List<HeroProperties> _heroesParty = new();

    private readonly List<ActionProperties> _actions = new();

    private void Start()
    {
        CombatManager combat = CombatManager.Instance;

        combat.OnCombatStarted += HandleCombatStarted;
        combat.OnCombatStopped += HandleCombatStopped;
        combat.OnTick += HandleTick;
        InputManager.Instance.OnButtonPressed += HandleButtonPressed;
    }

    private void OnDestroy()
    {
        CombatManager.Instance.OnCombatStarted -= HandleCombatStarted;
        CombatManager.Instance.OnCombatStopped -= HandleCombatStopped;
        CombatManager.Instance.OnTick -= HandleTick;
        InputManager.Instance.OnButtonPressed -= HandleButtonPressed;
    }

    private void HandleCombatStarted()
    {
        foreach (HeroProperties hero in _heroesParty)
        {
            foreach (ActionProperties action in hero.actions)
            {
                ActionProperties copy = Instantiate(action);
                copy.cooldown = 0;
                _actions.Add(copy);
            }
        }
    }

    private void HandleCombatStopped()
    {
        ClearActions();
    }

    private void HandleButtonPressed(int action)
    {
        
    }

    private void HandleTick()
    {
        foreach (ActionProperties action in _actions)
        {
            if (action.cooldown > 0)
            {
                action.cooldown--;
            }
        }
    }

    private void ClearActions()
    {
        if (_actions.Count > 0)
        {
            _actions.Clear();
        }
    }
}