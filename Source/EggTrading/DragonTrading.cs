using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Crows_DragonBond
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.crows.dragonbond");
            harmony.PatchAll();
            Log.Message("[DragonBond] Harmony patches applied.");
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
            Log.Message("[DragonBond] TradeDeal.TryExecute_Patch Prefix called.");
            __state = 0;
            dragonItemCount = 0;

            foreach (Tradeable tradeable in ___tradeables)
            {
                if (tradeable.ActionToDo == TradeAction.PlayerSells && IsDragonEggOrDragon(tradeable.ThingDef))
                {
                    __state += tradeable.CountToTransferToDestination;
                    dragonItemCount += tradeable.CountToTransferToDestination;
                    Log.Message($"[DragonBond] Found {tradeable.CountToTransferToDestination} {tradeable.ThingDef.label} selected for sale.");
                }
            }

            Log.Message($"[DragonBond] Total dragon items selected for sale: {dragonItemCount}");
        }

        // Postfix to apply goodwill penalties after the trade is executed
        public static void Postfix(Tradeable __instance)
        {
            // Only show the message if the player is selling (PlayerSells) and the item is a dragon or dragon egg
            if (__instance.ActionToDo == TradeAction.PlayerSells && TradeDeal_TryExecute_Patch.IsDragonEggOrDragon(__instance.ThingDef))
            {
                Messages.Message("[DragonBond] Warning: Selling dragon eggs or dragons will have consequences with certain factions!", MessageTypeDefOf.NegativeEvent, historical: false);
            }
        }

            // Check if the item is a dragon or dragon egg using the ModExtensions
            private static bool IsDragonEggOrDragon(ThingDef def)
        {
            // Get the AbilityDef that holds the mod extensions
            AbilityDef dragonBondAbility = DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbility", false);

            if (dragonBondAbility == null)
            {
                Log.Error("[DragonBond] Crows_DragonBondAbility not found in DefDatabase!");
                return false;
            }

            // Check if the ThingDef is in the allowedAnimals or allowedEggs list
            var dragonExtension = dragonBondAbility.GetModExtension<ModExtension_Crows_DragonBond>();
            if (dragonExtension != null && dragonExtension.allowedAnimals.Contains(def))
            {
                Log.Message($"[DragonBond] {def.defName} is recognized as a dragon via AbilityDef ModExtension_Crows_DragonBond.");
                return true;
            }

            var eggExtension = dragonBondAbility.GetModExtension<ModExtension_Crows_DragonEggs>();
            if (eggExtension != null && eggExtension.allowedEggs.Contains(def))
            {
                Log.Message($"[DragonBond] {def.defName} is recognized as a dragon egg via AbilityDef ModExtension_Crows_DragonEggs.");
                return true;
            }

            Log.Message($"[DragonBond] {def.defName} is not recognized as a dragon or dragon egg.");
            return false;
        }

        // Check if the faction is Velos Enclave or Ashen Dominion
        private static bool IsVelosOrAshenFaction(Faction faction)
        {
            bool isVelosOrAshen = faction.def.defName == "Crows_VelosEnclave" || faction.def.defName == "Crows_AshenDominion";
            Log.Message($"[DragonBond] Faction {faction?.Name} is Velos or Ashen: {isVelosOrAshen}");
            return isVelosOrAshen;
        }

        // Adjust goodwill for Velos Enclave and Ashen Dominion
        private static void AdjustGoodwillForDragonSale()
        {
            Log.Message("[DragonBond] Adjusting goodwill for Velos Enclave and Ashen Dominion.");
            Faction velosEnclave = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("Crows_VelosEnclave"));
            Faction ashenDominion = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("Crows_AshenDominion"));

            if (velosEnclave != null)
            {
                Log.Message("[DragonBond] Applying goodwill penalty to Velos Enclave.");
                velosEnclave.TryAffectGoodwillWith(Faction.OfPlayer, -20, true, false, null, null);
            }
            else
            {
                Log.Warning("[DragonBond] Velos Enclave faction not found.");
            }

            if (ashenDominion != null)
            {
                Log.Message("[DragonBond] Applying goodwill penalty to Ashen Dominion.");
                ashenDominion.TryAffectGoodwillWith(Faction.OfPlayer, -20, true, false, null, null);
            }
            else
            {
                Log.Warning("[DragonBond] Ashen Dominion faction not found.");
            }
        }
    }
}
