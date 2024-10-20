using RimWorld;
using Verse;
using System.Linq;

namespace Crows_DragonBond
{
    public class ThoughtWorker_DragonBond : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            // Ensure pawn belongs to the player's faction
            if (pawn.Faction == null || !pawn.Faction.IsPlayer)
                return ThoughtState.Inactive;

            // Ensure pawn has an Ideology and check for the relevant Precept
            if (pawn.Ideo == null || !pawn.Ideo.HasPrecept(DefDatabase<PreceptDef>.GetNamed("Crows_DragonVeneratedPrecept")))
                return ThoughtState.Inactive;

            // Retrieve the ModExtension from the AbilityDef
            ModExtension_Crows_DragonBond modExt = DefDatabase<AbilityDef>
                .GetNamed("Crows_DragonBondAbility")
                .GetModExtension<ModExtension_Crows_DragonBond>();

            // Check if the modExtension exists and if the allowedAnimals list is populated
            if (modExt == null || modExt.allowedAnimals == null)
                return ThoughtState.Inactive;

            // Check if any of the allowed dragons are in the faction
            bool dragonPresent = pawn.Map.mapPawns.AllPawnsSpawned
                .Any(p => p.Faction == pawn.Faction && modExt.allowedAnimals.Contains(p.def));

            return dragonPresent ? ThoughtState.ActiveDefault : ThoughtState.Inactive;
        }
    }

    public class ThoughtWorker_DragonDeath : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            // Ensure pawn belongs to the player's faction
            if (pawn.Faction == null || !pawn.Faction.IsPlayer)
                return ThoughtState.Inactive;

            // Ensure pawn has an Ideology and check for the relevant Precept
            if (pawn.Ideo == null || !pawn.Ideo.HasPrecept(DefDatabase<PreceptDef>.GetNamed("Crows_DragonVeneratedPrecept")))
                return ThoughtState.Inactive;

            // Check if there are any dragon corpses on the map
            bool dragonDied = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse)
                .Any(corpse => IsTamedDragon(corpse as Corpse, pawn.Faction));

            return dragonDied ? ThoughtState.ActiveDefault : ThoughtState.Inactive;
        }

        private bool IsTamedDragon(Corpse corpse, Faction faction)
        {
            if (corpse == null || corpse.InnerPawn == null) return false;

            Pawn deadPawn = corpse.InnerPawn;
            ModExtension_Crows_DragonBond modExt = DefDatabase<AbilityDef>
                .GetNamed("Crows_DragonBondAbility")
                .GetModExtension<ModExtension_Crows_DragonBond>();

            return deadPawn.Faction == faction && modExt != null && modExt.allowedAnimals.Contains(deadPawn.def);
        }
    }

    public class DragonDeathTracker : MapComponent
    {
        public DragonDeathTracker(Map map) : base(map) { }

        // Override the tick method to check for dead dragons
        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // Check for dead tamed dragons and apply mood debuff if necessary
            foreach (var corpse in map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
            {
                Pawn deadPawn = (corpse as Corpse)?.InnerPawn;
                if (deadPawn != null && IsTamedDragon(deadPawn))
                {
                    ApplyMoodDebuffForDragonDeath(deadPawn);
                }
            }
        }

        // Check if the dead pawn is a tamed dragon belonging to the player's faction
        private bool IsTamedDragon(Pawn pawn)
        {
            ModExtension_Crows_DragonBond modExt = DefDatabase<AbilityDef>
                .GetNamed("Crows_DragonBondAbility")
                .GetModExtension<ModExtension_Crows_DragonBond>();

            return pawn.Faction != null && pawn.Faction.IsPlayer
                   && modExt != null
                   && modExt.allowedAnimals.Contains(pawn.def);
        }

        // Apply the dragon death mood debuff to colonists with the correct precept
        private void ApplyMoodDebuffForDragonDeath(Pawn dragon)
        {
            foreach (Pawn colonist in map.mapPawns.FreeColonists)
            {
                // Ensure colonist has the correct Ideology precept
                if (colonist.Ideo != null && colonist.Ideo.HasPrecept(DefDatabase<PreceptDef>.GetNamed("Crows_DragonVeneratedPrecept")))
                {
                    // Check if the colonist has a mood system
                    if (colonist.needs?.mood != null)
                    {
                        // Add the memory of mourning the dragon's death
                        colonist.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("Crows_DragonDeathMoodDebuff"));
                    }
                }
            }
        }
    }

    public class ThoughtWorker_DragonVenerated : ThoughtWorker
    {
        // Override for social thought (pawn: the observer, other: the target bonded pawn)
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            // Ensure both pawns belong to the player's faction
            if (pawn.Faction == null || other.Faction == null || !pawn.Faction.IsPlayer || !other.Faction.IsPlayer)
                return ThoughtState.Inactive;

            // Ensure both pawns share the relevant Ideology Precept
            if (pawn.Ideo == null || other.Ideo == null ||
                !pawn.Ideo.HasPrecept(DefDatabase<PreceptDef>.GetNamed("Crows_DragonBondingExaltedPrecept")) ||
                !other.Ideo.HasPrecept(DefDatabase<PreceptDef>.GetNamed("Crows_DragonBondingExaltedPrecept")))
                return ThoughtState.Inactive;

            // Check if the "other" pawn (the one being admired) has a DragonBond
            if (HasDragonBond(other))
            {
                // Pawn is admiring the other pawn's DragonBond, apply opinion boost
                return ThoughtState.ActiveAtStage(0);
            }

            // No DragonBond for the other pawn, thought is inactive
            return ThoughtState.Inactive;
        }

        private bool HasDragonBond(Pawn pawn)
        {
            // Retrieve the ModExtension from AbilityDef to check dragon bonding status
            ModExtension_Crows_DragonBond modExt = DefDatabase<AbilityDef>
                .GetNamed("Crows_DragonBondAbility")
                .GetModExtension<ModExtension_Crows_DragonBond>();

            if (modExt == null || modExt.allowedAnimals == null)
                return false;

            // Check if the pawn has a bonded dragon
            return pawn.relations.DirectRelations.Any(rel => rel.def == PawnRelationDefOf.Bond && modExt.allowedAnimals.Contains(rel.otherPawn.def));
        }
    }
}