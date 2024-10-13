using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Crows_DragonBond
{
    using System;
    using System.Collections.Generic;
    using RimWorld;
    using Verse;

    namespace Crows_DragonBond
    {
        public class GameComponent_DragonBondManager : GameComponent
        {
            public List<GoodwillImpactDelayed> goodwillImpacts = new List<GoodwillImpactDelayed>();

            // Constructor that takes a Game object and passes it to the base GameComponent constructor
            public GameComponent_DragonBondManager(Game game)
            {
            }

            public override void GameComponentTick()
            {
                base.GameComponentTick();

                // Iterate over all delayed goodwill impacts and apply them when their time has come.
                for (int i = goodwillImpacts.Count - 1; i >= 0; i--)
                {
                    if (Find.TickManager.TicksGame >= goodwillImpacts[i].impactInTicks)
                    {
                        goodwillImpacts[i].DoImpact();  // Apply the goodwill impact.
                        goodwillImpacts.RemoveAt(i);    // Remove the impact from the list after applying.
                    }
                }
            }

            public override void ExposeData()
            {
                base.ExposeData();
                Scribe_Collections.Look(ref goodwillImpacts, "goodwillImpacts", LookMode.Deep);
                if (Scribe.mode == LoadSaveMode.PostLoadInit && goodwillImpacts == null)
                {
                    goodwillImpacts = new List<GoodwillImpactDelayed>();
                }
            }

            public void AddGoodwillImpact(Faction faction, int amount, int delayTicks, System.Action onImpactApplied = null)
            {
                goodwillImpacts.Add(new GoodwillImpactDelayed(faction, amount, delayTicks, onImpactApplied));
                if (Prefs.DevMode)
                {
                    Log.Message($"[DragonBond] Scheduled goodwill impact for {faction.Name}: {amount} goodwill change in {delayTicks} ticks.");
                }
            }
        }
    }
}


// Class to handle delayed goodwill impacts for factions.
public class GoodwillImpactDelayed : IExposable
{
    public Faction faction;        // The faction affected by the goodwill impact.
    public int impactAmount;       // The amount of goodwill change.
    public int impactInTicks;      // When (in ticks) the impact will be applied.
    private Action onImpactApplied; // Optional action to trigger when the impact is applied.

    // Default constructor for Scribe serialization.
    public GoodwillImpactDelayed() { }

    // Constructor to initialize the goodwill impact.
    public GoodwillImpactDelayed(Faction faction, int impactAmount, int delayInTicks, Action onImpactApplied = null)
    {
        this.faction = faction;
        this.impactAmount = impactAmount;
        this.impactInTicks = Find.TickManager.TicksGame + delayInTicks;
        this.onImpactApplied = onImpactApplied;
    }

    // Method required for Scribe serialization.
    public void ExposeData()
    {
        Scribe_References.Look(ref faction, "faction");
        Scribe_Values.Look(ref impactAmount, "impactAmount");
        Scribe_Values.Look(ref impactInTicks, "impactInTicks");
    }

    // Apply the goodwill impact to the faction when the specified time has elapsed.
    public void DoImpact()
    {
        if (faction != null)
        {
            faction.TryAffectGoodwillWith(Faction.OfPlayer, impactAmount, true, false, null, null);
            if (Prefs.DevMode)
            {
                Log.Message($"[DragonBond] Goodwill impact applied to {faction.Name}: {impactAmount} goodwill change.");
            }

            // Invoke the callback action if one is specified.
            onImpactApplied?.Invoke();

            Messages.Message("CrowsDragonBond.VelosAshenAngeredDesc".Translate(), MessageTypeDefOf.NegativeEvent, true);
        }
    }
}

