using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;
using DragonBond;

namespace Crows_DragonBond
{
    // This class defines the mod extension that allows specifying which animals can be bonded with.
    public class ModExtension_Crows_DragonBond : DefModExtension
    {
        public List<ThingDef> allowedAnimals; // List of allowed dragon ThingDefs
    }

    public class Verb_DragonBond : Verb_CastAbility
    {

        private bool IsValidTarget(Pawn target)
        {
            // Retrieve the ModExtension containing allowedAnimals
            ModExtension_Crows_DragonBond extension = this.ability.def.GetModExtension<ModExtension_Crows_DragonBond>();

            // Ensure the extension exists and contains allowed animals
            if (extension != null && extension.allowedAnimals != null)
            {
                // Check if the target's ThingDef is in the allowedAnimals list
                if (extension.allowedAnimals.Contains(target.def))
                {
                    return true; // Target is a valid dragon
                }
            }

            // If the target is not valid, show a rejection message
            Messages.Message("This ability can only be used on dragons!", MessageTypeDefOf.RejectInput, false);
            return false;
        }

        // Handle the actual casting of the ability
        protected override bool TryCastShot()
        {
            Pawn casterPawn = CasterPawn;
            Pawn targetPawn = (Pawn)currentTarget.Thing;

            if (ValidateTarget(targetPawn))
            {
                // Check if the dragon is already bonded to another pawn
                if (targetPawn.relations.DirectRelations.Any(rel => rel.def == DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond")))
                {
                    Messages.Message("This dragon is already bonded to another pawn!", MessageTypeDefOf.RejectInput, false);
                    return false; // Cancel if already bonded
                }

                // Try to tame and bond the dragon
                if (TameDragon(targetPawn, casterPawn))
                {
                    Messages.Message("Dragon bonding successful!", MessageTypeDefOf.PositiveEvent, false);
                    ApplyDragonRiderPsychicBond(casterPawn, targetPawn);
                    return true; // Bonding successful
                }
                else
                {
                    Messages.Message("Dragon bonding failed. The dragon has turned into a manhunter!", MessageTypeDefOf.ThreatBig, false);

                    if (targetPawn.Faction != casterPawn.Faction)
                    {
                        // Only make the dragon a manhunter if it was untamed or in another faction
                        targetPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
                    }
                }
            }

            return false; // Bonding failed
        }

        private bool TameDragon(Pawn dragon, Pawn tamer)
        {
            if (!dragon.RaceProps.Animal || dragon.Faction != null)
            {
                return false; // Can't tame if it's not an animal or already part of a faction
            }

            if (Rand.Value < 0.2f)
            {
                return false; // 20% chance to fail bonding
            }

            dragon.SetFaction(Faction.OfPlayer); // Tame the dragon by setting its faction
            tamer.relations.AddDirectRelation(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), dragon);
            tamer.relations.AddDirectRelation(PawnRelationDefOf.Bond, dragon);
            return true; // Bonding successful
        }

        private void ApplyDragonRiderPsychicBond(Pawn rider, Pawn dragon)
        {
            if (rider.RaceProps.Humanlike)
            {
                Hediff riderHediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("Crows_DragonBondHediff"), rider);
                rider.health.AddHediff(riderHediff);

                var riderBondComp = riderHediff.TryGetComp<HediffComp_DragonBondLink>();
                riderBondComp?.SetLinkedPawn(dragon);
            }

            if (dragon.RaceProps.Animal)
            {
                Hediff dragonHediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("Crows_DragonBondHediff"), dragon);
                dragon.health.AddHediff(dragonHediff);

                var dragonBondComp = dragonHediff.TryGetComp<HediffComp_DragonBondLink>();
                dragonBondComp?.SetLinkedPawn(rider);
            }
        }
    }


}





