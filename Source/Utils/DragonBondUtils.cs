using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;


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

        public static void TearDragonBond(Pawn pawn, Pawn dragon, bool isDeath = false)
        {
            if (pawn == null || dragon == null)
            {
                Log.Error("TearDragonBond: Called with a null pawn or dragon.");
                return;
            }

            // Ensure that the parameters are not swapped accidentally
            if (!pawn.RaceProps.Humanlike && dragon.RaceProps.Humanlike)
            {
                // Swap to ensure pawn is always the human, and dragon is the animal
                Pawn temp = pawn;
                pawn = dragon;
                dragon = temp;
                Log.Warning("TearDragonBond: Swapped pawn and dragon parameters to correct roles.");
            }

            // Determine if the bond should be torn due to death (either human or dragon)
            isDeath = isDeath || pawn.Dead || dragon.Dead;

            if (!isDeath)
            {
                Log.Warning("TearDragonBond: Called without a death condition.");
                return;
            }

            Log.Message($"TearDragonBond: Called for Pawn: {pawn.NameShortColored}, Dragon: {dragon.NameShortColored}, Pawn Dead: {pawn.Dead}, Dragon Dead: {dragon.Dead}, isDeath: {isDeath}");

            if (HasBondAlreadyBeenTorn(pawn, dragon))
            {
                Log.Warning("TearDragonBond: Bond has already been torn. Exiting to prevent duplicate handling.");
                return;
            }

            if (dragon.Dead && !pawn.Dead)
            {
                Log.Message($"TearDragonBond: Handling dragon death for {dragon.NameShortColored}. Human pawn is still alive.");
                HandleDragonDeath(pawn, dragon);
                MarkBondAsTorn(pawn, dragon);
                return;
            }

            if (pawn.Dead && !dragon.Dead)
            {
                Log.Message($"TearDragonBond: Handling human death for {pawn.NameShortColored}. Dragon is still alive.");
                HandleHumanDeath(pawn, dragon);
                MarkBondAsTorn(pawn, dragon);
                return;
            }

            if (pawn.Dead && dragon.Dead)
            {
                Log.Message($"TearDragonBond: Handling both deaths for {pawn.NameShortColored} (human) and {dragon.NameShortColored} (dragon).");
                HandleHumanDeath(pawn, dragon);
                HandleDragonDeath(pawn, dragon);
                MarkBondAsTorn(pawn, dragon);
            }
        }

        // Check if bond has already been torn to prevent re-triggering
        private static bool HasBondAlreadyBeenTorn(Pawn pawn, Pawn dragon)
        {
            // You can implement a more robust solution using custom fields, a dictionary, or the pawn’s memory
            return pawn.health.hediffSet.hediffs.All(h => h.TryGetComp<HediffComp_DragonBondLink>() == null) &&
                   dragon.health.hediffSet.hediffs.All(h => h.TryGetComp<HediffComp_DragonBondLink>() == null);
        }

        // Mark bond as torn to prevent re-triggering
        private static void MarkBondAsTorn(Pawn pawn, Pawn dragon)
        {
            Log.Message($"Marking bond as torn between {pawn.NameShortColored} and {dragon.NameShortColored}.");
            // This could involve setting a custom flag or using a state-tracking dictionary if necessary
        }

        public static void HandleDragonDeath(Pawn pawn, Pawn dragon)
        {
            Log.Message($"TearDragonBond: Dragon {dragon.NameShortColored} has died. Removing bond and handling human's reaction.");

            // Remove bond from the dragon's corpse
            RemoveBondHediffFromCorpse(dragon);

            // Apply the catatonic breakdown to the human pawn due to the bond tear
            ApplyCatatonicBreakdownToPawn(pawn);

            // Set the human's mood to zero
            SetPawnMoodToZero(pawn);

            // Remove bond from the living human only if it hasn't been removed yet
            RemoveBondHediffFromLiving(pawn);
        }

        public static void HandleHumanDeath(Pawn pawn, Pawn dragon)
        {
            Log.Message($"TearDragonBond: Human pawn {pawn.NameShortColored} has died. Removing bond and handling dragon's reaction.");

            // Remove bond from the human's corpse
            RemoveBondHediffFromCorpse(pawn);

            // Handle dragon leaving the faction
            HandleDragonLeaveFaction(dragon);

            // Remove bond from the living dragon only if it hasn't been removed yet
            RemoveBondHediffFromLiving(dragon);
        }

        private static void RemoveBondHediffFromLiving(Pawn pawn)
        {
            if (pawn == null || pawn.health == null)
            {
                Log.Warning("RemoveBondHediffFromLiving: called with a null or invalid pawn.");
                return;
            }

            // Get the correct HediffComp_DragonBondLink for the living pawn's bond
            Hediff bondHediff = pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.TryGetComp<HediffComp_DragonBondLink>() != null);

            if (bondHediff != null)
            {
                pawn.health.RemoveHediff(bondHediff);

            }
            else
            {

            }
        }

        private static void RemoveBondHediffFromCorpse(Pawn corpsePawn)
        {
            if (corpsePawn == null || corpsePawn.Corpse == null)
            {
                Log.Warning("RemoveBondHediffFromCorpse: called with a null or invalid corpse.");
                return;
            }

            Pawn corpseInnerPawn = corpsePawn.Corpse.InnerPawn;

            // Log message to indicate human or dragon removal
            string pawnType = corpseInnerPawn.RaceProps.Humanlike ? "human" : "dragon";

            // Use the Hediff system to access the bond linked HediffComp_DragonBondLink
            Hediff bondHediff = corpseInnerPawn.health.hediffSet.hediffs.FirstOrDefault(h => h.TryGetComp<HediffComp_DragonBondLink>() != null);

            if (bondHediff != null)
            {
                corpseInnerPawn.health.RemoveHediff(bondHediff);
            }
            else
            {
            }
        }

        // Set Mood to 0
        private static void SetPawnMoodToZero(Pawn pawn)
        {
            if (pawn.needs?.mood != null)
            {
                // Set the mood need level to zero
                pawn.needs.mood.CurLevel = 0f;
                Log.Message($"SetPawnMoodToZero: Pawn {pawn.NameShortColored}'s mood has been set to zero due to bond being torn.");
            }
            else
            {
                Log.Warning($"SetPawnMoodToZero: Pawn {pawn.NameShortColored} does not have a mood need. No change applied.");
            }
        }

        // Force an immediate mental break on the pawn
        private static void ApplyCatatonicBreakdownToPawn(Pawn pawn)
        {
            if (pawn != null && !pawn.Dead && pawn.needs != null && pawn.needs.mood != null)
            {
                Log.Message($"Applying catatonic breakdown to {pawn.NameShortColored} due to bond tear.");

                // Apply a catatonic breakdown to the human pawn
                Hediff catatonicBreakdown = HediffMaker.MakeHediff(HediffDefOf.CatatonicBreakdown, pawn);
                pawn.health.AddHediff(catatonicBreakdown);

                // Example: If the pawn goes catatonic due to the bond being torn
                if (pawn.health.hediffSet.HasHediff(HediffDefOf.CatatonicBreakdown))
                {
                    Messages.Message("CrowsDragonBond.PawnBondCatatonicBreak".Translate().Formatted(pawn.NameShortColored), MessageTypeDefOf.NegativeEvent);
                }
            }
        }
        private static void HandleDragonLeaveFaction(Pawn dragon)
        {
            if (dragon == null || dragon.Dead)
            {
                Log.Warning("HandleDragonLeaveFaction: Called with a null or dead dragon.");
                return;
            }

            // Guard clause to ensure the dragon is not already in the intended final state
            if (dragon.mindState.mentalStateHandler.CurStateDef == MentalStateDefOf.ManhunterPermanent || dragon.mindState.duty?.def == DutyDefOf.ExitMapBestAndDefendSelf)
            {
                Log.Message($"HandleDragonLeaveFaction: {dragon.NameShortColored} is already a manhunter or exiting the map. Aborting further processing.");
                return;
            }

            Log.Message($"{dragon.NameShortColored} is leaving the faction due to the death of their bonded pawn.");

            // Remove the dragon from the faction and set it as wild
            dragon.SetFaction(null);  // Set faction to null, making the dragon wild

            // Decide whether the dragon becomes a Manhunter or leaves the map
            float chance = Rand.Value;  // Generates a random float between 0 and 1

            if (chance <= 0.2f)
            {
                Log.Message($"{dragon.NameShortColored} has become a Manhunter after losing their bond.");
                dragon.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
                Messages.Message("CrowsDragonBond.DragonManhunterGrief".Translate().Formatted("", dragon.NameShortColored), MessageTypeDefOf.NegativeEvent);
                SoundDef.Named("Dragon_Angry").PlayOneShot(dragon); // Play a dragon sound

                // Skip exit map logic if the dragon becomes a manhunter
                return;
            }

            Log.Message($"{dragon.NameShortColored} is leaving the map after losing their bond.");

            IntVec3 exitPoint;
            if (RCellFinder.TryFindBestExitSpot(dragon, out exitPoint, TraverseMode.ByPawn))
            {
                Job job = new Job(JobDefOf.Goto, exitPoint) { exitMapOnArrival = true };
                dragon.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
                Messages.Message("CrowsDragonBond.DragonLeaveGrief".Translate().Formatted("", dragon.NameShortColored), MessageTypeDefOf.NegativeEvent);
                SoundDef.Named("Dragon_Call").PlayOneShot(dragon); // Play a dragon sound
            }
            else
            {
                Log.Warning($"HandleDragonLeaveFaction: Could not find a suitable exit spot for {dragon.NameShortColored}.");
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
        private bool bondTorn = false;  // Flag to track if the bond has been torn

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

            // Tear the bond if either the Pawn (human or dragon) or the linkedPawn (bonded) is dead
            if (linkedPawn == null || Pawn.Dead || linkedPawn.Dead)
            {
                TearBond();
                return;
            }

            // Check if either the Pawn or the Dragon has changed maps
            if (Pawn.Map != lastPawnMap || linkedPawn.Map != lastDragonMap)
            {
                UpdateBondStatus();  // Apply bond logic based on map distance
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
            Log.Message($"TearBond: Trying to tear bond between {Pawn.NameShortColored} and {linkedPawn?.NameShortColored ?? "null"}.");

            if (linkedPawn != null && !bondTorn)
            {
                bondTorn = true;
                DragonBondUtils.TearDragonBond(Pawn, linkedPawn, isDeath: true);
                linkedPawn = null;
            }
        }

        private void ApplyCloseBond()
        {
            if (linkedPawn.Spawned && linkedPawn.Map == this.Pawn.Map)

            this.parent.Severity = 0.5f;  // Set severity for close bond.

        }

        private void ApplyDistanceBond()
        {

            this.parent.Severity = 1.0f;  // Set severity for distant bond.

        }

        // Called when the Hediff is removed, which often happens when the pawn dies
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            if (linkedPawn != null && !bondTorn)  // Check to ensure bond is not already torn
            {
                bondTorn = true;
                DragonBondUtils.TearDragonBond(this.Pawn, linkedPawn);
            }
        }
    }
    public class ThoughtWorker_DragonBondedDied : ThoughtWorker
        {
            protected override ThoughtState CurrentStateInternal(Pawn pawn)
            {
                // Get the specific bond Hediff Def for this pawn's dragon
                HediffDef bondHediffDef = Verb_DragonBond.GetDragonBondHediffForDragon(pawn);

                // Use the bond Hediff Def to find the actual Hediff instance on the pawn
                Hediff dragonBondHediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(bondHediffDef);

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



