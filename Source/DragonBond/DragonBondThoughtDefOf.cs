using RimWorld;
using Verse;

[DefOf]
public static class DragonBondThoughtDefOf
{
    public static ThoughtDef Crows_DragonBondedClose;
    public static ThoughtDef Crows_DragonBondedFar;
    public static ThoughtDef Crows_DragonBondedDied;

    static DragonBondThoughtDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(DragonBondThoughtDefOf));
    }
    public static void UpdateDragonBondThought(Pawn pawn, Pawn dragon)
    {
        if (pawn.needs?.mood?.thoughts != null && dragon != null)
        {
            float distance = (pawn.Position - dragon.Position).LengthHorizontal;
            ThoughtDef currentThoughtDef = null;

            // Determine the appropriate thought based on the dragon's state and proximity
            if (dragon.Dead)
            {
                currentThoughtDef = Crows_DragonBondedDied;
            }
            else if (distance < 10f) // Close range
            {
                currentThoughtDef = Crows_DragonBondedClose;
            }
            else // Far distance
            {
                currentThoughtDef = Crows_DragonBondedFar;
            }

            // Apply the appropriate thought if available
            if (currentThoughtDef != null)
            {
                Thought newThought = ThoughtMaker.MakeThought(currentThoughtDef);

                // Ensure that the new thought is of type Thought_Memory before casting
                if (newThought is Thought_Memory thoughtMemory)
                {
                    thoughtMemory.otherPawn = dragon;
                    pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtMemory);
                }
                else
                {
                    // Log a warning if the thought is not of the expected type
                    Log.Warning($"Expected Thought_Memory but got {newThought.GetType()} for {currentThoughtDef.defName}.");
                }
            }
        }
    }

}
