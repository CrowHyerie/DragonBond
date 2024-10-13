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
            // Ensure this is a trade window and a valid trader is present
            if (Find.WindowStack.IsOpen<Dialog_Trade>() && TradeSession.trader != null
                && DragonTradingUtility.IsDragonEggOrDragon(__instance.ThingDef) && TradeSession.trader.Faction != null
                && __instance.CountToTransferToDestination > 0)
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

                    // Determine whether it's a gifting session or a normal trade
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
