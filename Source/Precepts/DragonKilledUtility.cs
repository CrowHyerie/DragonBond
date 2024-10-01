using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace Crows_DragonBond
{
    [StaticConstructorOnStartup]
    public static class DragonKilledUtility
    {
        // The method for applying both mood and social debuffs after a dragon is killed
        public static void Postfix_Kill(Pawn __instance, DamageInfo? dinfo)
        {
            Pawn killer = dinfo?.Instigator as Pawn;

            if (killer != null && IsDragon(__instance) && IsIdeologyDragonVeneration(killer))
            {
                // Apply the "killed dragon" mood debuff to the killer
                ApplyMoodDebuffForKillingDragon(killer);

                // Apply the social debuff to all other pawns in the faction
                ApplySocialDebuffForKillingDragon(killer);
            }
        }

        private static void ApplySocialDebuffForKillingDragon(Pawn killer)
        {
            // Iterate over all colonists in the killer's faction
            foreach (Pawn pawn in killer.Map.mapPawns.FreeColonists)
            {
                // Skip the killer, since they shouldn't have a social debuff against themselves
                if (pawn == killer)
                    continue;

                // Apply the social debuff to other colonists
                Thought_MemorySocial socialThought = (Thought_MemorySocial)ThoughtMaker.MakeThought(ThoughtDef.Named("Crows_SocialDragonKilledDebuff"));
                socialThought.opinionOffset = -30;  // Adjust based on your ThoughtDef

                // Check if the pawn has the memory system
                if (pawn.needs?.mood?.thoughts?.memories != null)
                {
                    // Add the social thought, referencing the killer
                    pawn.needs.mood.thoughts.memories.TryGainMemory(socialThought, killer);
                }
            }
        }
        // Check if the killed pawn is a dragon
        private static bool IsDragon(Pawn pawn)
        {
            ModExtension_Crows_DragonBond modExt = DefDatabase<AbilityDef>
                .GetNamed("Crows_DragonBondAbility")
                .GetModExtension<ModExtension_Crows_DragonBond>();

            return modExt != null && modExt.allowedAnimals.Contains(pawn.def);
        }

        // Check if the killer is part of the ideology that venerates dragons
        private static bool IsIdeologyDragonVeneration(Pawn pawn)
        {
            return pawn.Ideo != null && pawn.Ideo.HasPrecept(DefDatabase<PreceptDef>.GetNamed("Crows_DragonVeneratedPrecept"));
        }

        // Apply the mood debuff for killing a dragon
        private static void ApplyMoodDebuffForKillingDragon(Pawn killer)
        {
            Thought_Memory thought = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDef.Named("Crows_DragonKilledMoodDebuff"));
            killer.needs.mood.thoughts.memories.TryGainMemory(thought);
        }
    }

    [StaticConstructorOnStartup]
    public static class Patch_Pawn_Kill
    {
        static Patch_Pawn_Kill()
        {
            var harmony = new Harmony("com.crows.dragonbond");
            harmony.Patch(
                original: AccessTools.Method(typeof(Pawn), "Kill"),
                postfix: new HarmonyMethod(typeof(Patch_Pawn_Kill), nameof(Postfix_Kill))
            );
        }

        public static void Postfix_Kill(Pawn __instance, DamageInfo? dinfo)
        {
            DragonKilledUtility.Postfix_Kill(__instance, dinfo);
        }
    }
}