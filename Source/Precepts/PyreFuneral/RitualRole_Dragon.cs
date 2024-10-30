using Crows_DragonBond;
using RimWorld;
using System.Linq;
using Verse;

namespace Crows_DragonBond
{
    public class RitualRole_BondedDragon : RitualRole
    {
        public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            reason = null;

            // Retrieve the ModExtension to access allowed animals (in this case, dragons)
            ModExtension_Crows_DragonBond extension = DefDatabase<ThingDef>.AllDefs
                .Select(def => def.GetModExtension<ModExtension_Crows_DragonBond>())
                .FirstOrDefault(ext => ext != null && ext.allowedAnimals != null);

            // Ensure we have a valid extension with allowed animals
            if (extension == null)
            {
                if (!skipReason)
                {
                    reason = "CrowsDragonBond.NoValidDragons".Translate(base.LabelCap);
                }
                return false;
            }

            // Check if the pawn's ThingDef is in the allowedAnimals list (identifying it as a dragon)
            if (extension.allowedAnimals.Contains(p.def))
            {
                return true; // Valid dragon
            }

            // If the pawn is not a valid dragon, provide reason if not skipping
            if (!skipReason)
            {
                reason = "CrowsDragonBond.NotADragon".Translate(base.LabelCap);
            }
            return false;
        }

        public override bool AppliesToRole(Precept_Role role, out string reason, Precept_Ritual ritual = null, Pawn p = null, bool skipReason = false)
        {
            reason = null;
            return false;
        }
    }
}
