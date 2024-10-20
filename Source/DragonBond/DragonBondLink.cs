using RimWorld;
using Verse;

namespace Crows_DragonBond
{
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

        // New flags to track proximity state
        public bool isDragonClose = false;
        public bool isDragonDistant = false;

        public ThingDef BondedPawn { get; internal set; }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref linkedPawn, "linkedPawn");
            Scribe_References.Look(ref lastPawnMap, "lastPawnMap");
            Scribe_References.Look(ref lastDragonMap, "lastDragonMap");
            Scribe_Values.Look(ref isDragonClose, "isDragonClose", defaultValue: false);
            Scribe_Values.Look(ref isDragonDistant, "isDragonDistant", defaultValue: false);
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
                if (Prefs.DevMode)
                {
                    Log.Message($"Dragon {linkedPawn.NameShortColored} and pawn {Pawn.NameShortColored} are on the same map. Applying close bond.");
                }
            }
            else
            {
                // Dragon and Pawn are on different maps, apply distance bond
                ApplyDistanceBond();
                if (Prefs.DevMode)
                {
                    Log.Message($"Dragon {linkedPawn.NameShortColored} and pawn {Pawn.NameShortColored} are on different maps. Applying distance bond.");
                }
            }
        }

        private void TearBond()
        {
            if (Prefs.DevMode)
            {
                Log.Message($"[DragonBond] TearBond: Trying to tear bond between {Pawn.NameShortColored} and {linkedPawn?.NameShortColored ?? "null"}.");
            }

            if (linkedPawn != null && !bondTorn)
            {
                bondTorn = true;
                TearDragonBondUtils.TearDragonBond(Pawn, linkedPawn, isDeath: true);
                linkedPawn = null;
                isDragonClose = false;
                isDragonDistant = false;
            }
        }

        // Called when the Hediff is removed, which often happens when the pawn dies
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            if (linkedPawn != null && !bondTorn)  // Check to ensure bond is not already torn
            {
                bondTorn = true;
                TearDragonBondUtils.TearDragonBond(this.Pawn, linkedPawn);
            }
        }

        private void ApplyCloseBond()
        {

            // Set the proximity flags to indicate the dragon is close
            isDragonClose = true;
            isDragonDistant = false;

            this.parent.Severity = 0.5f; // Keep the existing severity behavior

        }

        private void ApplyDistanceBond()
        {
            // Set the proximity flags to indicate the dragon is distant
            isDragonClose = false;
            isDragonDistant = true;

            this.parent.Severity = 1.0f; // Keep the existing severity behavior

        }
    }
}