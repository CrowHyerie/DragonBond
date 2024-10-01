using RimWorld;
using Verse;

namespace Crows_DragonBond
{
    public class Gene_DragonBond : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            // Optionally trigger the ability here if needed
        }

        public override void PostRemove()
        {
            base.PostRemove();
            // Cleanup or sever bond if necessary
        }
    }
}
