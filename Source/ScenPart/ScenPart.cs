using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CrowsDragonBond
{
    public class ScenPart_SpawnDeadDragon : ScenPart
    {
        public ScenPart_SpawnDeadDragon() { } // Default constructor

        public override void PostMapGenerate(Map map)
        {
            var center = map.Center;

            // Generate ruins first
            if (ScenarioUtils.GenerateRuins(map, center))
            {
                Log.Message("Ruins generated successfully.");
            }
            else
            {
                Log.Error("Failed to generate ruins - using fallback placement.");
            }

            // Generate the dead dragon
            var dragonDef = DefDatabase<PawnKindDef>.GetNamed("White_Dragon"); // Replace with your dragon's PawnKindDef
            var dragon = PawnGenerator.GeneratePawn(new PawnGenerationRequest(dragonDef, Faction.OfAncients, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: true));

            // Kill the dragon and spawn the corpse near the center (or within ruins if possible)
            ScenarioUtils.DamageUntilDead(dragon, new List<ThingDef>());
            ScenarioUtils.SpawnNear(dragon.Corpse, map, center);
        }
    }

    public class ScenPart_SpawnDragonNestAndEgg : ScenPart
    {
        public ScenPart_SpawnDragonNestAndEgg() { } // Default constructor

        public override void PostMapGenerate(Map map)
        {
            if (Find.TickManager.TicksGame > 5f) return;

            var center = map.Center;

            // Ensure ruins are generated first
            if (ScenarioUtils.GenerateRuins(map, center))
            {
                Log.Message("Ruins generated successfully.");
            }
            else
            {
                Log.Error("Failed to generate ruins - using fallback placement.");
            }

            // Spawn the dragon's nest
            var nestDef = DefDatabase<ThingDef>.GetNamed("DragonNest"); // Replace with your nest's ThingDef
            var eggDef = DefDatabase<ThingDef>.GetNamed("EggWhiteDragonFertilized"); // Replace with your egg's ThingDef

            var nest = ThingMaker.MakeThing(nestDef);
            var nestCell = ScenarioUtils.SpawnNear(nest, map, center);

            // Spawn the dragon egg inside the nest
            var egg = ThingMaker.MakeThing(eggDef);
            GenPlace.TryPlaceThing(egg, nestCell, map, ThingPlaceMode.Direct);
        }
    }

    internal static class ScenarioUtils
    {
        internal static void DamageUntilDead(Pawn pawn, List<ThingDef> weapons)
        {
            // Add the logic to damage the pawn until it's dead
            while (!pawn.Dead)
            {
                var damageInfo = new DamageInfo(DamageDefOf.Cut, 9999); // Force kill with high damage
                pawn.TakeDamage(damageInfo);
            }
        }

        internal static IntVec3 SpawnNear(Thing thing, Map map, IntVec3 center)
        {
            // Spawn the item near the center
            RCellFinder.TryFindRandomCellNearWith(center, c => c.Standable(map) && !c.Fogged(map), map, out IntVec3 spawnCell, 10, 20);
            GenSpawn.Spawn(thing, spawnCell, map);
            return spawnCell;
        }

        internal static void SpawnNear(Corpse corpse, Map map, IntVec3 center)
        {
            // Spawn the corpse near the center (inside the ruins)
            SpawnNear((Thing)corpse, map, center);
        }

        internal static bool GenerateRuins(Map map, IntVec3 center)
        {
            // Use the identified GenStepDef to generate ruins
            var ruinsGenStep = DefDatabase<GenStepDef>.GetNamed("ScatterRuinsSimple", false);
            if (ruinsGenStep != null)
            {
                try
                {
                    // Generate the ruins using the found GenStepDef
                    ruinsGenStep.genStep.Generate(map, new GenStepParams());
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error($"Error generating ruins with ScatterRuinsSimple: {ex.Message}");
                }
            }
            else
            {
                Log.Error("ScatterRuinsSimple not found. Ruins generation skipped.");
            }

            return false;
        }
    }
}
