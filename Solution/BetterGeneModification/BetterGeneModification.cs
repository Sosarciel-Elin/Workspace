using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection.Emit;
using BepInEx.Logging;
using UnityEngine;



[BepInPlugin("sosarciel.bettergenemodification", "BetterGeneModification", "1.0.0.0")]
public class BetterGeneModification : BaseUnityPlugin {
    public static new ManualLogSource Logger;

    void Awake() {
        Logger = base.Logger;
        Initialize();
        var harmony = new Harmony("BetterGeneModification");
        harmony.PatchAll();
    }
    public void Initialize(){
        BGMUtils.GeneFeatMultiplier = base.Config.Bind<float>("General", "GeneFeatMultiplier", 1.0f, @"
基因专长点数乘数
Multiplier for gene feat points.
".Trim());
        BGMUtils.GeneSynthesisTimeMultiplier = base.Config.Bind<float>("General", "GeneSynthesisTimeMultiplier", 1.0f, @"
基因合成时间乘数
Multiplier for gene synthesis time.
".Trim());
        BGMUtils.ReturnItemOnRemoval = base.Config.Bind<bool>("General", "ReturnItemOnRemoval", false,
            "移除基因时退还物品\nReturn item when gene is removed");
        BGMUtils.MaxGeneCount = base.Config.Bind<int>("General", "MaxGeneCount", -1,
            "基因容量上限(-1为不做修改)\nMaximum capacity of genes (-1 for no change)");
        BGMUtils.MaxGeneCountMultiplier = base.Config.Bind<float>("General", "MaxGeneCountMultiplier", 1.0f,@"
基因容量上限乘数,根据原始基因上限调整基因个数, 仅在MaxGeneCount不启用时生效
Gene capacity upper limit multiplier, adjusts the number of genes based on the original limit, only effective when MaxGeneCount is disabled
".Trim());
        BGMUtils.AllGeneCanRemove = base.Config.Bind<bool>("General", "AllGeneCanRemove", false,@"
使所有基因均可移除
注意:原版狐狸女仆基因安装后将会无法移除
如果此项为真, 移除基因也仅会移除1点尾巴专长, 这会导致在你拥有多条尾巴的情况下, 移除专长后依然保留x-1条尾巴
Allows all genes to be removed
Note: In the base game, the Fox Maid gene cannot be removed after installation
If this option is true, removing the gene will only remove 1 tail feat point, which means if you have multiple tails, you will still retain x-1 tails after removing the feat
".Trim());
        BGMUtils.GeneSlotMultiplier = base.Config.Bind<float>("General", "GeneSlotMultiplier", 1,
            "基因槽位乘数\nMultiplier for gene slot consumption");



        BGMUtils.ModifyMetalDamageCalculation = base.Config.Bind<bool>("General", "ModifyMetalDamageCalculation", false,@"
修改金属伤害计算(无论特性等级固定为1/10的伤害减免, 原版在为999级时能获得高达1/1000的不合理减免, 在1级时却又几乎不提供任何减免)
Modify metal damage calculation (fixed 1/10 damage reduction regardless of trait level; original version provided unreasonable 1/1000 reduction at level 999, and almost no reduction at level 1)
".Trim());
        BGMUtils.ModifyHardwareUpgrade = base.Config.Bind<bool>("General", "ModifyHardwareUpgrade", false, @"
修改硬件升级(玛尼的神器效果)为拥有魔导生命体专长(魔像种族专长)的单位提供增幅(原版仅有机械种族与玛尼使徒能获得增幅)
Modify Hardware Upgrade (Mani artifact effect) to provide boost to units with the Arcane Core feat (Golem race feat); original version only provided boost to mechanical race and Mani apostles
".Trim());
        BGMUtils.ModifyDefensiveInstinct = base.Config.Bind<bool>("General", "ModifyDefensiveInstinct", false, @"
移除防卫本能专长(圣骑士职业专长)的信仰限制
Remove the faith restriction for the Defensive Instinct feat (Paladin class feat).
".Trim());

        BGMUtils.AllowBoostGeneDrop = base.Config.Bind<bool>("General", "AllowBoostGeneDrop", false,
            "允许增幅基因掉落 (无效) \nAllow boost gene drop (invalid)");
        BGMUtils.AllowRapidCastGeneDrop = base.Config.Bind<bool>("General", "AllowRapidCastGeneDrop", false,
            "允许高速咏唱基因掉落 (无效) \nAllow Rapid Cast gene drop (invalid)");
        BGMUtils.AllowRapidArrowGeneDrop = base.Config.Bind<bool>("General", "AllowRapidArrowGeneDrop", false,
            "允许连续射击基因掉落 (无效) \nAllow Rapid Arrow gene drop (invalid)");

        BGMUtils.AllowGeneCrafting = base.Config.Bind<bool>("General", "AllowGeneCrafting", false,
            "允许制造基因\nAllow gene crafting");
    }
    public void OnStartCore() {
        Logger.LogInfo("OnStartCore");
        if(BGMUtils.AllowGeneCrafting.Value){
            var dir = Path.GetDirectoryName(Info.Location);
            var sources = Core.Instance.sources;
            var tableDir = dir + "/BetterGeneModificationTable.xlsx";
            ModUtil.ImportExcel(tableDir, "recipes", sources.recipes);
        }
        Logger.LogInfo("End OnStartCore");
    }
}


public static class BGMUtils{
    public static ConfigEntry<float> GeneFeatMultiplier;
    public static ConfigEntry<float> GeneSynthesisTimeMultiplier;
    public static ConfigEntry<bool> ReturnItemOnRemoval;
    public static ConfigEntry<int> MaxGeneCount;
    public static ConfigEntry<float> MaxGeneCountMultiplier;
    public static ConfigEntry<bool> AllGeneCanRemove;
    public static ConfigEntry<float> GeneSlotMultiplier;
    public static ConfigEntry<bool> ModifyMetalDamageCalculation;
    public static ConfigEntry<bool> ModifyHardwareUpgrade;
    public static ConfigEntry<bool> ModifyDefensiveInstinct;
    public static ConfigEntry<bool> AllowBoostGeneDrop;
    public static ConfigEntry<bool> AllowRapidCastGeneDrop;
    public static ConfigEntry<bool> AllowRapidArrowGeneDrop;
    public static ConfigEntry<bool> AllowGeneCrafting;

    public static Thing ReturnGene(DNA dna){
        Thing thing = ThingGen.Create((dna.type == DNA.Type.Brain) ? "gene_brain" : "gene");
        Chara c = new();
        c.Create(dna.id);
        thing.MakeRefFrom(c);
        thing.c_DNA = dna;
        thing.ChangeMaterial(dna.GetMaterialId(dna.type));
        EClass.pc.Say("pick_thing", EClass.pc, thing, null, null);
        //dna.GenerateWithGene
        return thing;
    }
    public static string MillGeneTag = "sosarciel_mill_gene";
}

[HarmonyPatch(typeof(TraitCrafter))]
[HarmonyPatch(nameof(TraitCrafter.Craft))]
[HarmonyPatch(new [] { typeof(AI_UseCrafter) })]
class TraitCrafter_Craft_Patch_bgm {
    public static bool Prefix(TraitCrafter __instance, AI_UseCrafter ai, ref Thing __result){
        if(!BGMUtils.AllowGeneCrafting.Value) return true;

        SourceRecipe.Row source = __instance.GetSource(ai);
        if (source == null) return true;
        //TraitGrindstone
        //Recipe
        if (!EClass.player.knownCraft.Contains(source.id)){
            SE.Play("idea");
            Msg.Say("newKnownCraft");
            EClass.player.knownCraft.Add(source.id);
            if ((bool)LayerDragGrid.Instance)
                LayerDragGrid.Instance.info.Refresh();
        }

        TraitCrafter.MixType mixType = source.type.ToEnum<TraitCrafter.MixType>();
        Thing t = null;
        switch (mixType){
            case TraitCrafter.MixType.Food:
            case TraitCrafter.MixType.Resource:
            case TraitCrafter.MixType.Dye:
            case TraitCrafter.MixType.Butcher:
            case TraitCrafter.MixType.Grind:
            case TraitCrafter.MixType.Sculpture:
            case TraitCrafter.MixType.Talisman:
            case TraitCrafter.MixType.Scratch:
            case TraitCrafter.MixType.Incubator:
                break;
            default:
                //raitGodStatue.GetManiGene
                //Thing.GenerateGene
                //TraitDrinkMilkMother.OnDrink
                if (source.tag.Contains(BGMUtils.MillGeneTag)){
                    if (ai.ings[0].source != null){
                        //t = ThingGen.Create("gene");
                        var refcard = ai.ings[0].c_idRefCard;
                        //CardRow r = SpawnList.Get("chara").Select(100);
                        //Thing thing = DNA.GenerateGene(r, DNA.Type.Superior, owner.LV, owner.c_seed);
                        //Msg.Say(refcard);
                        //t.MakeRefFrom(refcard);
                        //DNA dna = new DNA();
                        Chara chara = CharaGen.Create(refcard,50 + EClass.pc.LV);
                        t = chara.MakeGene(DNA.Type.Superior);
                        //t.c_DNA = dna;
                        //dna.GenerateWithGene(dna.GetRandomType(), t, chara);
                        //t.c_DNA.GenerateWithGene(DNA.Type.Inferior, t);
                    }
                    __result = t;
                    return false;
                }
                break;
        }
        return true;
    }
}

[HarmonyPatch(typeof(Chara))]
[HarmonyPatch(nameof(Chara.MaxGeneSlot))]
[HarmonyPatch(MethodType.Getter)]
public static class Chara_MaxGene_Patch{
    public static bool Prefix(ref int __result){
        if(BGMUtils.MaxGeneCount.Value<=-1) return true;
        __result = BGMUtils.MaxGeneCount.Value;
        return false;
    }
}

[HarmonyPatch(typeof(Chara))]
[HarmonyPatch(nameof(Chara.MaxGeneSlot))]
[HarmonyPatch(MethodType.Getter)]
public static class Chara_MaxGene_post_Patch {
    public static void Postfix(ref int __result) {
        if (BGMUtils.MaxGeneCountMultiplier.Value == 1f) return;
        __result = (int)(__result * BGMUtils.MaxGeneCountMultiplier.Value);
    }
}



[HarmonyPatch(typeof(DNA))]
[HarmonyPatch(nameof(DNA.cost))]
[HarmonyPatch(MethodType.Getter)]
public static class DNA_cost_Patch{
    public static bool Prefix(DNA __instance,ref int __result){
        var mul = BGMUtils.GeneFeatMultiplier.Value;
        if(mul==1) return true;
        __result = Mathf.CeilToInt(mul * __instance.ints[1]);
        return false;
    }
}

[HarmonyPatch(typeof(DNA))]
[HarmonyPatch(nameof(DNA.CanRemove))]
public static class DNA_CanRemove_Patch{
    public static bool Prefix(DNA __instance,ref bool __result){
        if(BGMUtils.AllGeneCanRemove.Value){
            __result = true;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharaGenes))]
[HarmonyPatch(nameof(CharaGenes.Remove))]
[HarmonyPatch(new[] { typeof(Chara), typeof(DNA) })]
public static class CharaGenes_Remove_Patch{
    public static void Prefix(CharaGenes __instance, Chara c, DNA item){
        if (!BGMUtils.ReturnItemOnRemoval.Value)
            return;
        var t = BGMUtils.ReturnGene(item);
        EClass.pc.AddThing(t);
    }
}

[HarmonyPatch(typeof(DNA))]
[HarmonyPatch(nameof(DNA.slot))]
[HarmonyPatch(MethodType.Getter)]
public static class DNA_slot_Patch{
    public static void Postfix(DNA __instance,ref int __result){
        if(BGMUtils.GeneSlotMultiplier.Value==1) return;
        __result = Mathf.CeilToInt(__result * BGMUtils.GeneSlotMultiplier.Value);
    }
}

[HarmonyPatch(typeof(Card))]
[HarmonyPatch(nameof(Card.DamageHP))]
[HarmonyPatch(new[] { typeof(int),typeof(int),typeof(int),typeof(AttackSource),typeof(Card),typeof(bool) })]
public static class Card_DamageHP_Patch{
    public static bool Prefix(Card __instance, ref int dmg, int ele, int eleP, AttackSource attackSource, Card origin, bool showEffect){
        var modifyMetalDamageCalculation = BGMUtils.ModifyMetalDamageCalculation.Value;
        if(!modifyMetalDamageCalculation) return true;

        if (__instance.HasElement(1218) & modifyMetalDamageCalculation){
            float defp = 1000f / (1000 - __instance.Evalue(1218));
            dmg = (int)(dmg * (defp/10));
        }
        return true;
    }
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions){
        if(!BGMUtils.ModifyDefensiveInstinct.Value) return instructions;
        BetterGeneModification.Logger.LogInfo("BetterGeneModification Card_DamageHP_Patch Transpiler");
        //var codes = new List<CodeInstruction>(instructions);
        //foreach (var code in codes)
        //    BetterGeneModification.Logger.LogInfo(code.ToString());

        var matcher = new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_S, null),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Property(typeof(Chara), nameof(Chara.faith)).GetMethod),
                new CodeMatch(OpCodes.Call, AccessTools.Property(typeof(EClass), nameof(EClass.game)).GetMethod),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Game), nameof(Game.religions))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ReligionManager), nameof(ReligionManager.Healing))),
                new CodeMatch(OpCodes.Bne_Un,null))
            .RemoveInstructions(6);


        BetterGeneModification.Logger.LogInfo("BetterGeneModification Card_DamageHP_Patch Transpiler End");

        var outcodes = matcher.InstructionEnumeration();
        //foreach (var code in outcodes)
        //    BetterGeneModification.Logger.LogInfo(code.ToString());
        return outcodes;
    }
}


