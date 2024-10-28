using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using static Crows_DragonBond.DragonTradingUtility;

namespace Crows_DragonBond
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.crows.dragonbond");
            harmony.PatchAll();
            if (Prefs.DevMode)
            {
                Log.Message("[DragonBond] Harmony patches applied.");
            }
        }
    }

    // Define the Harmony patch for TradeDeal.TryExecute
    [HarmonyPatch(typeof(TradeDeal), "TryExecute", new[] { typeof(bool) }, new[] { ArgumentType.Out })]
    public static class TradeDeal_TryExecute_Patch
    {
        // Store the count of dragon-related items being sold
        public static int dragonItemCount;

        // Prefix to count dragon eggs or dragons being sold before the trade is executed
        public static void Prefix(List<Tradeable> ___tradeables, out int __state)
        {
            if (Prefs.DevMode)
            {
                Log.Message("[DragonBond] TradeDeal.TryExecute_Patch Prefix called.");
            }
            __state = 0;
            dragonItemCount = 0;

            // Skip the entire block if the goodwill penalties are disabled
            if (!DragonBondMod.settings.goodwillPenaltyEnabled)
            {
                if (Prefs.DevMode)
                {
                    Log.Message("[DragonBond] Goodwill penalties are disabled, skipping warnings and penalty calculations.");
                }
                return; // Exit early if the feature is disabled
            }

            // Get the trading faction
            Faction tradingFaction = TradeSession.trader.Faction;

            // Skip the entire block if trading with Velos Enclave or Ashen Dominion
            if (FactionCheckLogger.IsVelosOrAshenFaction(tradingFaction))
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"[DragonBond] Trading with favored faction: {tradingFaction.Name}. Skipping warnings.");
                }
                return; // Exit early if trading with Velos Enclave or Ashen Dominion
            }

            foreach (Tradeable tradeable in ___tradeables)
            {
                if (tradeable.ActionToDo == TradeAction.PlayerSells && DragonTradingUtility.IsDragonEggOrDragon(tradeable.ThingDef))
                {
                    __state += tradeable.CountToTransferToDestination;
                    dragonItemCount += tradeable.CountToTransferToDestination;
                    if (Prefs.DevMode)
                    {
                        Log.Message($"[DragonBond] Found {tradeable.CountToTransferToDestination} {tradeable.ThingDef.label} selected for sale.");
                    }

                    // Show warning when selling/gifting dragons or dragon eggs, but only if not trading with favored factions
                    if (TradeSession.giftMode)
                    {
                        Messages.Message("CrowsDragonBond.GiftingDragonWarning".Translate(), MessageTypeDefOf.CautionInput);
                    }
                    else
                    {
                        Messages.Message("CrowsDragonBond.SellingDragonWarning".Translate(), MessageTypeDefOf.CautionInput);
                    }
                }
            }
            if (Prefs.DevMode)
            {
                Log.Message($"[DragonBond] Total dragon items selected for sale: {dragonItemCount}");
            }
        }

        // Postfix to apply goodwill penalties after the trade is executed
        public static void Postfix(int __state, bool __result)
        {
            if (Prefs.DevMode)
            {
                Log.Message("[DragonBond] TradeDeal.TryExecute_Patch Postfix called.");
                Log.Message($"[DragonBond] Trade result: {__result}, Dragon items traded: {__state}");
            }

            // If items were sold and the trade executed successfully
            if (__state > 0 && __result)
            {
                Faction tradingFaction = TradeSession.trader.Faction;
                if (Prefs.DevMode)
                {
                    Log.Message($"[DragonBond] Trading with faction: {tradingFaction?.Name ?? "Null"}");
                }

                if (tradingFaction != null && !FactionCheckLogger.IsVelosOrAshenFaction(tradingFaction))
                {
                    if (Prefs.DevMode)
                    {
                        Log.Message($"[DragonBond] Trade completed with non-favored faction: {tradingFaction.Name}. Applying goodwill penalty.");
                    }
                    AdjustGoodwillForDragonSale();

                }
                else
                {
                    if (Prefs.DevMode)
                    {
                        Log.Message("[DragonBond] Trade completed with favored faction or no faction detected.");
                    }
                }
            }
            else
            {
                if (Prefs.DevMode)
                {
                    Log.Message("[DragonBond] Trade not executed or no dragon items were sold.");
                }
            }
        }

        // Adjust goodwill for Velos Enclave and Ashen Dominion
        // Replace direct goodwill adjustments with delayed goodwill impacts.
        private static void AdjustGoodwillForDragonSale()
        {
            if (!DragonBondMod.settings.goodwillPenaltyEnabled)
            {
                if (Prefs.DevMode)
                {
                    Log.Message("[DragonBond] Goodwill penalties are disabled by mod settings.");
                }
                return; // If the setting is disabled, skip the penalty logic.
            }
            if (Prefs.DevMode)
            {
                Log.Message("[DragonBond] Adjusting goodwill for Velos Enclave and Ashen Dominion.");
            }

            Faction velosEnclave = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("Crows_VelosEnclave"));
            Faction ashenDominion = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("Crows_AshenDominion"));

            // Retrieve the instance of GameComponent_DragonBondManager from Current.Game.components
            GameComponent_DragonBondManager dragonBondManager = Current.Game.components.FirstOrDefault(c => c is GameComponent_DragonBondManager) as GameComponent_DragonBondManager;

            if (dragonBondManager == null)
            {
                Log.Error("[DragonBond] Could not find DragonBondManager component.");
                return;
            }

            if (velosEnclave != null)
            {
                if (Prefs.DevMode)
                {
                    Log.Message("[DragonBond] Scheduling delayed goodwill penalty for Velos Enclave.");
                }
                dragonBondManager.AddGoodwillImpact(velosEnclave, -20, 60000); // Delayed by 60000 ticks (1 in-game day)
            }
            else
            {
                Log.Warning("[DragonBond] Velos Enclave faction not found.");
            }

            if (ashenDominion != null)
            {
                if (Prefs.DevMode)
                {
                    Log.Message("[DragonBond] Scheduling delayed goodwill penalty for Ashen Dominion.");
                }
                dragonBondManager.AddGoodwillImpact(ashenDominion, -20, 60000); // Delayed by 60000 ticks (1 in-game day)
            }
            else
            {
                Log.Warning("[DragonBond] Ashen Dominion faction not found.");
            }
        }
    }
}
