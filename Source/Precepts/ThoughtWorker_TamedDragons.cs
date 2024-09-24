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

            // Retrieve the ModExtension from the AbilityDef (as specified)
            ModExtension_Crows_DragonBond modExt = DefDatabase<AbilityDef>
                .GetNamed("Crows_DragonBondAbility") // Make sure this matches your AbilityDef name
                .GetModExtension<ModExtension_Crows_DragonBond>();

            // Check if the modExtension exists and if the allowedAnimals list is populated
            if (modExt == null || modExt.allowedAnimals == null)
                return ThoughtState.Inactive;

            // Check if any of the allowed dragons are in the faction
            bool dragonPresent = pawn.Map.mapPawns.AllPawnsSpawned
                .Any(p => p.Faction == pawn.Faction && modExt.allowedAnimals.Contains(p.def));

            // Return active thought if a dragon is present
            return dragonPresent ? ThoughtState.ActiveDefault : ThoughtState.Inactive;
        }
    }

    public class ThoughtWorker_DragonDeath : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            // Ensure the pawn is from the player's faction
            if (pawn.Faction == null || !pawn.Faction.IsPlayer)
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

        // Apply the dragon death mood debuff to all colonists in the player's faction
        private void ApplyMoodDebuffForDragonDeath(Pawn dragon)
        {
            foreach (Pawn colonist in map.mapPawns.FreeColonists)
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




