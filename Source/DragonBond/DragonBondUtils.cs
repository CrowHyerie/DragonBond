using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Crows_DragonBond
{
    internal class DragonBondUtils
    {
        // Color for when the bond is torn, using a reddish hue to indicate the severed bond.
        public static Color DragonBondTornLabelColor => new Color(0.9f, 0.3f, 0.3f);

        // Defining constants for the custom thought and hediff defs in your mod
        private const string THOUGHT_DRAGON_BOND_PROXIMITY_DEF_NAME = "Crows_DragonBondProximity";

        // Thought Def reference for the bond
        private static ThoughtDef dragonBondProximityDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(THOUGHT_DRAGON_BOND_PROXIMITY_DEF_NAME);

        // Thought stages for the bond
        private static ThoughtStage dragonBondStageCloseMood = dragonBondProximityDef?.stages?.FirstOrDefault(stage => stage.label == "dragon bond");
        private static ThoughtStage dragonBondStageDistanceMood = dragonBondProximityDef?.stages?.FirstOrDefault(stage => stage.label == "dragon bond distance");

        // Create a dragon bond between a pawn and their dragon
        public static void CreateDragonBond(Pawn pawn, Pawn dragon)
        {
            // Existing bond creation logic remains the same.
            if (pawn == null || dragon == null || pawn.relations == null || dragon.relations == null)
            {
                Log.Error("Failed to create bond: pawn or dragon or their relations tracker is null.");
                return;
            }

            if (pawn.relations.DirectRelationExists(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), dragon))
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"{pawn.NameShortColored} already has a bond with {dragon.NameShortColored}.");
                }
                return;
            }

            // Add the direct relationship for Dragon Bond
            pawn.relations.AddDirectRelation(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), dragon);
            dragon.relations.AddDirectRelation(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), pawn);

            // Use Verb_DragonBond to determine the correct Hediff for the bond
            HediffDef pawnBondHediffDef = Verb_DragonBond.GetDragonBondHediffForDragon(dragon);
            HediffDef dragonBondHediffDef = Verb_DragonBond.GetDragonBondHediffForDragon(dragon);

            Hediff pawnBondHediff = HediffMaker.MakeHediff(pawnBondHediffDef, pawn);
            pawn.health.AddHediff(pawnBondHediff);

            Hediff dragonBondHediff = HediffMaker.MakeHediff(dragonBondHediffDef, dragon);
            dragon.health.AddHediff(dragonBondHediff);

            // Link the dragon and pawn using HediffComp_DragonBondLink
            var pawnComp = pawnBondHediff.TryGetComp<HediffComp_DragonBondLink>();
            if (pawnComp != null)
            {
                pawnComp.SetLinkedPawn(dragon);
            }

            var dragonComp = dragonBondHediff.TryGetComp<HediffComp_DragonBondLink>();
            if (dragonComp != null)
            {
                dragonComp.SetLinkedPawn(pawn);
            }
        }

        // Add this part to fetch the appropriate bond stage dynamically based on the dragon
        public static void AddDragonBondDistanceStage(Pawn dragon)
        {
            HediffDef bondHediffDef = Verb_DragonBond.GetDragonBondHediffForDragon(dragon);

            // Assuming "dragon bond distance" is a valid label in the Hediff stages
            HediffStage dragonBondStageDistance = bondHediffDef?.stages?.FirstOrDefault(stage => stage.overrideLabel == "dragon bond distance");

            if (dragonBondStageDistance != null)
            {
                bondHediffDef.stages.Add(dragonBondStageDistance);
            }
            else
            {
                Log.Warning("dragonBondStageDistance not found for this dragon's bond.");
            }
        }

        // Remove the "Dragon bond distance" stage if it's present
        public static void RemoveDragonBondDistanceStage(Pawn dragon)
        {
            HediffDef bondHediffDef = Verb_DragonBond.GetDragonBondHediffForDragon(dragon);

            List<int> stagesToRemove = new List<int>();
            for (int i = 0; i < bondHediffDef.stages.Count; ++i)
            {
                HediffStage stage = bondHediffDef.stages[i];
                if (stage.overrideLabel == "dragon bond distance")
                {
                    stagesToRemove.Add(i);
                }
            }

            foreach (int stageIndex in stagesToRemove)
            {
                bondHediffDef.stages.RemoveAt(stageIndex);
            }
        }

        // Adjust the mood bonus for being close to the dragon
        public static void SetDragonBondProximityMoodBonus(float moodBonus)
        {
            dragonBondStageCloseMood.baseMoodEffect = moodBonus;
        }

        // Adjust the mood penalty for being distant from the dragon
        public static void SetDragonBondDistanceMoodPenalty(float moodPenalty)
        {
            dragonBondStageDistanceMood.baseMoodEffect = moodPenalty;
        }

        // Refresh the bond for all pawns that have a bond with a dragon (recalculates effects)
        public static void RefreshDragonBonds()
        {
            foreach (Pawn pawn in PawnsFinder.AllMapsAndWorld_Alive)
            {
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    // Check if the Hediff is a DragonBond by using the Verb_DragonBond logic
                    if (hediff.TryGetComp<HediffComp_DragonBondLink>() != null)
                    {
                        // Use the bonded dragon to find the correct Hediff definition
                        Pawn bondedDragon = hediff.TryGetComp<HediffComp_DragonBondLink>()?.linkedPawn;

                        if (bondedDragon != null)
                        {
                            HediffDef bondHediffDef = Verb_DragonBond.GetDragonBondHediffForDragon(bondedDragon);

                            // If the Hediff matches the bond Hediff, reset its severity
                            if (hediff.def == bondHediffDef)
                            {
                                // Reset severity to refresh bond state (close or far)
                                hediff.Severity = 0.1f;
                            }
                        }
                    }
                }
            }
        }
    }
}