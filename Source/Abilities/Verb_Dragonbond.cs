using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;

namespace Crows_DragonBond
{
    // This class defines the mod extension that allows specifying which animals can be bonded with.
    public class ModExtension_Crows_DragonBond : DefModExtension
    {
        public List<ThingDef> allowedAnimals; // List of allowed dragon ThingDefs
    }

    public class Verb_DragonBond : Verb_CastAbility
    {
        // This method checks if the target is a valid dragon.
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

        // This method handles the actual casting of the ability.
        protected override bool TryCastShot()
        {
            Pawn casterPawn = CasterPawn;
            Pawn targetPawn = (Pawn)currentTarget.Thing;


            if (IsValidTarget(targetPawn))
            {
                // Check if the caster already has a bonded dragon
                if (casterPawn.relations.DirectRelations.Any(relation => relation.def == DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond")))
                {
                    Messages.Message("You can only be bonded with one Dragon at a time!", MessageTypeDefOf.RejectInput, false);
                    return false; // Cancels the action if there's already a bonded dragon
                }

                // Implement the taming and bonding logic
                if (TameDragon(targetPawn, casterPawn))
                {
                    Messages.Message("Dragon bonding successful!", MessageTypeDefOf.PositiveEvent, false);
                    ApplyDragonRiderPsychicBond(casterPawn); // Apply the psychic bond effect to the rider
                    return true; // Indicates the action was successful.
                }
                else
                {
                    Messages.Message("Dragon bonding failed. The dragon has turned into a manhunter!", MessageTypeDefOf.ThreatBig, false);
                    targetPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent); // Dragon becomes a manhunter
                    return false; // Indicates the action failed.
                }
            }
            return false; // Indicates the action failed.
        }

        // This method handles taming and bonding the dragon.
        private bool TameDragon(Pawn dragon, Pawn tamer)
        {
            if (!dragon.RaceProps.Animal || dragon.Faction != null)
            {
                return false; // Can't tame if it's not an animal or if it's already part of a faction
            }

            // 20% fail chance for bonding
            if (Rand.Value < 0.2f)
            {
                return false; // Bonding failed
            }

            dragon.SetFaction(Faction.OfPlayer); // Tame the dragon by setting its faction to the player's faction

            // Form a bond between the dragon and the tamer
            tamer.relations.AddDirectRelation(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), dragon);
            return true; // Bonding successful
        }

        // This method applies the psychic bond effect to the rider (human pawn) only.
        private void ApplyDragonRiderPsychicBond(Pawn rider)
        {
            if (rider.RaceProps.Humanlike)
            {
                Hediff hediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed("Crows_DragonBondEffect"), rider);
                rider.health.AddHediff(hediff);
            }
        }
    }
}
