using System;
using System.Collections.Generic;
using ASTeams.Base.Gameplay;
using UnityEngine;

public sealed class ReviveActionRouter : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] actions;

    public IReviveAction Resolve(FailType failType)
    {
        if (actions == null)
        {
            return null;
        }

        for (var i = 0; i < actions.Length; i++)
        {
            var mb = actions[i];
            if (mb == null)
            {
                continue;
            }

            if (mb is IReviveAction act && act.FailType == failType)
            {
                return act;
            }
        }

        return null;
    }

    public void TryExecute(FailType failType)
    {
        var act = Resolve(failType);
        act?.Execute();
    }

    public void RegisterAction(MonoBehaviour action)
    {
        if (action == null || action is not IReviveAction reviveAction)
        {
            return;
        }

        var registeredActions = new List<MonoBehaviour>();
        if (actions != null)
        {
            for (var i = 0; i < actions.Length; i++)
            {
                if (actions[i] == null)
                {
                    continue;
                }

                if (actions[i] is IReviveAction existing && existing.FailType == reviveAction.FailType)
                {
                    return;
                }

                registeredActions.Add(actions[i]);
            }
        }

        registeredActions.Add(action);
        actions = registeredActions.ToArray();
    }
}