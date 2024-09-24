using Crows_DragonBond;
using RimWorld;
using System;
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

        // Ensure that dragonBondProximityDef and Crows_DragonBondHediff are not null before using them
        private static ThoughtStage dragonBondStageCloseMood = dragonBondProximityDef?.stages?.FirstOrDefault(stage => stage.label == "dragon bond");
        private static ThoughtStage dragonBondStageDistanceMood = dragonBondProximityDef?.stages?.FirstOrDefault(stage => stage.label == "dragon bond distance");

        private static HediffStage dragonBondStageClose = Crows_DragonBondHediff?.stages?.FirstOrDefault(stage => stage.overrideLabel == "dragon bond close");
        private static HediffStage dragonBondStageDistance = Crows_DragonBondHediff?.stages?.FirstOrDefault(stage => stage.overrideLabel == "dragon bond distance");

        // Capacity modifiers for consciousness, ensure we only reference them if their stages exist
        private static PawnCapacityModifier dragonBondConsciousnessClose = dragonBondStageClose?.capMods?.FirstOrDefault(capMod => capMod.capacity?.defName == "Consciousness");
        private static PawnCapacityModifier dragonBondConsciousnessDistance = dragonBondStageDistance?.capMods?.FirstOrDefault(capMod => capMod.capacity?.defName == "Consciousness");

        // Create a dragon bond between a pawn and their dragon
        // Bond tracker for resurrected pawns
        private static Dictionary<Pawn, Pawn> deadPawnBondMap = new Dictionary<Pawn, Pawn>();

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
                Log.Message($"{pawn.NameShortColored} already has a bond with {dragon.NameShortColored}.");
                return;
            }

            pawn.relations.AddDirectRelation(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), dragon);
            dragon.relations.AddDirectRelation(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), pawn);

            Hediff pawnBondHediff = HediffMaker.MakeHediff(HediffDef.Named("Crows_DragonBondHediff"), pawn);
            pawn.health.AddHediff(pawnBondHediff);

            Hediff dragonBondHediff = HediffMaker.MakeHediff(HediffDef.Named("Crows_DragonBondHediff"), dragon);
            dragon.health.AddHediff(dragonBondHediff);

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

        public static void TearDragonBond(Pawn pawn, Pawn dragon, float moodDebuff = -20f, int moodDebuffDurationDays = 10)
        {
            if (pawn == null || dragon == null)
            {
                Log.Error("TearDragonBond called with a null pawn or dragon.");
                return;
            }

            Log.Message($"Tearing bond between {pawn.NameShortColored} and {dragon.NameShortColored}");

            // Ensure we tear the bond relation from both pawns
            if (pawn.relations != null)
            {
                pawn.relations.RemoveDirectRelation(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), dragon);
            }
            if (dragon.relations != null)
            {
                dragon.relations.RemoveDirectRelation(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), pawn);
            }

            // Explicitly remove the bond Hediff from both living and dead pawns
            RemoveDragonBondHediff(pawn, dragon);
            RemoveDragonBondHediff(dragon, pawn);

            // If the pawn is not dead, apply mood debuff and force mental break
            if (!pawn.Dead && pawn.needs != null && pawn.needs.mood != null)
            {
                ThoughtDef bondTornDef = ThoughtDef.Named("Crows_DragonBondTorn");
                pawn.needs.mood.thoughts.memories.TryGainMemory(bondTornDef, dragon);
                ForcePawnMentalBreak(pawn);
                SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera();
            }

            // Ensure the dead pawn also has their Hediff removed
            if (pawn.Dead)
            {
                RemoveDragonBondHediff(pawn, dragon);
            }
            if (dragon.Dead)
            {
                RemoveDragonBondHediff(dragon, pawn);
            }
        }


        // Utility method to remove the DragonBond Hediff from a pawn
        public static void RemoveDragonBondHediff(Pawn pawn, Pawn bondedPawn)
        {
            if (pawn == null || pawn.health == null)
            {
                Log.Error("Attempted to remove Dragon Bond Hediff from a null or invalid pawn.");
                return;
            }

            // Get the specific Hediff for the dragon type (if the pawn is a dragon)
            HediffDef pawnDragonBondHediff = Verb_DragonBond.GetDragonBondHediffForDragon(pawn);

            // Handle alive pawns first
            if (!pawn.Dead)
            {
                Hediff dragonBondHediff = pawn.health.hediffSet.GetFirstHediffOfDef(pawnDragonBondHediff);
                if (dragonBondHediff != null)
                {
                    pawn.health.RemoveHediff(dragonBondHediff);
                    Log.Message($"Removed {pawnDragonBondHediff.defName} Hediff from {pawn.NameShortColored} (alive).");
                }
                else
                {
                    Log.Warning($"Could not find {pawnDragonBondHediff.defName} Hediff on {pawn.NameShortColored} to remove.");
                }
            }
            else // Handle dead pawns (corpses)
            {
                Log.Message($"Processing removal of {pawnDragonBondHediff.defName} Hediff for dead pawn {pawn.NameShortColored}.");

                if (pawn.Corpse != null)
                {
                    Pawn corpseInnerPawn = pawn.Corpse.InnerPawn;

                    // Remove bond from corpse's InnerPawn
                    Hediff corpseBondHediff = corpseInnerPawn.health.hediffSet.GetFirstHediffOfDef(pawnDragonBondHediff);
                    if (corpseBondHediff != null)
                    {
                        corpseInnerPawn.health.RemoveHediff(corpseBondHediff);
                        Log.Message($"Removed {pawnDragonBondHediff.defName} Hediff from the corpse of {corpseInnerPawn.NameShortColored}.");
                    }
                    else
                    {
                        Log.Warning($"Could not find {pawnDragonBondHediff.defName} Hediff on the corpse of {corpseInnerPawn.NameShortColored} to remove.");
                    }
                }
            }

            // Handle bondedPawn (likely the rider) separately
            if (bondedPawn != null && bondedPawn.health != null)
            {
                HediffDef bondedPawnDragonBondHediff = Verb_DragonBond.GetDragonBondHediffForDragon(bondedPawn); // Correct Hediff for bonded pawn (if also a dragon)

                Hediff bondedPawnHediff = bondedPawn.health.hediffSet.GetFirstHediffOfDef(bondedPawnDragonBondHediff);
                if (bondedPawnHediff != null)
                {
                    bondedPawn.health.RemoveHediff(bondedPawnHediff);
                    Log.Message($"Removed {bondedPawnDragonBondHediff.defName} Hediff from bonded pawn {bondedPawn.NameShortColored}.");
                }
                else
                {
                    Log.Warning($"Could not find {bondedPawnDragonBondHediff.defName} Hediff on bonded pawn {bondedPawn.NameShortColored} to remove.");
                }
            }
        }

        public static void HandlePawnDeath(Pawn pawn)
        {
            Hediff dragonBondHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("Crows_DragonBondHediff"));
            if (dragonBondHediff != null)
            {
                Pawn bondedDragon = dragonBondHediff.TryGetComp<HediffComp_DragonBondLink>()?.linkedPawn;

                if (bondedDragon != null)
                {
                    Log.Message($"Pawn {pawn.NameShortColored} has died. Severing bond with {bondedDragon.NameShortColored}.");

                    // Add both the dead pawn and bonded dragon to the deadPawnBondMap for resurrection tracking
                    deadPawnBondMap[pawn] = bondedDragon;
                    deadPawnBondMap[bondedDragon] = pawn;

                    TearDragonBond(pawn, bondedDragon);

                    // Also, ensure the bond is removed from the corpse of the dead pawn
                    if (pawn.Dead)
                    {
                        RemoveDragonBondHediff(pawn, bondedDragon);
                    }
                }
            }
        }

        // Method to restore bond if resurrected
        public static void RestoreBondIfResurrected(Pawn resurrectedPawn)
        {
            if (deadPawnBondMap.TryGetValue(resurrectedPawn, out Pawn bondedPawn))
            {
                Log.Message($"Restoring bond between resurrected pawn {resurrectedPawn.NameShortColored} and {bondedPawn.NameShortColored}.");
                TryCastShot(resurrectedPawn, bondedPawn);
                deadPawnBondMap.Remove(resurrectedPawn);  // Remove from tracking after restoring
            }
        }

        private static void TryCastShot(Pawn resurrectedPawn, Pawn bondedPawn)
        {
            throw new NotImplementedException();
        }

        // Force an immediate mental break on the pawn
        private static void ForcePawnMentalBreak(Pawn pawn)
        {
            if (pawn.mindState.mentalStateHandler != null && !pawn.Dead)
            {
                // Apply the Catatonic breakdown Hediff instead of a mental state
                Hediff catatonicBreakdown = HediffMaker.MakeHediff(HediffDefOf.CatatonicBreakdown, pawn);
                pawn.health.AddHediff(catatonicBreakdown);
                Log.Message($"Pawn {pawn.NameShortColored} has gone catatonic due to the torn bond.");
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
        public Pawn linkedPawn; // The dragon or rider linked to this pawn
        private Map lastPawnMap; // Store the last known map for the pawn
        private Map lastDragonMap; // Store the last known map for the dragon

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref linkedPawn, "linkedPawn");
            Scribe_References.Look(ref lastPawnMap, "lastPawnMap");
            Scribe_References.Look(ref lastDragonMap, "lastDragonMap");
        }

        public void SetLinkedPawn(Pawn pawn)
        {
            linkedPawn = pawn;
            lastDragonMap = pawn?.Map;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (linkedPawn == null || Pawn.Dead || linkedPawn.Dead)
            {
                TearBond(); // Tear the bond if either the Pawn or Dragon is dead
                return;
            }

            // Check if either the Pawn or the Dragon has changed maps
            if (Pawn.Map != lastPawnMap || linkedPawn.Map != lastDragonMap)
            {
                // Pawn or Dragon has left or entered the map
                UpdateBondStatus();
                lastPawnMap = Pawn.Map; // Update the last known map for the Pawn
                lastDragonMap = linkedPawn.Map; // Update the last known map for the Dragon
            }
        }

        private void UpdateBondStatus()
        {
            if (linkedPawn.Map == Pawn.Map)
            {
                // Dragon and Pawn are on the same map, apply close bond
                ApplyCloseBond();
                Log.Message($"Dragon {linkedPawn.NameShortColored} and pawn {Pawn.NameShortColored} are on the same map. Applying close bond.");
            }
            else
            {
                // Dragon and Pawn are on different maps, apply distance bond
                ApplyDistanceBond();
                Log.Message($"Dragon {linkedPawn.NameShortColored} and pawn {Pawn.NameShortColored} are on different maps. Applying distance bond.");
            }
        }

        private void TearBond()
        {
            if (linkedPawn != null)
            {
                Log.Message($"Tearing bond between {Pawn.NameShortColored} and {linkedPawn.NameShortColored} due to death.");
                DragonBondUtils.TearDragonBond(Pawn, linkedPawn);
                linkedPawn = null; // Remove reference after tearing bond
            }
        }

        private void ApplyCloseBond()
        {
            if (linkedPawn.Spawned && linkedPawn.Map == this.Pawn.Map)
            
                Log.Message($"Dragon is nearby for pawn {this.Pawn.NameShortColored}. Applying close bond.");
                this.parent.Severity = 0.5f;  // Set severity for close bond.
            
        }

        private void ApplyDistanceBond()
        {
            
                Log.Message($"Dragon is far or off-map for pawn {this.Pawn.NameShortColored}. Applying distance debuff.");
                this.parent.Severity = 1.0f;  // Set severity for distant bond.
            
        }           
            
        

        // Called when the Hediff is removed, which often happens when the pawn dies
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            // This will be called when the Hediff is removed from the pawn
            if (linkedPawn != null && !linkedPawn.Dead)
            {
                Log.Message($"Bonded pawn {this.Pawn.NameShortColored} has had the bond removed, clearing bond on {linkedPawn.NameShortColored}.");
                DragonBondUtils.RemoveDragonBondHediff(linkedPawn, this.Pawn);
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
