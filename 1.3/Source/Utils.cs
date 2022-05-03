using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MakeAnythingBuildable
{
    [StaticConstructorOnStartup]
    public static class Utils
    {
        public static List<ThingDef> buildings = new List<ThingDef>();
        public static List<ThingDef> spawnableItems = new List<ThingDef>();
        static Utils()
        {
            buildings = DefDatabase<ThingDef>.AllDefs.Where(x => typeof(Building).IsAssignableFrom(x.thingClass) 
                && !typeof(Frame).IsAssignableFrom(x.thingClass)).ToList();
            spawnableItems = DefDatabase<ThingDef>.AllDefs.Where(x => x.Spawnable()).ToList();
        }
        public static bool Spawnable(this ThingDef item)
        {
            return (DebugThingPlaceHelper.IsDebugSpawnable(item) || item.Minifiable)
                                && !typeof(Filth).IsAssignableFrom(item.thingClass)
                                && !typeof(Mote).IsAssignableFrom(item.thingClass)
                                && item.category != ThingCategory.Ethereal && item.plant is null
                                && (item.building is null || item.Minifiable);
        }

        public static void ApplySettings()
        {
            var buildPropsByDefs = MakeAnythingBuildableMod.settings.buildPropsByDefs;
            foreach (var def in buildings)
            {
                if (buildPropsByDefs.TryGetValue(def.defName, out var buildProps))
                {
                    buildProps.ModifyThingDef(def);
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerKnowledgeDatabase), "ReloadAndRebind")]
    public static class PlayerKnowledgeDatabasePatch
    {
        public static void Prefix()
        {
            Utils.ApplySettings();
        }
    }

    [HarmonyPatch(typeof(StatsReportUtility), "Reset")]
    public static class StatsReportUtility_Reset_Patch
    {
        public static bool Prefix()
        {
            if (Current.Game?.World?.factionManager is null)
            {
                Reset();
                return false;
            }
            return true;
        }

        public static void Reset()
        {
            StatsReportUtility.scrollPosition = default(Vector2);
            StatsReportUtility.scrollPositionRightPanel = default(Vector2);
            StatsReportUtility.selectedEntry = null;
            StatsReportUtility.scrollPositioner.Arm(armed: false);
            StatsReportUtility.mousedOverEntry = null;
            StatsReportUtility.cachedDrawEntries.Clear();
            StatsReportUtility.quickSearchWidget.Reset();
            PermitsCardUtility.selectedPermit = null;
            PermitsCardUtility.selectedFaction = null;
        }
    }
}
