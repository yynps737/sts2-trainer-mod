using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Sts2Trainer.Shared;

namespace Sts2Trainer.Mod.Runtime;

internal static class TrainerPatches
{
    public static void Apply(Harmony harmony)
    {
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    private static bool TryGetSettings(out TrainerSettings? settings)
    {
        settings = TrainerRuntime.Current?.Settings;
        return settings is not null && TrainerRuntime.Current!.GameplayFeaturesAllowed;
    }

    [HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.GainGold))]
    private static class GainGoldPatch
    {
        private static void Prefix(ref decimal amount)
        {
            if (!TryGetSettings(out var settings))
            {
                return;
            }

            if (settings!.GoldMultiplier > 1m)
            {
                amount *= settings.GoldMultiplier;
            }
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyMerchantPrice))]
    private static class MerchantPricePatch
    {
        private static void Postfix(ref decimal __result)
        {
            if (TryGetSettings(out var settings) && settings!.FreePurchaseInShop)
            {
                __result = 0m;
            }
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ShouldForcePotionReward))]
    private static class PotionRewardPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (TryGetSettings(out var settings) && settings!.AlwaysRewardPotion)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardRewardUpgradeOdds))]
    private static class UpgradeRewardOddsPatch
    {
        private static void Postfix(ref decimal __result)
        {
            if (TryGetSettings(out var settings) && settings!.AlwaysUpgradeCardRewards)
            {
                __result = 1m;
            }
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardRewardCreationOptions))]
    private static class CardRewardRarityPatch
    {
        private static void Postfix(ref CardCreationOptions __result, Player player)
        {
            if (!TryGetSettings(out var settings) || !settings!.MaxCardRewardRarity)
            {
                return;
            }

            var pool = __result.GetPossibleCards(player).ToList();
            if (pool.Count == 0)
            {
                return;
            }

            var rarePool = pool.Where(static c => c.Rarity == CardRarity.Rare).ToList();
            if (rarePool.Count > 0)
            {
                __result = __result.WithCustomPool(rarePool, CardRarityOddsType.Uniform);
                return;
            }

            var uncommonPool = pool.Where(static c => c.Rarity == CardRarity.Uncommon).ToList();
            if (uncommonPool.Count > 0)
            {
                __result = __result.WithCustomPool(uncommonPool, CardRarityOddsType.Uniform);
            }
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyUnknownMapPointRoomTypes))]
    private static class UnknownMapPatch
    {
        private static void Postfix(ref IReadOnlySet<RoomType> __result)
        {
            if (TryGetSettings(out var settings) && settings!.UnknownMapPointsAlwaysGiveTreasure)
            {
                __result = new HashSet<RoomType> { RoomType.Treasure };
            }
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyMaxEnergy))]
    private static class MaxEnergyPatch
    {
        private static void Postfix(ref decimal __result)
        {
            if (TryGetSettings(out var settings) && settings!.EnforceMaxEnergy)
            {
                __result = settings.MaxEnergyTarget;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.LoseEnergy))]
    private static class LoseEnergyPatch
    {
        private static bool Prefix()
        {
            return !(TryGetSettings(out var settings) && settings!.UnlimitedEnergy);
        }
    }

    [HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.LoseStars))]
    private static class LoseStarsPatch
    {
        private static bool Prefix()
        {
            return !(TryGetSettings(out var settings) && settings!.UnlimitedStars);
        }
    }

    [HarmonyPatch(typeof(Creature), nameof(Creature.TakeTurn))]
    private static class FreezeEnemyTurnPatch
    {
        private static bool Prefix(Creature __instance, ref Task __result)
        {
            if (TryGetSettings(out var settings) &&
                settings!.FreezeEnemies &&
                __instance.IsMonster &&
                __instance.Side == CombatSide.Enemy)
            {
                __result = Task.CompletedTask;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage), [typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel)])]
    private static class DamageCommandPatch
    {
        private static void Prefix(ref IEnumerable<Creature> targets, ref decimal amount, Creature? dealer)
        {
            if (!TryGetSettings(out var settings))
            {
                return;
            }

            var targetList = targets.ToList();
            targets = targetList;

            if (targetList.Count == 0)
            {
                return;
            }

            var allTargetsArePlayers = targetList.All(static t => t.Side == CombatSide.Player);
            var allTargetsAreEnemies = targetList.All(static t => t.Side == CombatSide.Enemy);

            if (allTargetsAreEnemies && dealer is not null && dealer.Side == CombatSide.Player && settings!.DamageMultiplier > 1m)
            {
                amount *= settings.DamageMultiplier;
            }

            if (!allTargetsArePlayers)
            {
                return;
            }

            if (settings!.GodMode)
            {
                amount = 0m;
                return;
            }

            if (settings.DefenseMultiplier > 1m)
            {
                amount /= settings.DefenseMultiplier;
            }
        }
    }
}
