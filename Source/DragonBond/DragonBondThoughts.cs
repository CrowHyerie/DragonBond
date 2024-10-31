using Crows_DragonBond;
using RimWorld;
using System.Linq;
using Verse;

namespace Crows_DragonBond
{
    public class ThoughtWorker_DragonBondClose : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            // Get the bond component directly from the pawn's health set
            HediffComp_DragonBondLink bondComp = pawn.health?.hediffSet?.GetAllComps()
                .OfType<HediffComp_DragonBondLink>()
                .FirstOrDefault();

            if (bondComp == null)
            {
                return ThoughtState.Inactive;
            }

            // If the bondComp indicates that the dragon is close, activate the thought
            if (bondComp.isDragonClose)
            {
                return ThoughtState.ActiveAtStage(0); // Show the close bond thought
            }

            return ThoughtState.Inactive;
        }
    }

    public class ThoughtWorker_DragonBondDistant : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            // Get the bond component directly from the pawn's health set
            HediffComp_DragonBondLink bondComp = pawn.health?.hediffSet?.GetAllComps()
                .OfType<HediffComp_DragonBondLink>()
                .FirstOrDefault();

            if (bondComp == null)
            {
                return ThoughtState.Inactive;
            }

            // If the bondComp indicates that the dragon is distant, activate the thought
            if (bondComp.isDragonDistant)
            {
                return ThoughtState.ActiveAtStage(0); // Show the distant bond thought
            }

            return ThoughtState.Inactive;
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


        public static void HandleDragonSold(Pawn dragon)
        {
            if (dragon == null || dragon.Dead)
            {
                Log.Warning("HandleDragonSold: Called with a null or dead dragon.");
                return;
            }

            // Fetch the human pawn that is bonded to this dragon
            var bondedPawn = dragon.relations.GetFirstDirectRelationPawn(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"));

            if (bondedPawn == null || bondedPawn.Dead)
            {
                Log.Warning("HandleDragonSold: No bonded pawn found or bonded pawn is dead.");
                return;
            }

            // Assign the "Sold My Bonded Dragon" thought to the bonded pawn
            ThoughtDef soldDragonThought = DefDatabase<ThoughtDef>.GetNamed("Crows_SoldMyBondedDragon");
            bondedPawn.needs?.mood?.thoughts?.memories?.TryGainMemory(soldDragonThought, dragon);
            if (Prefs.DevMode)
            {
                Log.Message($"HandleDragonSold: {bondedPawn.NameShortColored} has received the thought of their dragon {dragon.NameShortColored} being sold.");
            }
        }
    }
}