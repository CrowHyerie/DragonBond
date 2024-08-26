using Verse;
using RimWorld;
using System.Linq;
using UnityEngine; 

namespace DragonBond
{
    public class Verb_DragonBond : Verb_CastAbility
    {
        // This method checks if the target is a valid dragon.
        private bool IsValidTarget(Pawn target)
        {
            {
                // Check if the target's def is in the DB_DragonRaces category
                if (!target.def.thingCategories.Contains(ThingCategoryDef.Named("DB_DragonRaces")))
                {
                    Messages.Message("This ability can only be used on dragons!", MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                return true;
            }
            return true;
        }

        // Ability cast.
        protected override bool TryCastShot()
        {
            Pawn casterPawn = CasterPawn;
            Pawn targetPawn = (Pawn)currentTarget.Thing;

            if (IsValidTarget(targetPawn))
            {
                // Check if the caster already has a bonded dragon
                if (casterPawn.relations.DirectRelations.Any(relation => relation.def == PawnRelationDefOf.Bond))
                {
                    Messages.Message("You can only be bonded with one Dragon at a time!", MessageTypeDefOf.RejectInput, false);
                    return false; // Cancels the action if there's already a bonded dragon
                }

                //Taming and bonding logic
                if (TameDragon(targetPawn, casterPawn))
                {
                    Messages.Message("Dragon bonding successful!", MessageTypeDefOf.PositiveEvent, false);
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

        // Handles taming and bonding the dragon.
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
            tamer.relations.AddDirectRelation(PawnRelationDefOf.Bond, dragon);

            return true; // Bonding successful
        }
    }
}
