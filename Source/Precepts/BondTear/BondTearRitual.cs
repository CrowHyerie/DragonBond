using Verse;
using RimWorld;
using System.Linq;
using System.Collections.Generic;

namespace Crows_DragonBond
{
    public class RitualOutcomeEffectWorker_DragonBondTear : RitualOutcomeEffectWorker_FromQuality
    {
        public RitualOutcomeEffectWorker_DragonBondTear() { }
        public RitualOutcomeEffectWorker_DragonBondTear(RitualOutcomeEffectDef def) : base(def) { }

        public override bool SupportsAttachableOutcomeEffect => false;
        public override bool GivesDevelopmentPoints => false;

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            Log.Message("RitualOutcomeEffectWorker_DragonBondTear: Starting Apply method.");

            if (jobRitual == null)
            {
                Log.Error("RitualOutcomeEffectWorker_DragonBondTear: jobRitual is null. Exiting Apply method.");
                return;
            }

            if (jobRitual.Ritual == null)
            {
                Log.Error("No ritual associated with this jobRitual.");
                return;
            }

            // Quality and Outcome Assessment
            float quality = base.GetQuality(jobRitual, progress);
            RitualOutcomePossibility outcome = this.GetOutcome(quality, jobRitual);

            if (outcome == null)
            {
                Log.Error($"No valid outcome found for the specified quality: {quality}");
                return;
            }

            // Ensure valid LookTargets
            LookTargets lookTargets = jobRitual.selectedTarget;
            if (lookTargets == null || !lookTargets.IsValid)
            {
                lookTargets = new LookTargets(jobRitual.PawnWithRole("bondedPawn"));
                if (lookTargets == null || !lookTargets.IsValid)
                {
                    Log.Error("No valid look target for ritual outcome.");
                    return;
                }
            }

            string extraText = null;

            // Apply attachable outcome effects
            if (jobRitual.Ritual != null)
            {
                this.ApplyAttachableOutcome(totalPresence, jobRitual, outcome, out extraText, ref lookTargets);
            }

            // Positivity-based actions
            float positivityFactor;
            switch (outcome.positivityIndex)
            {
                case 1:
                    positivityFactor = 0.25f;
                    break;
                case 2:
                    positivityFactor = 0.5f;
                    break;
                case 3:
                    positivityFactor = 0.75f;
                    break;
                case 4:
                    positivityFactor = 1f;
                    break;
                default:
                    positivityFactor = 0f;
                    break;
            }

            // Apply memory and role-specific actions
            foreach (Pawn pawn in totalPresence.Keys)
            {
                if (outcome.memory != null && DefDatabase<ThoughtDef>.GetNamedSilentFail(outcome.memory.defName) != null)
                {
                    base.GiveMemoryToPawn(pawn, outcome.memory, jobRitual);
                }
            }

            // Check if bondedPawn exists
            Pawn bondedPawn = jobRitual.PawnWithRole("bondedPawn");
            if (bondedPawn != null)
            {
                HediffComp_DragonBondLink bondComp = bondedPawn.health?.hediffSet?.GetAllComps()
                    .OfType<HediffComp_DragonBondLink>()
                    .FirstOrDefault();

                if (bondComp != null)
                {
                    TearBond(bondedPawn, bondComp, positivityFactor);
                }
                else
                {
                    Log.Warning($"{bondedPawn.NameShortColored} does not have a DragonBond.");
                }
            }
            else
            {
                Log.Warning("No bondedPawn found for the role in this ritual.");
            }

            // Outcome Letter
            string outcomeText = outcome.description.Formatted(jobRitual.Ritual.Label).CapitalizeFirst() + "\n\n" +
                                 this.OutcomeQualityBreakdownDesc(quality, progress, jobRitual);

            string moodBreakdown = this.def.OutcomeMoodBreakdown(outcome);
            if (!moodBreakdown.NullOrEmpty())
            {
                outcomeText += "\n\n" + moodBreakdown;
            }

            if (extraText != null)
            {
                outcomeText += "\n\n" + extraText;
            }

            Find.LetterStack.ReceiveLetter("OutcomeLetterLabel".Translate(outcome.label.Named("OUTCOMELABEL"), jobRitual.Ritual.Label.Named("RITUALLABEL")),
                                            outcomeText,
                                            outcome.Positive ? LetterDefOf.RitualOutcomePositive : LetterDefOf.RitualOutcomeNegative,
                                            lookTargets);
        }

        private void TearBond(Pawn bondedPawn, HediffComp_DragonBondLink bondComp, float positivityFactor)
        {
            Log.Message($"Tearing bond for {bondedPawn.NameShortColored} with positivity factor {positivityFactor}");

            // Check that bondedPawn and bondComp are valid
            if (bondedPawn?.health != null && bondComp != null)
            {
                Pawn dragonPawn = bondComp.linkedPawn; // Assuming bondComp has a linked pawn (dragon) field

                if (dragonPawn != null && dragonPawn.health != null)
                {
                    // Apply your new bond-tear logic here
                    TearDragonBondUtils.HandleRitualBond(bondedPawn, dragonPawn);
                }
                else
                {
                    Log.Warning("TearBond: No valid dragon associated with this bond.");
                }

                // Remove the bond hediff from the bondedPawn
                bondedPawn.health.RemoveHediff(bondComp.parent);
            }
            else
            {
                Log.Warning("TearBond: Either bondedPawn's health is null or bondComp is null, unable to tear bond.");
            }
        }
    }
}
