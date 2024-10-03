using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Crows_DragonBond
{
    [HarmonyPatch(typeof(Transferable), "CanAdjustBy")]
    public static class Transferable_CanAdjustBy_Patch
    {
        public static Transferable curTransferable;

        public static void Postfix(Transferable __instance, ref AcceptanceReport __result)
        {
            // Ensure that curTransferable is not the same as the previous transferable, and conditions are met.
            if (curTransferable != __instance && Find.WindowStack.IsOpen<Dialog_Trade>() && TradeSession.trader != null
                && DragonTradingUtility.IsDragonEggOrDragon(__instance.ThingDef) && TradeSession.trader.Faction != null
                && __instance.CountToTransferToDestination > 0)
            {
                curTransferable = __instance;

                // Skip showing the message if goodwill penalties are disabled
                if (!DragonBondMod.settings.goodwillPenaltyEnabled)
                {
                    Log.Message("[DragonBond] Goodwill penalties are disabled, skipping trade warning messages.");
                    return; // Exit early if the feature is disabled
                }

                }
            }
        }
    }

