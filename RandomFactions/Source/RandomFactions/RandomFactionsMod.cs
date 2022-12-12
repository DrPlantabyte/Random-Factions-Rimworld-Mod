using HugsLib.Logs;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Verse;

namespace RandomFactions
{

    public class RandomFactionsMod : HugsLib.ModBase
    {
        public static string RANDOM_CATEGORY_NAME = "Random";

        private SettingHandle<int> xenoPercentHandle;
        private List<FactionDef> defaultFactions = null;

        public RandomFactionsMod() {
            // constructor (invoked by reflection, do not add parameters)
            Logger.Trace("RandomFactions constructed");
        }
        public override string ModIdentifier
        {
            /*
Each ModBase class needs to have a unique identifier. Provide yours by overriding the ModIdentifier property. The identifier will be used in the settings XML to store your settings, so avoid spaces and special characters. You will get an exception if you provide an improper identifier.
             */
            get
            {
                return "RandFactions";
            }
        }
        /*
Property Notes: HugsLib.ModBase.*

.Logger

The Logger property allows a mod to write identifiable messages to the console. Error and Warning methods are also available. Calling:
Logger.Message("test");
will result in the following console output:
[ModIdentifier] test
Additionally, the Trace method of the logger will write a console message only if Rimworld is in Dev mode.

.ModIsActive

Returns true if the mod is enabled in the Mods dialog. Disabled mods would not be loaded or instantiated, but if a mod was enabled, and then disabled in the Mods dialog this property will return false.
This property is no longer useful as of A17, since the game restarts when the mod configuration changes.

.Settings

Returns the ModSettingsPack for your mod, from where you can get your SettingsHandles. See the wiki page of creating configurable settings for more information.

.ModContentPack

Returns the ModContentPack for your mod. This can be used to access the name and PackageId, as well as loading custom files from your mod's directory.

.HarmonyInstance

All assemblies that declare a class that extends ModBase are automatically assigned a HarmonyInstance and their Harmony patches are applied. This is where the Harmony instance for each ModBase instance is stored.

.HarmonyAutoPatch

Override this and return false if you don't want a HarmonyInstance to be automatically created and the patches in your assembly applied. Having multiple ModBase classes in your assembly will produce warnings if their HarmonyAutoPatch is not disabled, but your assembly will only be patched once.
*/

        //        public override void EarlyInitialize()
        //        {
        //            /*
        //Called during Verse.Mod instantiation, and only if your class has the [EarlyInit] attribute.
        //Nothing is yet loaded at this point, so you might want to place your initialization code in Initialize, instead this method is mostly used for custom patching.
        //You will not receive any callbacks on Update, FixedUpdate, OnGUI and SettingsChanged until after the Initialize callback comes through.
        //Initialize will still be called at the normal time, regardless of the [EarlyInit] attribute.*/
        //            base.EarlyInitialize();
        //        }

        public override void Initialize()
        {
            /*
Called once when the mod is first initialized, closely after it is instantiated.
If the mods configuration changes, or Defs are reloaded, this method is not called again.*/
            base.Initialize();
        }

        public override void DefsLoaded()
        {
            /*
Called after all Defs are loaded.
This happens when game loading has completed, after Initialize is called. This is a good time to inject any Random defs. Make sure you call HugsLib.InjectedDefHasher.GiveShortHasToDef on any defs you manually instantiate to avoid def collisions (it's a vanilla thing).
Since A17 it no longer matters where you initialize your settings handles, since the game automatically restarts both when the mod configuration or the language changes. This means that both Initialize and DefsLoaded are only ever called once per ModBase instance.*/
            base.DefsLoaded();

            // add mod options
            this.xenoPercentHandle = Settings.GetHandle<int>("PercentXenotype", "% Xenotype Fequency", "If Biotech DLC is detected, then random factions will substitute baseliners for xenotypes this percent of the time (default 20%)", 20, Validators.IntRangeValidator(0, 100));
            xenoPercentHandle.ValueChanged += handle => {
                //Logger.Message("Xenotype changed to " + xenoPercentHandle.Value);
            };
        }

        public override void Update()
        {
            /*
Called on each frame.
Keep in mind that frame rate varies significantly, so this callback is recommended only to do any custom drawing.*/
        }

