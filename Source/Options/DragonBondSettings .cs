using System.Runtime;
using UnityEngine;
using Verse;


namespace Crows_DragonBond
{
    public class DragonBondMod : Mod
    {
        public static DragonBondSettings settings;

        public DragonBondMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<DragonBondSettings>();
        }

        // This method draws the mod settings menu.
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // Existing settings
            listingStandard.Label("Dragon Trading Settings");
            listingStandard.GapLine();
            listingStandard.CheckboxLabeled("Enable Goodwill Penalties for Dragon Trades", ref settings.goodwillPenaltyEnabled,
                "If disabled, trading dragon-related items will not result in goodwill penalties.");

            listingStandard.Gap();
            listingStandard.Label("Dragon Bond Settings");
            listingStandard.GapLine();
            listingStandard.Label($"Manhunter Chance: {Mathf.RoundToInt(settings.manhunterChance * 100)}%");
            settings.manhunterChance = listingStandard.Slider(settings.manhunterChance, 0f, 1f);

            // New settings for custom behaviors
            listingStandard.Gap();
            listingStandard.Label("Bond Tearing Reactions Settings");
            listingStandard.GapLine();

            // Checkbox for Catatonic Breakdown
            listingStandard.CheckboxLabeled("Apply Catatonic Breakdown on Dragon Death", ref settings.applyCatatonicBreakdown,
                "If enabled, the bonded human will suffer a catatonic breakdown when their dragon dies.");

            // Checkbox for Mood Zeroing
            listingStandard.CheckboxLabeled("Set Mood to Zero on Dragon Death", ref settings.setPawnMoodToZero,
                "If enabled, the bonded human's mood will be set to zero when their dragon dies.");

            // Checkbox for Dragon Leaving Faction
            listingStandard.CheckboxLabeled("Handle Dragon Leaving Faction on Human Death", ref settings.handleDragonLeaveFaction,
                "If enabled, dragons will leave the faction when their bonded human dies.");

            // New settings for debug
            listingStandard.Gap();
            listingStandard.Label("Debug");
            listingStandard.GapLine();

            //Draconic Regeneration Debug
            bool previousState = settings.applyDraconicRegenerationGene;
            listingStandard.CheckboxLabeled(
                "Tick this if you're missing Draconic Regeneration Gene",
                ref settings.applyDraconicRegenerationGene,
                "If enabled, applies Draconic Regeneration gene to bonded pawns without it."
            );
            if (settings.applyDraconicRegenerationGene && !previousState)
            {
                // Trigger the method only when the setting changes from unchecked to checked
                ApplyDraconicGeneIfEnabled();
            }

            listingStandard.End();
        }
    
        // The name of the settings category in the Mod Settings menu.
        public override string SettingsCategory()
        {
            return "DragonBond Mod Settings";
        }
        public void ApplyDraconicGeneIfEnabled()
        {
            if (settings.applyDraconicRegenerationGene)
            {
                // Run the gene application on all maps
                foreach (var map in Find.Maps)
                {
                    var geneUpdater = map.GetComponent<DragonBondGeneUpdater>();
                    if (geneUpdater != null)
                    {
                        geneUpdater.ApplyDraconicRegenerationGeneToAllBondedPawns();
                    }
                }
            }
        }
    }
}
public class DragonBondSettings : ModSettings
        {
            public bool goodwillPenaltyEnabled = true;  // Existing setting
            public float manhunterChance = 0.2f;  // Existing setting

            // New settings for handling specific behaviors
            public bool applyCatatonicBreakdown = true;
            public bool setPawnMoodToZero = true;
            public bool handleDragonLeaveFaction = true;
            public bool applyDraconicRegenerationGene = false;

            public override void ExposeData()
            {
                base.ExposeData();
                Scribe_Values.Look(ref goodwillPenaltyEnabled, "goodwillPenaltyEnabled", true);
                Scribe_Values.Look(ref manhunterChance, "manhunterChance", 0.2f);
                Scribe_Values.Look(ref applyCatatonicBreakdown, "applyCatatonicBreakdown", true);
                Scribe_Values.Look(ref setPawnMoodToZero, "setPawnMoodToZero", true);
                Scribe_Values.Look(ref handleDragonLeaveFaction, "handleDragonLeaveFaction", true);
                Scribe_Values.Look(ref applyDraconicRegenerationGene, "applyDraconicRegenerationGene", false);
            }
        }
    


