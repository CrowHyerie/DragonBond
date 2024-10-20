using RimWorld;
using System.Linq;
using Verse.AI;
using Verse;
using Verse.Sound;

namespace Crows_DragonBond
{
    internal class TearDragonBondUtils
    {
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
            if (Prefs.DevMode)
            {
                Log.Message($"TearDragonBond: Called for Pawn: {pawn.NameShortColored}, Dragon: {dragon.NameShortColored}, Pawn Dead: {pawn.Dead}, Dragon Dead: {dragon.Dead}, isDeath: {isDeath}");
            }
            if (HasBondAlreadyBeenTorn(pawn, dragon))
            {
                Log.Warning("TearDragonBond: Bond has already been torn. Exiting to prevent duplicate handling.");
                return;
            }

            if (dragon.Dead && !pawn.Dead)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"TearDragonBond: Handling dragon death for {dragon.NameShortColored}. Human pawn is still alive.");
                }
                HandleDragonDeath(pawn, dragon);
                MarkBondAsTorn(pawn, dragon);
                return;
            }

            if (pawn.Dead && !dragon.Dead)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"TearDragonBond: Handling human death for {pawn.NameShortColored}. Dragon is still alive.");
                }
                HandleHumanDeath(pawn, dragon);
                MarkBondAsTorn(pawn, dragon);
                return;
            }

            if (pawn.Dead && dragon.Dead)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"TearDragonBond: Handling both deaths for {pawn.NameShortColored} (human) and {dragon.NameShortColored} (dragon).");
                }
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
            if (Prefs.DevMode)
            {
                Log.Message($"Marking bond as torn between {pawn.NameShortColored} and {dragon.NameShortColored}.");
            }
            // This could involve setting a custom flag or using a state-tracking dictionary if necessary
        }

        public static void HandleDragonDeath(Pawn pawn, Pawn dragon)
        {
            if (Prefs.DevMode)
            {
                Log.Message($"TearDragonBond: Dragon {dragon.NameShortColored} has died. Removing bond and handling human's reaction.");
            }
            // Remove bond from the dragon's corpse
            RemoveBondHediffFromCorpse(dragon);

            // Apply the catatonic breakdown to the human pawn due to the bond tear
            ApplyCatatonicBreakdownToPawn(pawn);

            // Set the human's mood to zero
            SetPawnMoodToZero(pawn);

            // Remove bond from the living human only if it hasn't been removed yet
            RemoveBondHediffFromLiving(pawn);

            // Remove the Draconic Regeneration gene from the rider pawn
            RemoveDraconicRegenerationGene(pawn);

        }

        public static void HandleHumanDeath(Pawn pawn, Pawn dragon)
        {
            if (Prefs.DevMode)
            {
                Log.Message($"TearDragonBond: Human pawn {pawn.NameShortColored} has died. Removing bond and handling dragon's reaction.");
            }
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

        public static void RemoveDraconicRegenerationGene(Pawn pawn)
        {
            if (pawn.genes != null)
            {
                // Find the Draconic Regeneration gene by defName
                Gene draconicGene = pawn.genes.GetGene(DefDatabase<GeneDef>.GetNamed("Crows_DraconicRegenerationGene", false));

                if (draconicGene != null)
                {
                    if (Prefs.DevMode)
                    {
                        Log.Message($"Removing Draconic Regeneration gene from {pawn.NameShortColored}.");
                    }
                    pawn.genes.RemoveGene(draconicGene);
                }
            }
        }

        // Set Mood to 0
        private static void SetPawnMoodToZero(Pawn pawn)
        {
            if (pawn.needs?.mood != null)
            {
                // Set the mood need level to zero
                pawn.needs.mood.CurLevel = 0f;
                if (Prefs.DevMode)
                {
                    Log.Message($"SetPawnMoodToZero: Pawn {pawn.NameShortColored}'s mood has been set to zero due to bond being torn.");
                }
            }
            else
            {
                if (Prefs.DevMode)
                {
                    Log.Warning($"SetPawnMoodToZero: Pawn {pawn.NameShortColored} does not have a mood need. No change applied.");
                }
            }
        }

        // Force an immediate mental break on the pawn
        private static void ApplyCatatonicBreakdownToPawn(Pawn pawn)
        {
            if (pawn != null && !pawn.Dead && pawn.needs != null && pawn.needs.mood != null)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"Applying catatonic breakdown to {pawn.NameShortColored} due to bond tear.");
                }
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

            // Guard clause to ensure the dragon is on a map before attempting exit logic
            if (dragon.Map == null)
            {
                Log.Warning($"HandleDragonLeaveFaction: {dragon.NameShortColored} is not on a map. Skipping exit logic.");
                return;
            }

            // Guard clause to ensure the dragon is not already in the intended final state
            if (dragon.mindState.mentalStateHandler.CurStateDef == MentalStateDefOf.ManhunterPermanent ||
                dragon.mindState.duty?.def == DutyDefOf.ExitMapBestAndDefendSelf)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"HandleDragonLeaveFaction: {dragon.NameShortColored} is already a manhunter or exiting the map. Aborting further processing.");
                }
                return;
            }
            if (Prefs.DevMode)
            {
                Log.Message($"{dragon.NameShortColored} is leaving the faction due to the death of their bonded pawn.");
            }

            // Remove the dragon from the faction and set it as wild
            dragon.SetFaction(null);  // Set faction to null, making the dragon wild

            // Decide whether the dragon becomes a Manhunter or leaves the map
            float chance = Rand.Value;  // Generates a random float between 0 and 1

            if (chance <= 0.2f)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"{dragon.NameShortColored} has become a Manhunter after losing their bond.");
                }
                dragon.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
                Messages.Message("CrowsDragonBond.DragonManhunterGrief".Translate().Formatted("", dragon.NameShortColored), MessageTypeDefOf.NegativeEvent);
                SoundDef.Named("Dragon_Angry").PlayOneShot(dragon); // Play a dragon sound

                // Skip exit map logic if the dragon becomes a manhunter
                return;
            }

            IntVec3 exitPoint;
            if (RCellFinder.TryFindBestExitSpot(dragon, out exitPoint, TraverseMode.ByPawn))
            {
                Job job = new Job(JobDefOf.Goto, exitPoint) { exitMapOnArrival = true };
                dragon.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
                Messages.Message("CrowsDragonBond.DragonLeaveGrief".Translate().Formatted("", dragon.NameShortColored), MessageTypeDefOf.NegativeEvent);
                SoundDef.Named("Dragon_Call").PlayOneShot(dragon); // Play a dragon sound
                if (Prefs.DevMode)
                {
                    Log.Message($"{dragon.NameShortColored} is leaving the map after losing their bond.");
                }
            }
            else
            {
                Log.Warning($"HandleDragonLeaveFaction: Could not find a suitable exit spot for {dragon.NameShortColored}.");
            }
        }
    }
}