        public override void FixedUpdate()
        {
            /*
Called on each physics update by Unity.
This is like Update, but independent of frame rate.*/
            base.FixedUpdate();
        }

        public override void OnGUI()
        {
            /*
Called when the Unity GUI system is redrawn or receives an input event.
This is a good time to draw custom GUI overlays and controls.
OnGUI will no longer be called during loading screens and will have UI scaling automatically applied.
Also useful for listening for input events, such as key strokes. Here's an example of a key binding listener:

if (Event.current.type == EventType.KeyDown) {
	if (KeyBindingDefOf.Misc1.JustPressed) {
		// do things
	}
}
             */
            base.OnGUI();
        }

        public override void SceneLoaded(Scene scene)
        {
            /*
Called after a Unity scene change. Receives a UnityEngine.SceneManagement.Scene type argument.
There are two scenes in Rimworld- Entry and Play, which stand for the menu, and the game itself. Use Verse.GenScene to check which scene has been loaded.
Note, that not everything may be initialized after the scene change, and the game may be in the middle of map loading or generation.*/
            base.SceneLoaded(scene);
            //Logger.Trace(string.Format("Scene change: play scene == {0}, entry scene == {1}", Verse.GenScene.InPlayScene, Verse.GenScene.InEntryScene));
            if (Verse.GenScene.InEntryScene) resetFactionDefs();
        }

        private void resetFactionDefs()
        {
            if (defaultFactions != null)
            {
                // undo the mess created by RandFacDataStore
                DefDatabase<FactionDef>.Clear();
                foreach(var def in defaultFactions)
                {
                    DefDatabase<FactionDef>.Add(def);
                }
            }
            defaultFactions = null;
        }

        public override void WorldLoaded()
        {
            /*
Called after the game has started and the world has been initialized.
Any maps may not have been initialized at this point.
This is a good place to get your UtilityWorldObjects with the data you store in the save file. See the appropriate wiki page on how to use those.
This is only called after the game has started, not on the "select landing spot" world map.
*/
            base.WorldLoaded();
            Logger.Message("World loaded! Applying Random generation rules to factions...");
            var world = Find.World;
            this.defaultFactions = new List<FactionDef>();
            string facdef_list = "";
            foreach(var fdef in DefDatabase<FactionDef>.AllDefs)
            {
                this.defaultFactions.Add(fdef);
                if (facdef_list.Length > 0) { facdef_list += ", "; }
                facdef_list += fdef.defName;
            }
            Logger.Trace(string.Format("Found {0} faction definitions: {1}", DefDatabase<FactionDef>.DefCount, facdef_list));
            string xenodef_list = "";
            foreach (var xdef in DefDatabase<XenotypeDef>.AllDefs)
            {
                if (xenodef_list.Length > 0) { xenodef_list += ", "; }
                xenodef_list += xdef.defName;
            }
            Logger.Trace(string.Format("Found {0} xenotype definitions: {1}", DefDatabase<XenotypeDef>.DefCount, xenodef_list));
            var hasRoyalty = ModsConfig.RoyaltyActive;
            var hasIdeology = ModsConfig.IdeologyActive;
            var hasBiotech = ModsConfig.BiotechActive;
            // load save data store (if it exists)
            Logger.Trace("fetching data store...");
            var wcomp = world.GetComponent(typeof(RandFacDataStore));
            RandFacDataStore dataStore;
            if (wcomp == null )
            {
                Logger.Trace("data store is null, initializing new one...");
                dataStore = new RandFacDataStore(world);
                Logger.Trace("adding data store to world...");
                world.components.Add(dataStore);
            } else
            {
                dataStore = (RandFacDataStore)wcomp;
                Logger.Trace("...data store loaded");
            }
            dataStore.Logger = Logger;
            if (hasBiotech)
            {
                dataStore.xenotypePercent = this.xenoPercentHandle.Value;
            } else
            {
                dataStore.xenotypePercent = 0;
            }
            Logger.Trace("synchronizing defs...");
            dataStore.synchronizeFactionDefs();
            RandomFactionGenerator fgen = new RandomFactionGenerator(dataStore, world, hasRoyalty, hasIdeology, hasBiotech, Logger);
            var allFactionList = new List<Faction>();
            var replaceList = new List<Faction>();
            foreach (var fac in Find.FactionManager.AllFactions) { allFactionList.Add(fac); }
            foreach(var fac in allFactionList)
            {
                Logger.Trace(string.Format("Found faction: {0} ({1})\tisPlayer == {2}\tisRandom == {3}\tisDefeated == {4}", fac.Name, fac.def.defName, fac.IsPlayer, fac.def.categoryTag.EqualsIgnoreCase(RANDOM_CATEGORY_NAME), fac.defeated)); // TODO: remove
                if (fac.def.categoryTag.EqualsIgnoreCase(RANDOM_CATEGORY_NAME) && fac.defeated == false)
                {
                    Logger.Trace(string.Format(">>> Detected random faction {0} ({1})", fac.Name, fac.def.defName)); // TODO: remove
                    replaceList.Add(fac);
                }
            }
            foreach(var pfFac in replaceList)
            {
                if (pfFac.def.defName.EqualsIgnoreCase("RF_RandomFaction"))
                {
                    fgen.replaceWithRandomNonHiddenFaction(pfFac);
                }
                else if (pfFac.def.defName.EqualsIgnoreCase("RF_RandomPirateFaction"))
                {
                    fgen.replaceWithRandomNonHiddenEnemyFaction(pfFac);
                }
                else if (pfFac.def.defName.EqualsIgnoreCase("RF_RandomRoughFaction"))
                {
                    fgen.replaceWithRandomNonHiddenWarlordFaction(pfFac);
                }
                else if (pfFac.def.defName.EqualsIgnoreCase("RF_RandomTradeFaction"))
                {
                    fgen.replaceWithRandomNonHiddenTraderFaction(pfFac);
                } else
                {
                    Logger.Warning("Faction defName {0} not recognized! Cannot replace faction {1} ({2})", pfFac.def.defName, pfFac.Name, pfFac.def.defName);
                }
            }
            Logger.Message(string.Format("...Random faction generation complete! Replaced {0} factions.", replaceList.Count));

        }

