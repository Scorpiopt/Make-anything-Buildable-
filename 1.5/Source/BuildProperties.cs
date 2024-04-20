using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MakeAnythingBuildable
{
    public class BuildProperties : IExposable
    {
        public float workToBuild;
        public Dictionary<string, int> costList = new Dictionary<string, int>();
        public List<string> stuffCategories = new List<string>();
        public int costStuffCount;
        public List<string> researchRequirements = new List<string>();
        public string designationCategory;
        public BuildProperties()
        {

        }

        public BuildProperties(ThingDef def)
        {
            if (def.costList != null)
            {
                foreach (var cost in def.costList)
                {
                    this.costList[cost.thingDef.defName] = cost.count;
                }
            }
            this.workToBuild = def.GetStatValueAbstract(StatDefOf.WorkToBuild);
            if (def.stuffCategories != null)
            {
                foreach (var stuffCategory in def.stuffCategories)
                {
                    this.stuffCategories.Add(stuffCategory.defName);
                }
            }
            this.costStuffCount = def.costStuffCount;
            this.designationCategory = def.designationCategory?.defName;
            if (def.researchPrerequisites != null)
            {
                foreach (var researchPrerequisite in def.researchPrerequisites)
                {
                    this.researchRequirements.Add(researchPrerequisite.defName);
                }
            }
        }

        public void ModifyThingDef(ThingDef def)
        {
            if (this.costList?.Any() ?? false)
            {
                def.costList = new List<ThingDefCountClass>();
                foreach (var cost in this.costList)
                {
                    var resource = DefDatabase<ThingDef>.GetNamedSilentFail(cost.Key);
                    if (resource != null)
                    {
                        def.costList.Add(new ThingDefCountClass(resource, cost.Value));
                    }
                }
            }
            if (this.stuffCategories?.Any() ?? false)
            {
                def.stuffCategories = new List<StuffCategoryDef>();
                foreach (var stuffCategory in this.stuffCategories)
                {
                    var category = DefDatabase<StuffCategoryDef>.GetNamedSilentFail(stuffCategory);
                    if (category != null)
                    {
                        def.stuffCategories.Add(category);
                    }
                }
            }
            def.costStuffCount = this.costStuffCount;
            def.SetStatBaseValue(StatDefOf.WorkToBuild, this.workToBuild);
            if (!this.designationCategory.NullOrEmpty())
            {
                var designationCategoryDef = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(this.designationCategory);
                if (designationCategoryDef != null)
                {
                    def.designationCategory = designationCategoryDef;
                }
            }
            if (this.researchRequirements?.Any() ?? false)
            {
                def.researchPrerequisites = new List<ResearchProjectDef>();
                foreach (var researchRequirement in this.researchRequirements)
                {
                    var researchProject = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchRequirement);
                    if (researchProject != null)
                    {
                        def.researchPrerequisites.Add(researchProject);
                    }
                }
            }
            def.ResolveReferences();
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref workToBuild, "workToBuild");
            Scribe_Collections.Look(ref costList, "costList", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref stuffCategories, "stuffCategories", LookMode.Value);
            Scribe_Collections.Look(ref researchRequirements, "researchRequirements", LookMode.Value);
            Scribe_Values.Look(ref costStuffCount, "costStuffCount");
            Scribe_Values.Look(ref designationCategory, "designationCategory");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (costList is null)
                {
                    costList = new Dictionary<string, int>();
                }
                if (stuffCategories is null)
                {
                    stuffCategories = new List<string>();
                }
                if (researchRequirements is null)
                {
                    researchRequirements = new List<string>();
                }
            }
        }
    }
}
