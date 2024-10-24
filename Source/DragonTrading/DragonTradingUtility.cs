﻿using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Crows_DragonBond
{
    // This class contains utility methods for commonly used checks and actions related to the Dragon Bond mod.
    public static class DragonTradingUtility
    {
        // Check if the item is a dragon or dragon egg.
        public static bool IsDragonEggOrDragon(ThingDef def)
        {
            AbilityDef dragonBondAbility = DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbility", false);

            if (dragonBondAbility == null)
            {
                Log.Error("[DragonBond] Crows_DragonBondAbility not found in DefDatabase!");
                return false;
            }

            var dragonExtension = dragonBondAbility.GetModExtension<ModExtension_Crows_DragonBond>();
            if (dragonExtension != null && dragonExtension.allowedAnimals.Contains(def))
            {
                return true;
            }

            var eggExtension = dragonBondAbility.GetModExtension<ModExtension_Crows_DragonEggs>();
            if (eggExtension != null && eggExtension.allowedEggs.Contains(def))
            {
                return true;
            }

            return false;
        }

        public static class FactionCheckLogger
        {
            // Static flag to check if we've already logged a message
            private static bool hasLoggedOnce = false;
            // Static dictionary to track which factions have already been logged
            private static HashSet<Faction> loggedFactions = new HashSet<Faction>();

            // Check if the faction is Velos Enclave or Ashen Dominion.
            public static bool IsVelosOrAshenFaction(Faction faction)
            {
                if (faction == null)
                {
                    return false;
                }

                bool isVelosOrAshen = faction.def.defName == "Crows_VelosEnclave" || faction.def.defName == "Crows_AshenDominion";

                // Log message if the faction hasn't been logged yet
                if (Prefs.DevMode && !loggedFactions.Contains(faction))
                {
                    Log.Message($"[DragonBond] Faction {faction?.Name} is Velos or Ashen: {isVelosOrAshen}");
                    loggedFactions.Add(faction);  // Mark faction as logged
                }

                return isVelosOrAshen;
            }
        }

    }
}
