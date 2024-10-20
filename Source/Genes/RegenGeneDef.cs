using DD;
using RimWorld;
using Verse;

namespace Crows_DragonBond
{
    public class Gene_DragonBondRegeneration : Gene
    {
        private const float RegenTimeout = 10f;

        public override void Tick()
        {
            base.Tick();

            // Use the HealthUtils-based conditions to decide when to trigger regeneration
            if (HealthUtils.CanRegen(pawn, RegenTimeout) && HealthUtils.ShouldRegen(pawn))
            {
                TryApplyDraconicRegeneration();
            }
        }

        private void TryApplyDraconicRegeneration()
        {
            // Check if the pawn already has the DraconicRegeneration hediff
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(DD_HediffDefOf.DraconicRegeneration);

            if (hediff == null)
            {

                // Apply the hediff if conditions are met
                if (HealthUtils.CanRegen(pawn) && HealthUtils.ShouldRegen(pawn))
                {

                    var regenGiver = new DD.HediffGiver_Regeneration
                    {
                        hediff = DD_HediffDefOf.DraconicRegeneration,
                        regenTimeout = 10f,  // Timeout before regen starts
                        startInBed = false    // Not required to be in bed
                    };

                    regenGiver.TryApply(pawn);
                }
                else
                {

                }
            }
            else
            {

                // Regeneration already applied, ensure it's healing as expected
                if (HealthUtils.CanRegen(pawn) && HealthUtils.ShouldRegen(pawn))
                {
                    // Tend to any injuries or scars that need healing
                    TendToInjuriesAndScars(pawn, hediff);
                }
                else
                {

                }
            }
        }

        private void TendToInjuriesAndScars(Pawn pawn, Hediff hediff)
        {
            bool healedCriticalInjury = false;  // Track whether any critical injury was healed

            // Handle injury healing
            foreach (Hediff injury in pawn.health.hediffSet.hediffs)
            {
                // Heal critical injuries (non-permanent, tendable injuries)
                if (injury is Hediff_Injury injuryInjury && injury.TendableNow() && !injury.IsPermanent())
                {
                    HealthUtils.Heal(injury, 0.05f);  // Adjust healing amount
                    healedCriticalInjury = true;
                }
                // Handle bleeding wounds separately
                else if (injury.Bleeding)
                {
                    HealthUtils.Heal(injury, 0.05f);  // Heal bleeding injuries
                    healedCriticalInjury = true;
                }
                else
                {

                }
            }

            // Only heal scars if no critical injuries were healed
            if (!healedCriticalInjury)
            {
                // Check for scars and use HediffComp_HealScar if needed
                if (hediff is HediffWithComps hediffWithComps)
                {
                    foreach (HediffComp comp in hediffWithComps.comps)
                    {
                        float severityAdjustment = 0;

                        // Heal scars using the HediffComp_HealScar component
                        if (comp is DD.HediffComp_HealScar healScarComp)
                        {
                            healScarComp.CompPostTick(ref severityAdjustment);  // Trigger scar healing
                        }

                        // Tend injuries using HediffComp_TendInjury
                        if (comp is DD.HediffComp_TendInjury tendInjuryComp)
                        {
                            tendInjuryComp.CompPostTick(ref severityAdjustment);  // Trigger injury tending
                        }

                        // Adjust regeneration severity scaling
                        if (comp is DD.HediffComp_RegenSeverityScaling regenSeverityComp)
                        {
                            regenSeverityComp.CompPostTick(ref severityAdjustment);  // Adjust severity based on scaling

                        }
                    }
                }
            }
            else
            {

            }
        }
    }
}

[DefOf]
public static class DB_GeneDefOf
{
    public static GeneDef Crows_DraconicRegenerationGene;
}
