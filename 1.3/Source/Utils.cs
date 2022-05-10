using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            try
            {
                return (DebugThingPlaceHelper.IsDebugSpawnable(item) || item.Minifiable)
                    && !typeof(Filth).IsAssignableFrom(item.thingClass)
                    && !typeof(Mote).IsAssignableFrom(item.thingClass)
                    && item.category != ThingCategory.Ethereal && item.plant is null
                    && (item.building is null || item.Minifiable);
            }
            catch (Exception ex)
            {
                Log.Error("Caught error processing " + item + ": " + ex.ToString());
                return false;
            }
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

    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    public static class DefGenerator_GenerateImpliedDefs_PreResolve_Patch
    {
        public static void Prefix()
        {
            Utils.ApplySettings();
        }
    }
}