[HarmonyPatch(typeof(ElementContainer))]
[HarmonyPatch(nameof(ElementContainer.ListGeneFeats))]
public static class ElementContainer_ListGeneFeats_Patch{
    public static void Postfix(ElementContainer __instance, ref List<Element> __result){
        var allowBoostGeneDrop = BGMUtils.AllowBoostGeneDrop.Value;
        var allowRapidCastGeneDrop = BGMUtils.AllowRapidCastGeneDrop.Value;
        var allowRapidArrowGeneDrop = BGMUtils.AllowRapidArrowGeneDrop.Value;
        if(!allowBoostGeneDrop && !allowRapidCastGeneDrop && !allowRapidArrowGeneDrop) return;

        List<int> availabeFeats = new List<int>();
        if(allowBoostGeneDrop) availabeFeats.Add(1409);
        if(allowRapidCastGeneDrop) availabeFeats.Add(1648);
        if(allowRapidArrowGeneDrop) availabeFeats.Add(1652);

        var els = __instance.ListElements();
        foreach(var el in els){
            if(availabeFeats.Contains(el.id) && !__result.Any(rel => rel.id == el.id))
                __result.Add(el);
        }

        return;
    }
}

[HarmonyPatch(typeof(ElementContainerCard))]
[HarmonyPatch(nameof(ElementContainerCard.ValueBonus))]
[HarmonyPatch(new[] { typeof(Element) })]
public static class ElementContainerCard_ValueBonus_Patch{
    public static void Postfix(ElementContainerCard __instance, Element e, ref int __result){
        if (EClass.game == null)
            return;
        var modifyHardwareUpgrade = BGMUtils.ModifyHardwareUpgrade.Value;
        if(!modifyHardwareUpgrade) return;

        if (modifyHardwareUpgrade){
        if (__instance.owner.IsPCFactionOrMinion){
        if (!(__instance.owner.Chara.race.IsMachine || __instance.owner.id == "android") && e.id != 664 && e.id != 1217){
        if (__instance.owner.HasElement(1217)){
            int num4 = __instance.owner.Evalue(664);
            if (num4 > 0){
                switch (e.id){
                    case 64:
                    case 65:
                        __result += (e.ValueWithoutLink + e.vLink) * num4 / 2 / 100;
                        break;
                    case 79:
                        __result += (e.ValueWithoutLink + e.vLink) * num4 / 100;
                        break;
                }
            }
        }
        }
        }
        }
    }
}


[HarmonyPatch(typeof(DNA))]
[HarmonyPatch(nameof(DNA.GetDurationHour))]
public static class DNA_GetDurationHour_Patch{
    public static void Postfix(DNA __instance, ref int __result){
        if (BGMUtils.GeneSynthesisTimeMultiplier.Value==1) return;
        __result = Mathf.CeilToInt(__result * BGMUtils.GeneSynthesisTimeMultiplier.Value);
    }
}
