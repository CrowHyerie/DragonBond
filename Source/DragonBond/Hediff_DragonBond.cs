using Verse;
using RimWorld;

namespace DragonBondMod
{
    // DefOf class for Hediff definitions
    [DefOf]
    public static class HediffDefOf_Crows
    {
        public static HediffDef Crows_DragonBondHediff;
        public static HediffDef Crows_DragonBondTorn;

        static HediffDefOf_Crows()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOf_Crows));
        }
    }

    // Hediff class for Dragon Bond
    public class Hediff_DragonBond : HediffWithComps
    {
        // Access the bonded dragon using HediffComp_DragonBond
        public Pawn BondedDragon => this.TryGetComp<HediffComp_DragonBond>()?.BondedDragon;

        // Handle the bond formation
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (BondedDragon != null)
            {
                Messages.Message($"{pawn.Name} has formed a powerful bond with {BondedDragon.Name}.", pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        // Main tick method to handle bond updates and dragon death
        public override void Tick()
        {
            base.Tick();

            // Check every 1000 ticks (~16.6 seconds)
            if (pawn.IsHashIntervalTick(1000))
            {
                Pawn dragon = BondedDragon;
                if (dragon == null || dragon.Dead)
                {
                    Messages.Message($"{pawn.Name}'s bonded dragon has died!", pawn, MessageTypeDefOf.NegativeEvent);
                    pawn.health.RemoveHediff(this);
                    pawn.health.AddHediff(HediffDefOf_Crows.Crows_DragonBondTorn); // Apply the "torn bond" hediff
                }
                else
                {
                    // Update mood thoughts related to bond distance
                    DragonBondUtils.UpdateDragonBondThought(pawn, dragon);
                }
            }
        }

        // Save/load bonded dragon data
        public override void ExposeData()
        {
            base.ExposeData();
            Pawn dragon = BondedDragon;
            Scribe_References.Look(ref dragon, "BondedDragon");
        }
    }

    // Hediff class for a Torn Dragon Bond
    public class Hediff_DragonBondTorn : HediffWithComps
    {
        public Pawn BondedDragon => this.TryGetComp<HediffComp_DragonBond>()?.BondedDragon;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            Messages.Message($"{pawn.Name} feels a deep loss as their bond with {BondedDragon?.Name} is torn.", pawn, MessageTypeDefOf.NegativeEvent);
        }
    }

    // HediffCompProperties to define the Dragon Bond component
    public class HediffCompProperties_DragonBond : HediffCompProperties
    {
        public HediffCompProperties_DragonBond()
        {
            this.compClass = typeof(HediffComp_DragonBond); // Assign the correct comp class
        }
    }

    // Component class to handle dragon bond logic
    public class HediffComp_DragonBond : HediffComp
    {
        // Reference to the bonded dragon
        public Pawn BondedDragon => this.Pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Bond);

        public override void CompExposeData()
        {
            base.CompExposeData();
            // Custom save/load logic (if needed)
        }

        // Triggered when the hediff is added
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            Messages.Message($"{Pawn.LabelShort} is now bonded with a dragon.", Pawn, MessageTypeDefOf.PositiveEvent);
        }

        // Triggered when the hediff is removed
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            Messages.Message($"{Pawn.LabelShort} has lost their dragon bond.", Pawn, MessageTypeDefOf.NegativeEvent);
        }

        // Periodically check bond status and update thoughts
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.IsHashIntervalTick(1000) && BondedDragon != null && BondedDragon.Dead)
            {
                Messages.Message($"{Pawn.LabelShort}'s bonded dragon has died.", Pawn, MessageTypeDefOf.NegativeEvent);
                Pawn.health.RemoveHediff(this.parent);
                Pawn.health.AddHediff(HediffDefOf_Crows.Crows_DragonBondTorn);
            }
        }
    }
}
