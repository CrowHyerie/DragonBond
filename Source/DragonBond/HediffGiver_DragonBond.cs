using RimWorld;
using System.Collections.Generic;
using Verse;

namespace DragonBond
{
    // Custom HediffGiver to apply DragonBond
    public class HediffGiver_DragonBond : HediffGiver
    {
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            // Check if the pawn is a valid dragon and doesn't already have the bond
            if (pawn.RaceProps.Animal && pawn.def.defName == "BaseDragon" && !pawn.health.hediffSet.HasHediff(HediffDef.Named("Crows_DragonBondHediff")))
            {
                // Ensure the dragon is alive before applying the bond
                if (!pawn.Dead)
                {
                    // Apply the dragon bond hediff
                    Hediff hediff = HediffMaker.MakeHediff(HediffDef.Named("Crows_DragonBondHediff"), pawn);
                    pawn.health.AddHediff(hediff);
                    Log.Message($"Applied DragonBond hediff to {pawn.NameShortColored}.");
                }
                else
                {
                    Log.Message($"Attempted to apply DragonBond to {pawn.NameShortColored}, but the dragon is dead.");
                }
            }
        }
        public static class DragonBondTracker
        {
            // Tracks bond information (pawn-to-dragon bond map)
            private static Dictionary<Pawn, Pawn> deadPawnBondMap = new Dictionary<Pawn, Pawn>();

            // Add bond to the tracker when either pawn or dragon dies
            public static void TrackBond(Pawn pawn, Pawn dragon)
            {
                if (!deadPawnBondMap.ContainsKey(pawn))
                {
                    deadPawnBondMap[pawn] = dragon;
                    Log.Message($"Tracking bond between {pawn.NameShortColored} and {dragon.NameShortColored}.");
                }
            }

            // Remove bond from tracker (e.g., when restored upon resurrection)
            public static void RemoveBondFromTracker(Pawn pawn)
            {
                if (deadPawnBondMap.ContainsKey(pawn))
                {
                    Log.Message($"Restoring bond for {pawn.NameShortColored} from the tracker.");
                    deadPawnBondMap.Remove(pawn);
                }
            }

            // Get bonded pawn from the tracker (if exists)
            public static Pawn GetBondedPawn(Pawn pawn)
            {
                if (deadPawnBondMap.ContainsKey(pawn))
                {
                    return deadPawnBondMap[pawn];
                }
                return null;
            }
        }


    }
}
