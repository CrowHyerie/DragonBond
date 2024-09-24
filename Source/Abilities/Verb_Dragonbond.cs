using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;
using DragonBond;
using Verse.Sound;

namespace Crows_DragonBond
{
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
                Pawn existingBondedPawn = targetPawn.relations.GetFirstDirectRelationPawn(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"));
                if (existingBondedPawn != null && existingBondedPawn != casterPawn)
                {
                    Messages.Message($"The dragon scoffs at your attempt, it is already bonded to {existingBondedPawn.NameShortColored}.", MessageTypeDefOf.RejectInput, false);
                    this.ability.StartCooldown(60000); // Apply cooldown on bond rejection
                    return false; // Cancel if already bonded to someone else
                }

                // Check if the dragon is already tame
                if (targetPawn.Faction == Faction.OfPlayer)
                {
                    // Introduce a 20% chance for failure when the dragon is tame
                    if (Rand.Chance(0.2f))
                    {
                        DragonIgnoresPawn(casterPawn, targetPawn); // Custom failure behavior
                        this.ability.StartCooldown(60000); // Apply cooldown on failure
                        return false; // Bonding failed
                    }

                    // Apply the regular Bond relation along with Dragon Rider Bond
                    if (!casterPawn.relations.DirectRelationExists(PawnRelationDefOf.Bond, targetPawn))
                    {
                        casterPawn.relations.AddDirectRelation(PawnRelationDefOf.Bond, targetPawn);
                        Messages.Message($"{casterPawn.NameShortColored} forms a deep bond with {targetPawn.NameShortColored}.", MessageTypeDefOf.PositiveEvent, false);
                    }

                    Messages.Message("The dragon nuzzles your hand back, the bond was easily formed!", MessageTypeDefOf.PositiveEvent, false);
                    ApplyDragonRiderPsychicBond(casterPawn, targetPawn); // Directly bond the already tame dragon
                    this.ability.StartCooldown(60000); // Apply cooldown on success
                    return true; // Bonding successful
                }

                // Try to tame and bond the dragon
                if (TameDragon(targetPawn, casterPawn))
                {
                    Messages.Message("The dragon looks at you back in recognition. It has chosen you back and the bond was formed!", MessageTypeDefOf.PositiveEvent, false);
                    ApplyDragonRiderPsychicBond(casterPawn, targetPawn);
                    this.ability.StartCooldown(60000); // Apply cooldown on success
                    return true; // Bonding successful
                }
                else
                {
                    Messages.Message("The dragon recoils in fury; the bond attempt has failed, and it’s now a manhunter!", MessageTypeDefOf.ThreatBig, false);

                    if (targetPawn.Faction != casterPawn.Faction)
                    {
                        // Only make the dragon a manhunter if it was untamed or in another faction
                        targetPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
                    }

                    this.ability.StartCooldown(60000); // Apply cooldown on failure
                }
            }

            return false; // Bonding failed
        }

        // Custom failure behavior where the dragon ignores the pawn
        private void DragonIgnoresPawn(Pawn pawn, Pawn dragon)
        {
            Messages.Message($"The dragon {dragon.NameShortColored} ignores {pawn.NameShortColored}'s attempt to bond.", MessageTypeDefOf.NeutralEvent, false);

            // Add a thought debuff to the pawn for being ignored
            ThoughtDef thoughtDef = ThoughtDef.Named("Crows_DragonIgnoredBondAttempt");
            pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef, dragon);

            // Optional: Make the dragon perform some "mocking" action, like playing a sound or walking away
            SoundDef.Named("Dragon_Hit").PlayOneShot(dragon); // Play a dragon sound (mocking or a hiss)

            // Apply cooldown when the dragon ignores the pawn
            this.ability.StartCooldown(60000);
        }

        private bool TameDragon(Pawn dragon, Pawn tamer)
        {
            // Check if the dragon is already tame and part of the player's faction
            if (dragon.Faction == Faction.OfPlayer)
            {
                return true; // Dragon is already tame, no need to retame
            }

            // Proceed with taming for wild dragons
            if (!dragon.RaceProps.Animal || dragon.Faction != null)
            {
                return false; // Can't tame if it's not an animal or already part of a faction
            }

            if (Rand.Value < 0.2f)
            {
                return false; // 20% chance to fail bonding
            }

            // Tame the dragon by setting its faction to the player's faction
            dragon.SetFaction(Faction.OfPlayer);
            tamer.relations.AddDirectRelation(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), dragon);
            tamer.relations.AddDirectRelation(PawnRelationDefOf.Bond, dragon); // Ensure regular bond is also applied
            return true; // Bonding successful
        }

        private void ApplyDragonRiderPsychicBond(Pawn rider, Pawn dragon)
        {
            if (rider.RaceProps.Humanlike)
            {
                // Get the specific Hediff for the rider based on the dragon type
                Hediff riderHediff = HediffMaker.MakeHediff(GetDragonBondHediffForDragon(dragon), rider);
                rider.health.AddHediff(riderHediff);

                var riderBondComp = riderHediff.TryGetComp<HediffComp_DragonBondLink>();
                riderBondComp?.SetLinkedPawn(dragon);
            }

            if (dragon.RaceProps.Animal)
            {
                // Get the specific Hediff for the dragon based on its type
                Hediff dragonHediff = HediffMaker.MakeHediff(GetDragonBondHediffForDragon(dragon), dragon);
                dragon.health.AddHediff(dragonHediff);

                var dragonBondComp = dragonHediff.TryGetComp<HediffComp_DragonBondLink>();
                dragonBondComp?.SetLinkedPawn(rider);
            }
        }

        public static HediffDef GetDragonBondHediffForDragon(Pawn dragon)
        {
            switch (dragon.def.defName)
            {
                case "Blue_Dragon":
                    return HediffDef.Named("Crows_DragonBondBlue");
                case "Green_Dragon":
                    return HediffDef.Named("Crows_DragonBondGreen");
                case "Purple_Dragon":
                    return HediffDef.Named("Crows_DragonBondPurple");
                case "Red_Dragon":
                    return HediffDef.Named("Crows_DragonBondRed");
                case "White_Dragon":
                    return HediffDef.Named("Crows_DragonBondWhite");
                case "Yellow_Dragon":
                    return HediffDef.Named("Crows_DragonBondYellow");
                case "Black_Dragon":
                    return HediffDef.Named("Crows_DragonBondBlack");
                case "Jade_Dragon":
                    return HediffDef.Named("Crows_DragonBondJade");
                case "Gold_Dragon":
                    return HediffDef.Named("Crows_DragonBondGold");
                case "Silver_Dragon":
                    return HediffDef.Named("Crows_DragonBondSilver");
                case "True_Dragon":
                    return HediffDef.Named("Crows_DragonBondTrue");
                default:
                    return HediffDef.Named("Crows_DragonBondHediff");
            }
        }
    }
}
