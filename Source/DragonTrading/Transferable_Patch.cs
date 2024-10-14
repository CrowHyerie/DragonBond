using HarmonyLib;
using RimWorld;
using Verse;
using static Crows_DragonBond.DragonTradingUtility;

namespace Crows_DragonBond
{
    [HarmonyPatch(typeof(Transferable), "CanAdjustBy")]
    public static class Transferable_CanAdjustBy_Patch
    {
        private static Transferable lastWarnedTransferable;

        public static void Postfix(Transferable __instance, ref AcceptanceReport __result)
        {
            // Avoid interference if another mod has already shown a warning (e.g., VanillaPsycastsExpanded)
            if (__instance.CountToTransferToDestination <= 0 || __result.Accepted == false)
            {
                // If nothing is being transferred or adjustment is rejected, skip processing
                return;
            }

            // Get the trading faction
            Faction tradingFaction = TradeSession.trader?.Faction;
            if (tradingFaction == null) return; // Ensure a valid trader is present

            // Skip if trading with Velos Enclave or Ashen Dominion
            if (FactionCheckLogger.IsVelosOrAshenFaction(tradingFaction))
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"[DragonBond] Trading with favored faction: {tradingFaction.Name}. Skipping warnings.");
                }
                return; // Exit early for Velos Enclave or Ashen Dominion
            }

            // Only process dragon-related items
            if (DragonTradingUtility.IsDragonEggOrDragon(__instance.ThingDef))
            {
                // Prevent showing the warning multiple times for the same item
                if (lastWarnedTransferable != __instance)
                {
                    lastWarnedTransferable = __instance;

                    // Skip showing the message if goodwill penalties are disabled
                    if (!DragonBondMod.settings.goodwillPenaltyEnabled)
                    {
                        if (Prefs.DevMode)
                        {
                            Log.Message("[DragonBond] Goodwill penalties are disabled, skipping trade warning messages.");
                        }
                        return;
                    }

                    // Show appropriate warning depending on trade mode (gifting or selling)
                    if (TradeSession.giftMode)
                    {
                        Messages.Message("CrowsDragonBond.GiftingDragonWarning".Translate(), MessageTypeDefOf.CautionInput);
                    }
                    else
                    {
                        Messages.Message("CrowsDragonBond.SellingDragonWarning".Translate(), MessageTypeDefOf.CautionInput);
                    }

                    if (Prefs.DevMode)
                    {
                        Log.Message($"[DragonBond] Warning shown for trading {__instance.CountToTransferToDestination} {__instance.ThingDef.label}");
                    }
                }
            }
        }
    }
}
