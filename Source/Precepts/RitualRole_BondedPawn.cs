using Crows_DragonBond;
using RimWorld;
using System.Linq;
using Verse;

namespace Crows_DragonBond
{
    public class RitualRole_BondedPawn : RitualRole
    {
        public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            reason = null;

            // Check if the pawn has the DragonBond hediff
            HediffComp_DragonBondLink bondComp = p.health?.hediffSet?.GetAllComps()
                .OfType<HediffComp_DragonBondLink>()
                .FirstOrDefault();

            // If the pawn doesn't have a DragonBond, return false and give a reason
            if (bondComp == null)
            {
                if (!skipReason)
                {
                    reason = "CrowsDragonBond.RitualRoleBondedPawn".Translate(base.LabelCap);
                }
                return false;
            }

            // If the pawn has a DragonBond, return true
            return true;
        }

        public override bool AppliesToRole(Precept_Role role, out string reason, Precept_Ritual ritual = null, Pawn p = null, bool skipReason = false)
        {
            // We aren't checking any specific role here, so just return false
            reason = null;
            return false;
        }
    }
}