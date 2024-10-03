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

            // Section: Dragon Trading Settings
            listingStandard.Label("Dragon Trading Settings"); // Header for Dragon Trading
            listingStandard.GapLine(); // Horizontal divider

            // Checkbox for Goodwill Penalty
            listingStandard.CheckboxLabeled("Enable Goodwill Penalties for Dragon Trades", ref settings.goodwillPenaltyEnabled,
                "If disabled, trading dragon-related items will not result in goodwill penalties.");

            // Optional spacing between sections
            listingStandard.Gap();

            // Slider for manhunter chance during Dragon Bond (0% to 100%)
            listingStandard.Label("Dragon Bond Settings"); // Header for Dragon Bond
            listingStandard.GapLine(); // Horizontal divider

            // Manhunter chance slider
            listingStandard.Label($"Manhunter Chance: {Mathf.RoundToInt(settings.manhunterChance * 100)}%");
            settings.manhunterChance = listingStandard.Slider(settings.manhunterChance, 0f, 1f);  // Slider with values from 0% to 100%

            listingStandard.End();
        }

        // The name of the settings category in the Mod Settings menu.
        public override string SettingsCategory()
        {
            return "DragonBond Mod Settings";
        }
    }
}    

public class DragonBondSettings : ModSettings
{
    // Store the setting for whether goodwill penalties are enabled.
    public bool goodwillPenaltyEnabled = true;

    // Store the manhunter chance for Dragon Bond (default to 20%)
    public float manhunterChance = 0.2f;

    // This method will save and load the settings.
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref goodwillPenaltyEnabled, "goodwillPenaltyEnabled", true);
        Scribe_Values.Look(ref manhunterChance, "manhunterChance", 0.2f);  // Default to 20%

    }
}

