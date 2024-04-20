using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MakeAnythingBuildable
{
    public class MakeAnythingBuildableMod : Mod
    {
        public static MakeAnythingBuildableSettings settings;
        public MakeAnythingBuildableMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<MakeAnythingBuildableSettings>();
            new Harmony("MakeAnythingBuildableMod.Patches").PatchAll();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoSettingsWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            Utils.ApplySettings();
        }
        public override string SettingsCategory()
        {
            return this.Content.Name;
        }
    }
    public class MakeAnythingBuildableSettings : ModSettings
    {
        public Dictionary<string, BuildProperties> buildPropsByDefs = new Dictionary<string, BuildProperties>();
        public ThingDef curThingDef;
        public BuildProperties curBuildProps;
        private int scrollHeightCount = 0;
        private Vector2 firstColumnPos;
        private Vector2 secondColumnPos;
        private Vector2 scrollPosition;
        private Vector2 scrollPosition2;
        string buf1, buf2, buf3, buf4;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref buildPropsByDefs, "buildPropsByDefs", LookMode.Value, LookMode.Deep);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Rect rect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            Widgets.BeginGroup(rect);
            DrawPage(rect);
            Widgets.EndGroup();
        }

        public void DrawPage(Rect rect)
        {
            if (curBuildProps is null)
            {
                ResetPositions();
                ResetProps();
            }

            var outRect = new Rect(0, 0, rect.width, rect.height);
            var viewRect = new Rect(0, 0, rect.width - 30, scrollHeightCount);
            scrollHeightCount = 0;
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            Rect buttonRect = DoButton(ref firstColumnPos, "MAB.SelectBuilding".Translate(), delegate
            {
                Find.WindowStack.Add(new Window_SelectItem<ThingDef>(Utils.buildings, delegate (ThingDef selected)
                {
                    ResetProps();
                    if (!buildPropsByDefs.TryGetValue(selected.defName, out curBuildProps))
                    {
                        curBuildProps = new BuildProperties(selected);
                    }
                    curThingDef = selected;
                }, (ThingDef x) => 0, delegate (ThingDef x)
                {
                    var postfix = "";
                    if (x.designationCategory is null)
                    {
                        postfix = "MAB.NonBuildable".Translate();
                    }
                    return x.LabelCap + postfix;
                }));
            });

            firstColumnPos.y += 12;
            Rect removeRect;

            if (curBuildProps != null)
            {
                var saveChangesRect = new Rect(buttonRect.xMax + 15, buttonRect.y, buttonRect.width, buttonRect.height);
                if (Widgets.ButtonText(saveChangesRect, "MAB.SaveChanges".Translate()))
                {
                    buildPropsByDefs[curThingDef.defName] = curBuildProps;
                    ResetProps();
                    ResetPositions();
                    Utils.ApplySettings();
                    Widgets.EndScrollView();
                    return;
                }

                DoLabel(ref firstColumnPos, curThingDef.LabelCap);
                DoInput(firstColumnPos.x, firstColumnPos.y, "MAB.SetWorkToBuild".Translate(), ref curBuildProps.workToBuild, ref buf1, 200);
                firstColumnPos.y += 24;

                var labelRect = DoLabel(ref firstColumnPos, "MAB.SetCostList".Translate());
                var toRemove = "";
                foreach (var key in curBuildProps.costList.Keys.ToList())
                {
                    var costCount = curBuildProps.costList[key];
                    Rect skillRect = new Rect(firstColumnPos.x, firstColumnPos.y, buttonRect.width - 30, 24);
                    var def = DefDatabase<ThingDef>.GetNamedSilentFail(key ?? "");
                    if (def != null)
                    {
                        if (Widgets.ButtonText(skillRect, def.LabelCap))
                        {
                            Find.WindowStack.Add(new Window_SelectItem<ThingDef>(Utils.spawnableItems,
                            delegate (ThingDef selected)
                            {
                                curBuildProps.costList.Remove(key);
                                curBuildProps.costList.Add(selected.defName, costCount);
                            }, x => x.index, (ThingDef x) => x.LabelCap));
                        }

                        DoInput(skillRect.xMax + 5, firstColumnPos.y, "MAB.Count".Translate(), ref costCount, ref buf2);
                        curBuildProps.costList[key] = costCount;

                        removeRect = new Rect(skillRect.xMax + 135, firstColumnPos.y, 20, 21f);
                        if (Widgets.ButtonImage(removeRect, TexButton.Delete))
                        {
                            toRemove = key;
                        }
                        firstColumnPos.y += 24;
                    }
                }

                if (!toRemove.NullOrEmpty())
                {
                    curBuildProps.costList.Remove(toRemove);
                }

                buttonRect = DoButton(ref firstColumnPos, "Add".Translate().CapitalizeFirst(), delegate
                {
                    Find.WindowStack.Add(new Window_SelectItem<ThingDef>(Utils.spawnableItems,
                    delegate (ThingDef selected)
                    {
                        curBuildProps.costList.Add(selected.defName, 1);
                    }, x => x.index, (ThingDef x) => x.LabelCap));
                });
                firstColumnPos.y += 12;

                labelRect = DoLabel(ref firstColumnPos, "MAB.SetStuffCategories".Translate());
                toRemove = "";
                for (var i = 0; i < curBuildProps.stuffCategories.ListFullCopy().Count; i++)
                {
                    var stuffCategory = curBuildProps.stuffCategories[i];
                    Rect skillRect = new Rect(firstColumnPos.x, firstColumnPos.y, buttonRect.width - 30, 24);
                    var def = DefDatabase<StuffCategoryDef>.GetNamed(stuffCategory ?? "");
                    if (def != null)
                    {
                        if (Widgets.ButtonText(skillRect, def.LabelCap))
                        {
                            var floatList = new List<FloatMenuOption>();
                            foreach (var stuffCategoryDef in DefDatabase<StuffCategoryDef>.AllDefs
                                .Where(x => !curBuildProps.stuffCategories.Any(y => x.defName == y)))
                            {
                                floatList.Add(new FloatMenuOption(stuffCategoryDef.LabelCap, delegate
                                {
                                    var index = curBuildProps.stuffCategories.IndexOf(stuffCategory);
                                    curBuildProps.stuffCategories.RemoveAt(index);
                                    curBuildProps.stuffCategories.Insert(index, stuffCategoryDef.defName);
                                }));
                            }
                            Find.WindowStack.Add(new FloatMenu(floatList));
                        }

                        removeRect = new Rect(skillRect.xMax + 5, firstColumnPos.y, 20, 21f);
                        if (Widgets.ButtonImage(removeRect, TexButton.Delete))
                        {
                            toRemove = stuffCategory;
                        }
                        firstColumnPos.y += 24;
                    }
                }

                if (!toRemove.NullOrEmpty())
                {
                    curBuildProps.stuffCategories.RemoveAll(x => x == toRemove);
                }

                buttonRect = DoButton(ref firstColumnPos, "Add".Translate().CapitalizeFirst(), delegate
                {
                    var floatList = new List<FloatMenuOption>();
                    foreach (var stuffCategory in DefDatabase<StuffCategoryDef>.AllDefs
                        .Where(x => !curBuildProps.stuffCategories.Any(y => x.defName == y)))
                    {
                        floatList.Add(new FloatMenuOption(stuffCategory.LabelCap, delegate
                        {
                            curBuildProps.stuffCategories.Add(stuffCategory.defName);
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatList));
                });

                DoInput(firstColumnPos.x, firstColumnPos.y, "MAB.SetCostStuffCount".Translate(), ref curBuildProps.costStuffCount, ref buf3, 200);
                firstColumnPos.y += 24;

                labelRect = DoLabel(ref firstColumnPos, "MAB.SetResearchRequirements".Translate());
                toRemove = "";
                for (var i = 0; i < curBuildProps.researchRequirements.ListFullCopy().Count; i++)
                {
                    var researchRequirement = curBuildProps.researchRequirements[i];
                    Rect skillRect = new Rect(firstColumnPos.x, firstColumnPos.y, buttonRect.width - 30, 24);
                    var def = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(researchRequirement ?? "");
                    if (def != null)
                    {
                        if (Widgets.ButtonText(skillRect, def.LabelCap))
                        {
                            Find.WindowStack.Add(new Window_SelectItem<ResearchProjectDef>(DefDatabase<ResearchProjectDef>.AllDefs
                                .Where(x => !curBuildProps.researchRequirements.Any(y => x.defName == y)).ToList(),
                            delegate (ResearchProjectDef selected)
                            {
                                var index = curBuildProps.researchRequirements.IndexOf(researchRequirement);
                                curBuildProps.researchRequirements.RemoveAt(index);
                                curBuildProps.researchRequirements.Insert(index, selected.defName);
                            }, x => x.index, (ResearchProjectDef x) => x.LabelCap));
                        }
                        removeRect = new Rect(skillRect.xMax + 5, firstColumnPos.y, 20, 21f);
                        if (Widgets.ButtonImage(removeRect, TexButton.Delete))
                        {
                            toRemove = researchRequirement;
                        }
                        firstColumnPos.y += 24;
                    }
                }

                if (!toRemove.NullOrEmpty())
                {
                    curBuildProps.researchRequirements.RemoveAll(x => x == toRemove);
                }

                buttonRect = DoButton(ref firstColumnPos, "Add".Translate().CapitalizeFirst(), delegate
                {
                    Find.WindowStack.Add(new Window_SelectItem<ResearchProjectDef>(DefDatabase<ResearchProjectDef>.AllDefs
                    .Where(x => !curBuildProps.researchRequirements.Any(y => x.defName == y)).ToList(),
                            delegate (ResearchProjectDef selected)
                            {
                                curBuildProps.researchRequirements.Add(selected.defName);
                            }, x => x.index, (ResearchProjectDef x) => x.LabelCap));
                });
                labelRect = DoLabel(ref firstColumnPos, "MAB.SelectDesignationCategory".Translate());
                var designatorCategoryDef = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(curBuildProps.designationCategory ?? "");
                buttonRect = DoButton(ref firstColumnPos, designatorCategoryDef != null ? designatorCategoryDef.LabelCap.ToString() : "-", delegate
                {
                    var floatList = new List<FloatMenuOption>();
                    foreach (var designationCategory in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
                    {
                        floatList.Add(new FloatMenuOption(designationCategory.LabelCap, delegate
                        {
                            curBuildProps.designationCategory = designationCategory.defName;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatList));
                });

            }

            Widgets.EndScrollView();
            scrollHeightCount = (int)Mathf.Max(rect.height, Mathf.Max(firstColumnPos.y, secondColumnPos.y));
            ResetPositions();
        }
        private void DoInput(float x, float y, string label, ref int count, ref string buffer, float width = 50)
        {
            Rect labelRect = new Rect(x, y, width, 24);
            Widgets.Label(labelRect, label);
            Rect inputRect = new Rect(labelRect.xMax, labelRect.y, 75, 24);
            buffer = count.ToString();
            Widgets.TextFieldNumeric<int>(inputRect, ref count, ref buffer);
        }
        private void DoInput(float x, float y, string label, ref float count, ref string buffer, float width = 50)
        {
            Rect labelRect = new Rect(x, y, width, 24);
            Widgets.Label(labelRect, label);
            Rect inputRect = new Rect(labelRect.xMax, labelRect.y, 75, 24);
            buffer = count.ToString();
            Widgets.TextFieldNumeric<float>(inputRect, ref count, ref buffer);
        }
        private void ResetPositions()
        {
            firstColumnPos = new Vector2(0, 0);
            secondColumnPos = new Vector2(420, 0);
        }
        private static Rect DoLabel(ref Vector2 pos, string label)
        {
            var labelRect = new Rect(pos.x, pos.y, 250, 24);
            Widgets.Label(labelRect, label);
            pos.y += 24;
            return labelRect;
        }

        private static Rect DoButton(ref Vector2 pos, string label, Action action)
        {
            var buttonRect = new Rect(pos.x, pos.y, 250, 24);
            pos.y += 24;
            if (Widgets.ButtonText(buttonRect, label))
            {
                UI.UnfocusCurrentControl();
                action();
            }
            return buttonRect;
        }

        public void ResetProps()
        {
            curThingDef = null;
            curBuildProps = null;
            buf1 = buf2 = buf3 = buf4 = "";
        }
    }
}
