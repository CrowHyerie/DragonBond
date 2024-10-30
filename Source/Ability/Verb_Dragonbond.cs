using Verse;
using RimWorld;
using Verse.Sound;
using System.Collections.Generic;
using System.Linq;

namespace Crows_DragonBond
{

    public class Verb_DragonBond : Verb_CastAbility
    {
        private bool IsValidTarget(Pawn target)
        {
            // Retrieve the ModExtension containing allowedAnimals
            ModExtension_Crows_DragonBond extension = this.ability.def.GetModExtension<ModExtension_Crows_DragonBond>();

            // Ensure the extension exists and contains allowed animals
            if (extension != null && extension.allowedAnimals != null)
            {
                // Debug log for checking target pawn's defName
                if (Prefs.DevMode)
                {
                    Log.Message($"Attempting to bond with target: {target.def.defName}");
                }

                // Check if the target's ThingDef is in the allowedAnimals list
                if (extension.allowedAnimals.Contains(target.def))
                {
                    return true; // Target is a valid dragon
                }
            }

            // If the target is not valid, show a rejection message
            Messages.Message("CrowsDragonBond.AbilityBlocked".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        // Handle the actual casting of the ability
        protected override bool TryCastShot()
        {
            Pawn casterPawn = CasterPawn;
            Pawn targetPawn = (Pawn)currentTarget.Thing;

            // Check if the caster already has a bond, and disable the ability if true
            if (HasDragonBond(casterPawn))
            {
                Messages.Message("CrowsDragonBond.BondExists".Translate(), MessageTypeDefOf.RejectInput, false);
                this.ability.StartCooldown(60000); // Apply cooldown on success
                return false; // Block casting if already bonded
            }

            // Check if the target is a valid dragon using IsValidTarget
            if (!IsValidTarget(targetPawn))
            {
                // Return false if the target is invalid
                return false;
            }

            // Check if the target is valid using ValidateTarget
            if (ValidateTarget(targetPawn))
            {
                // Check if the dragon is already bonded to another pawn
                Pawn existingBondedPawn = targetPawn.relations.GetFirstDirectRelationPawn(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"));
                if (existingBondedPawn != null && existingBondedPawn != casterPawn)
                {
                    Messages.Message("CrowsDragonBond.DragonDoubleBondRejection".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false; // Cancel if already bonded to someone else
                }

                // Check if the dragon is already tame
                if (targetPawn.Faction == Faction.OfPlayer)
                {
                    // Retrieve the manhunter chance from mod settings
                    float manhunterChance = DragonBondMod.settings.manhunterChance;

                    // Introduce a chance for failure based on the player's slider setting
                    if (Rand.Chance(manhunterChance))
                    {
                        DragonIgnoresPawn(casterPawn, targetPawn); // Custom failure behavior
                        return false; // Bonding failed
                    }

                    // Apply the regular Bond relation along with Dragon Rider Bond
                    if (!casterPawn.relations.DirectRelationExists(PawnRelationDefOf.Bond, targetPawn))
                    {
                        casterPawn.relations.AddDirectRelation(PawnRelationDefOf.Bond, targetPawn);
                    }

                    Messages.Message("CrowsDragonBond.TameBondSuccess".Translate(), MessageTypeDefOf.PositiveEvent, false);
                    ApplyDragonRiderPsychicBond(casterPawn, targetPawn); // Directly bond the already tame dragon
                    this.ability.StartCooldown(60000); // Apply cooldown on success
                    return true; // Bonding successful
                }

                // Try to tame and bond the dragon
                if (TameDragon(targetPawn, casterPawn))
                {
                    Messages.Message("CrowsDragonBond.BondSuccess".Translate(), MessageTypeDefOf.PositiveEvent, false);
                    ApplyDragonRiderPsychicBond(casterPawn, targetPawn);
                    this.ability.StartCooldown(60000); // Apply cooldown on success
                    return true; // Bonding successful
                }
                else
                {
                    // Use the player's manhunter chance from the slider
                    if (Rand.Chance(DragonBondMod.settings.manhunterChance))
                    {
                        Messages.Message("CrowsDragonBond.DragonManhunter".Translate(), MessageTypeDefOf.ThreatBig, false);

                        if (targetPawn.Faction != casterPawn.Faction)
                        {
                            // Only make the dragon a manhunter if it was untamed or in another faction
                            targetPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
                        }
                        this.ability.StartCooldown(60000); // Apply cooldown on success
                        return false; // Bonding failed, dragon turned into a manhunter
                    }
                    else
                    {
                        return false; // Bonding failed, but no manhunter
                    }
                }
            }

            // If ValidateTarget(targetPawn) is false, return false by default
            return false;
        }

        // Check if the caster already has a bond (disables ability if true)
        private bool HasDragonBond(Pawn pawn)
        {
            // Retrieve the ModExtension from AbilityDef to check dragon bonding status
            ModExtension_Crows_DragonBond modExt = DefDatabase<AbilityDef>
                .GetNamed("Crows_DragonBondAbility")
                .GetModExtension<ModExtension_Crows_DragonBond>();

            if (modExt == null || modExt.allowedAnimals == null)
                return false;

            // Check if the pawn has a bonded dragon in their relations
            bool hasBondedDragon = pawn.relations.DirectRelations
                .Any(rel => rel.def == PawnRelationDefOf.Bond && modExt.allowedAnimals.Contains(rel.otherPawn.def));

            if (!hasBondedDragon)
                return false;

            // Check if the pawn still has the Dragon Bond HediffComp (bond could have been broken)
            HediffComp_DragonBondLink bondComp = pawn.health?.hediffSet?.GetAllComps()
                .OfType<HediffComp_DragonBondLink>()
                .FirstOrDefault();

            // If the bondComp is null, the bond has been severed
            if (bondComp == null)
                return false;

            return true;
        }

        // Custom failure behavior where the dragon ignores the pawn
        private void DragonIgnoresPawn(Pawn pawn, Pawn dragon)
        {
            Messages.Message("CrowsDragonBond.IgnoredBond".Translate(), MessageTypeDefOf.NeutralEvent, false);

            // Add a thought debuff to the pawn for being ignored
            ThoughtDef thoughtDef = ThoughtDef.Named("Crows_DragonIgnoredBondAttempt");
            pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef, dragon);

            // Optional: Make the dragon perform some "mocking" action, like playing a sound or walking away
            SoundDef.Named("Dragon_Hit").PlayOneShot(dragon); // Play a dragon sound (mocking or a hiss)
            this.ability.StartCooldown(60000); // Apply cooldown on success
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

            // Replace hardcoded 20% chance with the player-configurable manhunter chance
            if (Rand.Value < DragonBondMod.settings.manhunterChance)
            {
                return false; // Bonding failed based on the player's manhunter chance
            }

            // Tame the dragon by setting its faction to the player's faction
            dragon.SetFaction(Faction.OfPlayer);
            tamer.relations.AddDirectRelation(DefDatabase<PawnRelationDef>.GetNamed("Crows_DragonRiderBond"), dragon);
            tamer.relations.AddDirectRelation(PawnRelationDefOf.Bond, dragon); // Ensure regular bond is also applied
            this.ability.StartCooldown(60000); // Apply cooldown on success
            return true; // Bonding successful
        }

        private void ApplyDragonRiderPsychicBond(Pawn rider, Pawn dragon)
        {
            // Ensure rider is a humanlike pawn before applying bond
            if (rider.RaceProps.Humanlike)
            {
                // Add the bond Hediff to the rider
                Hediff riderHediff = HediffMaker.MakeHediff(GetDragonBondHediffForDragon(dragon), rider);
                rider.health.AddHediff(riderHediff);

                var riderBondComp = riderHediff.TryGetComp<HediffComp_DragonBondLink>();
                riderBondComp?.SetLinkedPawn(dragon);

                // Check if rider can have genes (i.e., has a GeneSet)
                if (rider.genes != null)
                {
                    // Get the gene definition for the Draconic Regeneration gene
                    GeneDef geneDef = DefDatabase<GeneDef>.GetNamed("Crows_DraconicRegenerationGene", true);

                    // Add the Draconic Regeneration gene to the rider as a Xenogene
                    rider.genes.AddGene(geneDef, true);  // 'true' indicates this is a Xenogene
                }
                else
                {
                    Log.Error($"Tried to apply a gene to {rider.Name}, but they do not have a gene set.");
                }
            }
            // Apply the bond Hediff to the dragon
            if (dragon.RaceProps.Animal)
            {
                Hediff dragonHediff = HediffMaker.MakeHediff(GetDragonBondHediffForDragon(dragon), dragon);
                dragon.health.AddHediff(dragonHediff);

                var dragonBondComp = dragonHediff.TryGetComp<HediffComp_DragonBondLink>();
                dragonBondComp?.SetLinkedPawn(rider);
            }
        }

        public static HediffDef GetDragonBondHediffForDragon(Pawn dragon)
        {
            if (Prefs.DevMode)
            {
                Log.Message($"Dragon Color Name {dragon.def.defName}");
            }

            // Default switch for dragons
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
                //Rimworld of the Dragon
                case "Bloodwyrm":
                    return HediffDef.Named("Crows_DragonBondBloodwyrm");
                case "GoliathZaldrizes":
                    return HediffDef.Named("Crows_DragonBondGoliathZaldrizes");
                case "TopazZaldrizes":
                    return HediffDef.Named("Crows_DragonBondTopazZaldrizes");
                case "IvoryZaldrizes":
                    return HediffDef.Named("Crows_DragonBondIvoryZaldrizes");
                case "VerdantZaldrizes":
                    return HediffDef.Named("Crows_DragonBondVerdantZaldrizes");
                case "KunziteZaldrizes":
                    return HediffDef.Named("Crows_DragonBondKunziteZaldrizes");
                case "BloodstoneZaldrizes":
                    return HediffDef.Named("Crows_DragonBondBloodstoneZaldrizes");
                case "AuricZaldrizes":
                    return HediffDef.Named("Crows_DragonBondAuricZaldrizes");
                case "RubyZaldrizes":
                    return HediffDef.Named("Crows_DragonBondRubyZaldrizes");
                case "AgateZaldrizes":
                    return HediffDef.Named("Crows_DragonBondAgateZaldrizes");
                case "LapisZaldrizes":
                    return HediffDef.Named("Crows_DragonBondLapisZaldrizes");
                case "BronzeZaldrizes":
                    return HediffDef.Named("Crows_DragonBondBronzeZaldrizes");
                case "ArgentumZaldrizes":
                    return HediffDef.Named("Crows_DragonBondArgentumZaldrizes");
                case "DawnZaldrizes":
                    return HediffDef.Named("Crows_DragonBondDawnZaldrizes");
                case "CopperZaldrizes":
                    return HediffDef.Named("Crows_DragonBondCopperZaldrizes");
                case "PyriteZaldrizes":
                    return HediffDef.Named("Crows_DragonBondPyriteZaldrizes");
                case "RegalZaldrizes":
                    return HediffDef.Named("Crows_DragonBondPyriteZaldrizes");
                case "MistZaldrizes":
                    return HediffDef.Named("Crows_DragonBondMistZaldrizes");
                case "JadeZaldrizes":
                    return HediffDef.Named("Crows_DragonBondJadeZaldrizes");
            }
        }

        //Dragon Bond Abilities
        public static AbilityDef GetDragonBondAbilityForPawn(Pawn dragon)
        {
            if (Prefs.DevMode)
            {
                Log.Message($"Dragon Color Name {dragon.def.defName}");
            }

            // Check if the mod with the packageId is loaded.
            bool isDragonModLoaded = ModLister.HasActiveModWithName("aurorankingslayer.asoiafdd");

            if (isDragonModLoaded)
            {
                switch (dragon.def.defName)
                {
                    case "Bloodwyrm":
                        return DefDatabase<AbilityDef>.GetNamed("DD_DragonSpit_Fire");
                    case "GoliathZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityBlack");
                    case "TopazZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityGold");
                    case "IvoryZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityWhite");
                    case "VerdantZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityGreen");
                    case "KunziteZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityJade");
                    case "BloodstoneZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityBlack");
                    case "AuricZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityGold");
                    case "RubyZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("DD_DragonBreath_Fire");
                    case "AgateZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilitySilver");
                    case "LapisZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityBlue");
                    case "BronzeZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityTrue");
                    case "ArgentumZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilitySilver");
                    case "DawnZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityPurple");
                    case "CopperZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityBlue");
                    case "PyriteZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityYellow");
                    case "RegalZaldrizes":
                        return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityPurple");
                }
            }

            // Default switch for your original dragons
            switch (dragon.def.defName)
            {
                case "Blue_Dragon":
                    return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityBlue");
                case "Green_Dragon":
                    return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityGreen");
                case "Purple_Dragon":
                    return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityPurple");
                case "Red_Dragon":
                    return DefDatabase<AbilityDef>.GetNamed("DD_DragonSpit_Fire");
                case "White_Dragon":
                    return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityWhite");
                case "Yellow_Dragon":
                    return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityYellow");
                case "Black_Dragon":
                    return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityBlack");
                case "Jade_Dragon":
                    return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityJade");
                case "Gold_Dragon":
                    return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityGold");
                case "Silver_Dragon":
                    return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilitySilver");
                case "True_Dragon":
                    return DefDatabase<AbilityDef>.GetNamed("Crows_DragonBondAbilityTrue");
                default:
                    return DefDatabase<AbilityDef>.GetNamed("DD_DragonBreath_Fire");
            }
        }

    }
}
