using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace DragonBondMod
{
    internal class DragonBondUtils
    {
        // Constants for ThoughtDefs and HediffDefs
        private const string DRAGON_BOND_HEDIFF_DEF_NAME = "Crows_DragonBondHediff";
        private const string DRAGON_BONDED_CLOSE_DEF_NAME = "Crows_DragonBondedClose";
        private const string DRAGON_BONDED_MEDIUM_DEF_NAME = "Crows_DragonBondedMediumDistance";
        private const string DRAGON_BONDED_FAR_DEF_NAME = "Crows_DragonBondedFar";
        private const string DRAGON_BONDED_DEAD_DEF_NAME = "Crows_DragonBondedDead";

        // Cache the relevant Hediff and ThoughtDefs
        private static HediffDef dragonBondHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(DRAGON_BOND_HEDIFF_DEF_NAME);
        private static ThoughtDef dragonBondedCloseDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(DRAGON_BONDED_CLOSE_DEF_NAME);
        private static ThoughtDef dragonBondedMediumDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(DRAGON_BONDED_MEDIUM_DEF_NAME);
        private static ThoughtDef dragonBondedFarDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(DRAGON_BONDED_FAR_DEF_NAME);
        private static ThoughtDef dragonBondedDeadDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(DRAGON_BONDED_DEAD_DEF_NAME);

        // Method to apply the Dragon Bond Hediff
        public static void ApplyDragonBond(Pawn pawn, Pawn dragon)
        {
            if (pawn.health != null && dragonBondHediffDef != null)
            {
                // Add the Dragon Bond Hediff to the pawn if they don't have it already
                Hediff existingBond = pawn.health.hediffSet.GetFirstHediffOfDef(dragonBondHediffDef);
                if (existingBond == null)
                {
                    Hediff newBond = HediffMaker.MakeHediff(dragonBondHediffDef, pawn);
                    pawn.health.AddHediff(newBond);
                }

                // Update the thought based on proximity to the dragon
                UpdateDragonBondThought(pawn, dragon);
            }
        }

        // Method to update the Dragon Bond Thought based on distance
        public static void UpdateDragonBondThought(Pawn pawn, Pawn dragon)
        {
            if (pawn.needs?.mood?.thoughts != null && dragon != null)
            {
                float distance = (pawn.Position - dragon.Position).LengthHorizontal;
                ThoughtDef currentThoughtDef = null;

                if (dragon.Dead)
                {
                    currentThoughtDef = dragonBondedDeadDef;
                }
                else if (distance < 10f) // Close range
                {
                    currentThoughtDef = dragonBondedCloseDef;
                }
                else if (distance < 50f) // Medium distance
                {
                    currentThoughtDef = dragonBondedMediumDef;
                }
                else // Far distance
                {
                    currentThoughtDef = dragonBondedFarDef;
                }

                // Remove existing bond-related thoughts
                ClearBondThoughts(pawn, dragon);

                // Apply new thought based on proximity
                if (currentThoughtDef != null)
                {
                    Thought_Memory newThought = (Thought_Memory)ThoughtMaker.MakeThought(currentThoughtDef);
                    newThought.otherPawn = dragon;
                    pawn.needs.mood.thoughts.memories.TryGainMemory(newThought);
                }
            }
        }

        private static void ClearBondThoughts(Pawn pawn, Pawn dragon)
        {
            // Ensure that all bond-related thoughts are removed before adding new ones
            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(dragonBondedCloseDef, dragon);
            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(dragonBondedMediumDef, dragon);
            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(dragonBondedFarDef, dragon);
            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(dragonBondedDeadDef, dragon);
        }

        // Method to remove the Dragon Bond (e.g., upon dragon death)
        public static void RemoveDragonBond(Pawn pawn, Pawn dragon)
        {
            Hediff bondHediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(dragonBondHediffDef);
            if (bondHediff != null)
            {
                pawn.health.RemoveHediff(bondHediff);
            }

            // Remove all associated thoughts
            ClearBondThoughts(pawn, dragon);
        }
    }
}