        public override void MapComponentsInitializing(Map map)
        {
            /*
Called during the initialization of a map, more exactly right after Verse.Map.ConstructComponents(). Receives a Verse.Map type argument.
This is a good place for sneaky business and getting access to data that is unavailable after map loading has completed.
This is right before the map is populated with data from a save file.*/
            base.MapComponentsInitializing(map);
        }

        public override void MapGenerated(Map map)
        {
            /*
Called right after a new map has finished generating. This is the equivalent of creating a MapComponent and overriding its MapGenerated method, but without the need to pollute save files with unnecessary map components.*/
            base.MapGenerated(map);
        }

        public override void MapLoaded(Map map)
        {
            /*
Called after map loading and generation is complete and after Verse.MapDrawer.RegenerateEverythingNow was executed. Receives a Verse.Map type argument.
This is a good place to run initialization code specific to a game map.
Note, that this method may be called zero or multiple times after loading a save, depending on how many maps the player has active at the moment.*/
            base.MapLoaded(map);
        }

        public override void MapDiscarded(Map map)
        {
            /*
Called after a map has been abandoned or otherwise made inaccessible. Works on player bases, encounter maps, destroyed faction bases, etc. This is a good place to clean up any map-related data in your World and UtilityWorldObjects to avoid bloating the save file.*/
            base.MapDiscarded(map);
        }

        public override void Tick(int currentTick)
        {
            /*
Called during each tick, when a game is loaded. Receives an int argument, which is the number of the current tick.
Will be called even if the player is on the world map and no map is currently loaded.
Will not be called on the "select landing spot" world map.
*/
            base.Tick(currentTick);
        }

        public override void SettingsChanged()
        {
            /*
Called after the player closes the Mod Settings dialog after changing any setting.
Note, that the setting changed may belong to another mod.*/
            base.SettingsChanged();
        }

        public override void ApplicationQuit()
        {
            /*
Called before the game process shuts down. This is a good place to update any non-critical mod setting values or write any custom data files.
"Quit to OS", clicking the "X" button on the window, and pressing Alt+F4 all execute this event. There are still ways to forcibly terminate the game process, as well as the possibility of a crash, so this callback is not 100% reliable.
Modified mod settings are automatically saved after this call.
*/
            base.ApplicationQuit();
        }
    }
}