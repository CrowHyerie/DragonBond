using RimWorld;
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
                // Apply the dragon bond hediff
                Hediff hediff = HediffMaker.MakeHediff(HediffDef.Named("Crows_DragonBondHediff"), pawn);
                pawn.health.AddHediff(hediff);
                Log.Message($"Applied DragonBond hediff to {pawn.NameShortColored}.");
            }
        }
    }
}
