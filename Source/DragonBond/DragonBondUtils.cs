using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DragonBond
{
    internal class DragonBondUtils
    {
        // Color for when the bond is torn, using a reddish hue to indicate the severed bond.
        public static Color DragonBondTornLabelColor => new Color(0.9f, 0.3f, 0.3f);

        // Defining constants for the custom thought and hediff defs in your mod
        private const string THOUGHT_DRAGON_BOND_PROXIMITY_DEF_NAME = "Crows_DragonBondProximity";
        private const string HEDIFF_DRAGON_BOND_DEF_NAME = "Crows_DragonBondHediff";

        // Thought and Hediff Def references for the bond
        private static ThoughtDef dragonBondProximityDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(THOUGHT_DRAGON_BOND_PROXIMITY_DEF_NAME);
        private static HediffDef Crows_DragonBondHediff = DefDatabase<HediffDef>.GetNamedSilentFail(HEDIFF_DRAGON_BOND_DEF_NAME);

        // Stages for proximity (close and distant)
        private static ThoughtStage dragonBondStageCloseMood = dragonBondProximityDef.stages.First(stage => stage.label == "dragon bond");
        private static ThoughtStage dragonBondStageDistanceMood = dragonBondProximityDef.stages.First(stage => stage.label == "dragon bond distance");

        // Hediff stages for the bond and consciousness modification
        private static HediffStage dragonBondStageClose = Crows_DragonBondHediff.stages.First(stage => stage.overrideLabel == "dragon bond close");
        private static HediffStage dragonBondStageDistance = Crows_DragonBondHediff.stages.First(stage => stage.overrideLabel == "dragon bond distance");

        private static PawnCapacityModifier dragonBondConsciousnessClose = dragonBondStageClose.capMods.First(capMod => capMod.capacity.defName == "Consciousness");
        private static PawnCapacityModifier dragonBondConsciousnessDistance = dragonBondStageDistance.capMods.First(capMod => capMod.capacity.defName == "Consciousness");

        // Create a dragon bond between a pawn and their dragon
        public static void CreateDragonBond(Pawn pawn, Pawn dragon)
        {
            // Ensure the pawn and dragon are valid
            if (pawn == null || dragon == null || pawn.relations == null || dragon.relations == null)
            {
                Log.Error("Failed to create bond: pawn or dragon or their relations tracker is null.");
                return;
            }

            // Check if a bond already exists
            if (pawn.relations.DirectRelationExists(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), dragon))
            {
                Log.Message($"{pawn.NameShortColored} already has a bond with {dragon.NameShortColored}.");
                return;
            }

            // Create the bond relation between the pawn and the dragon
            pawn.relations.AddDirectRelation(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), dragon);
            dragon.relations.AddDirectRelation(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), pawn);

            // Apply the dragon bond hediff to the pawn
            Hediff pawnBondHediff = HediffMaker.MakeHediff(HediffDef.Named("Crows_DragonBondHediff"), pawn);
            pawn.health.AddHediff(pawnBondHediff);

            // Apply the dragon bond hediff to the dragon
            Hediff dragonBondHediff = HediffMaker.MakeHediff(HediffDef.Named("Crows_DragonBondHediff"), dragon);
            dragon.health.AddHediff(dragonBondHediff);

            // Link the pawn's bond to the dragon
            var pawnComp = pawnBondHediff.TryGetComp<HediffComp_DragonBondLink>();
            if (pawnComp != null)
            {
                pawnComp.SetLinkedPawn(dragon);
            }

            // Link the dragon's bond back to the pawn
            var dragonComp = dragonBondHediff.TryGetComp<HediffComp_DragonBondLink>();
            if (dragonComp != null)
            {
                dragonComp.SetLinkedPawn(pawn);
            }
        }


        // Add the "Dragon bond distance" stage if it's missing
        public static void AddDragonBondDistanceStage()
        {
            bool foundStage = false;
            foreach (HediffStage stage in Crows_DragonBondHediff.stages)
            {
                if (stage.overrideLabel == "dragon bond distance")
                {
                    foundStage = true;
                }
            }

            if (!foundStage)
            {
                Crows_DragonBondHediff.stages.Add(dragonBondStageDistance);
            }
        }

        // Remove the "Dragon bond distance" stage if it's present
        public static void RemoveDragonBondDistanceStage()
        {
            List<int> stagesToRemove = new List<int>();
            for (int i = 0; i < Crows_DragonBondHediff.stages.Count; ++i)
            {
                HediffStage stage = Crows_DragonBondHediff.stages[i];
                if (stage.overrideLabel == "dragon bond distance")
                {
                    stagesToRemove.Add(i);
                }
            }

            foreach (int stageIndex in stagesToRemove)
            {
                Crows_DragonBondHediff.stages.RemoveAt(stageIndex);
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

        // Adjust the consciousness effect when close to the dragon
        public static void SetDragonBondConsciousnessBonus(float consciousnessBonus)
        {
            dragonBondConsciousnessClose.offset = consciousnessBonus;
        }

        // Adjust the consciousness effect when distant from the dragon
        public static void SetDragonBondConsciousnessPenalty(float consciousnessPenalty)
        {
            dragonBondConsciousnessDistance.offset = consciousnessPenalty;
        }

        // Refresh the bond for all pawns that have a bond with a dragon (recalculates effects)
        public static void RefreshDragonBonds()
        {
            foreach (Pawn pawn in PawnsFinder.AllMapsAndWorld_Alive)
            {
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff.def == HediffDef.Named("Crows_DragonBondHediff"))
                    {
                        // Reset severity to 0.1 to refresh bond state (close or far)
                        hediff.Severity = 0.1f;
                    }
                }
            }
        }

        // Add a "refresh stage" to recalculate bond status dynamically
        private static void AddDragonBondRefreshStage()
        {
            dragonBondStageClose.minSeverity = 0.2f;

            HediffStage dragonBondRefreshStage = new HediffStage
            {
                overrideLabel = "dragon bond refresh",
                minSeverity = 0.1f, // Temporarily set severity for refresh
                painFactor = dragonBondStageClose.painFactor,
                statOffsets = new List<StatModifier>(dragonBondStageClose.statOffsets),
                capMods = new List<PawnCapacityModifier>(dragonBondStageClose.capMods)
            };

            Crows_DragonBondHediff.stages.Add(dragonBondRefreshStage);

            // Sort the stages by severity to avoid errors
            Crows_DragonBondHediff.stages.Sort((HediffStage stage1, HediffStage stage2) =>
            {
                return stage1.minSeverity.CompareTo(stage2.minSeverity);
            });
        }

        // Method to sever a bond between a pawn and a dragon, triggering mood debuffs and possibly mental breaks
        public static void TearDragonBond(Pawn pawn, Pawn dragon, int mentalBreakChance = 10, float moodDebuff = -20f, int moodDebuffDurationDays = 10)
        {
            // Remove direct relations between the pawn and the dragon
            pawn.relations.RemoveDirectRelation(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), dragon);

            // Remove the dragon bond hediff from both the pawn and the dragon
            RemoveDragonBondHediff(pawn, dragon);
            RemoveDragonBondHediff(dragon, pawn);

            // Apply the torn bond mood debuff
            ThoughtDef bondTornDef = ThoughtDef.Named("Crows_DragonBondTorn");
            pawn.needs.mood.thoughts.memories.TryGainMemory(bondTornDef, dragon);

            // Roll for mental break if the bond is strong and breaks
            if (Rand.Chance(mentalBreakChance / 100f))
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
            }

            // Trigger a negative sound event (ritual sound on bond tearing)
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera();
        }

        // Utility method to remove the DragonBond Hediff from a pawn
        private static void RemoveDragonBondHediff(Pawn pawn, Pawn bondedPawn)
        {
            Hediff dragonBondHediff = pawn.health.hediffSet.GetFirstHediffOfDef(Crows_DragonBondHediff);
            if (dragonBondHediff != null)
            {
                pawn.health.RemoveHediff(dragonBondHediff);
            }
        }
    }

    // Custom HediffComp to manage the linked dragon and bond behavior
    public class HediffCompProperties_DragonBondLink : HediffCompProperties
    {
        public PawnRelationDef targetRelation;

        public HediffCompProperties_DragonBondLink()
        {
            this.compClass = typeof(HediffComp_DragonBondLink);
        }
    }

    public class HediffComp_DragonBondLink : HediffComp
    {
        public Pawn linkedPawn; // The dragon

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref linkedPawn, "linkedPawn");
        }

        // Set the linked pawn manually (Dragon)
        public void SetLinkedPawn(Pawn pawn)
        {
            linkedPawn = pawn;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // Debugging output
            Log.Message($"Checking bond for pawn {this.Pawn.NameShortColored} and linked dragon {linkedPawn?.NameShortColored ?? "null"}.");

            if (linkedPawn == null)
            {
                Log.Warning($"No dragon is linked to {this.Pawn.NameShortColored}. Bonding failed.");
                return;
            }

            if (linkedPawn.Dead)
            {
                Log.Message($"Dragon is dead for pawn {this.Pawn.NameShortColored}. Applying death debuff.");
                this.parent.Severity = 1.0f;
            }
            else if (linkedPawn.Spawned && linkedPawn.Map == this.Pawn.Map)
            {
                Log.Message($"Dragon is nearby for pawn {this.Pawn.NameShortColored}. Applying close bond.");
                this.parent.Severity = 0.5f;
            }
            else
            {
                Log.Message($"Dragon is far or off-map for pawn {this.Pawn.NameShortColored}. Applying distance debuff.");
                this.parent.Severity = 1.0f;
            }
        }
    }



    public class ThoughtWorker_DragonBondedDied : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            // Check if the pawn has the dragon bond hediff
            Hediff dragonBondHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Crows_DragonBondHediff"));

            if (dragonBondHediff != null)
            {
                // Get the dragon the pawn is bonded to
                Pawn bondedDragon = dragonBondHediff.TryGetComp<HediffComp_DragonBondLink>()?.linkedPawn;

                // Check if the bonded dragon is null or dead
                if (bondedDragon == null || bondedDragon.Dead)
                {
                    return ThoughtState.ActiveAtStage(0); // Trigger the thought if the dragon is dead
                }
            }

            // Return inactive if the dragon is alive or off-map but not dead
            return ThoughtState.Inactive;
        }
    }
}
