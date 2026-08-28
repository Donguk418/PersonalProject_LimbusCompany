using System.Collections.Generic;
using UnityEngine;
using Limbus.Data;

namespace Limbus.Runtime
{
    public static class EnemyTargetingSolver
    {
        public static BattleInfo PickTarget(BattleInfo enemySlot, List<BattleInfo> playerSlots, TargetingPriority priority)
        {
            List<BattleInfo> validTargets = playerSlots.FindAll(s => s.Owner != null && !s.Owner.IsDead);
            if (validTargets.Count == 0) return null;

            switch (priority)
            {
                case TargetingPriority.LowestHp:
                    validTargets.Sort((a, b) => a.Owner.CurrentHp.CompareTo(b.Owner.CurrentHp));
                    return validTargets[0];

                case TargetingPriority.HighestHp:
                    validTargets.Sort((a, b) => b.Owner.CurrentHp.CompareTo(a.Owner.CurrentHp));
                    return validTargets[0];

                case TargetingPriority.LowestSpeed:
                    validTargets.Sort((a, b) => a.Speed.CompareTo(b.Speed));
                    return validTargets[0];

                case TargetingPriority.HighestSpeed:
                    validTargets.Sort((a, b) => b.Speed.CompareTo(a.Speed));
                    return validTargets[0];

                case TargetingPriority.LowestSanity:
                    validTargets.Sort((a, b) => a.Owner.CurrentSanity.CompareTo(b.Owner.CurrentSanity));
                    return validTargets[0];

                case TargetingPriority.FirstSlotPriority:
                    return validTargets[0];

                case TargetingPriority.Random:
                default:
                    int randomIndex = Random.Range(0, validTargets.Count);
                    return validTargets[randomIndex];
            }
        }
    }
}