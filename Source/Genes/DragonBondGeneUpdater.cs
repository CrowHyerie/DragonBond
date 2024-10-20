using Crows_DragonBond;
using Verse;

public class DragonBondGeneUpdater : MapComponent
{
    public DragonBondGeneUpdater(Map map) : base(map)
    {
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        ApplyDraconicRegenerationGeneToAllBondedPawns();
    }

    private void ApplyDraconicRegenerationGeneToAllBondedPawns()
    {
        if (Prefs.DevMode)
        {
            Log.Message("DragonBondGeneUpdater: Checking all pawns for DragonBond...");
        }

        foreach (var pawn in this.map.mapPawns.AllPawnsSpawned) // Loops through all pawns on this map
        {
            if (Prefs.DevMode)
            {
                Log.Message($"DragonBondGeneUpdater: Inspecting pawn {pawn.Name} ({pawn.def.defName})...");
            }

            if (pawn.health?.hediffSet == null)
            {
                continue;
            }

            // Check for any DragonBond hediff
            Hediff dragonBondHediff = pawn.health.hediffSet.hediffs
                .FirstOrDefault(h => h.def.defName.StartsWith("Crows_DragonBond"));

            if (dragonBondHediff != null)
            {
                if (pawn.genes == null)
                {
                    continue;
                }

                // Check if the pawn already has the Draconic Regeneration gene
                if (!pawn.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("Crows_DraconicRegenerationGene")))
                {
                    // Add the Draconic Regeneration gene
                    GeneDef geneDef = DefDatabase<GeneDef>.GetNamed("Crows_DraconicRegenerationGene", true);
                    pawn.genes.AddGene(geneDef, true); // true for Xenogene
                    if (Prefs.DevMode)
                    {
                        if (Prefs.DevMode)
                        {
                            Log.Message($"DragonBondGeneUpdater: Added Draconic Regeneration gene to {pawn.Name}.");
                        }
                    }
                }
                else
                {
                }
            }
            else
            {
            }
        }

        Log.Message("DragonBondGeneUpdater: Finished checking all pawns.");
    }
}
