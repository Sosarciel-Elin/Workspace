using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;


[BepInPlugin("sosarciel.equipmodify", "EquipModify", "1.0.0.0")]
[BepInProcess("Elin.exe")]
public class EquipModify : BaseUnityPlugin {
    public static new ManualLogSource Logger;

    void Awake() {
        Logger = base.Logger;
        Initialize();
        var harmony = new Harmony("EquipModify");
        harmony.PatchAll();
        Logger.LogInfo("End Awake");
    }
    public void OnStartCore() {
        Logger.LogInfo("OnStartCore");
        var dir = Path.GetDirectoryName(Info.Location);
        var sources = Core.Instance.sources;

        var tableDir = dir + "/EquipModifyTable.xlsx";
        ModUtil.ImportExcel(tableDir, "things", sources.things);
		ModUtil.ImportExcel(tableDir, "recipes", sources.recipes);
        Logger.LogInfo("End OnStartCore");
    }
    void Start(){
        Logger.LogInfo("Start");
        Logger.LogInfo("Try trans to CN");
        if(Lang.langCode == Lang.LangCode.CN.ToString()){
            var things = Core.Instance.sources.things;

            var dispelPowder = things.GetRow(EMUtils.DispelPowderID);
            dispelPowder.name = "祛魔粉";
            dispelPowder.unit = "袋";
            dispelPowder.detail = "这个粉末用于清除装备的附魔。";

            var enchantGem = things.GetRow(EMUtils.EnchantGemID);
            enchantGem.name = "附魔石";
            enchantGem.unit = "个";
            enchantGem.detail = "用于给装备附魔的宝石。";
        }
        Logger.LogInfo("End Start");
    }
    void Initialize(){
        EMUtils.AllowEnchantRangedWeapons = base.Config.Bind<bool>("General", "AllowEnchantRangedWeapons", false,
            "允许附魔远程武器\nAllows enchanting ranged weapons");
        EMUtils.AllowEnchantThrowWeapons = base.Config.Bind<bool>("General", "AllowEnchantThrowWeapons", false,
            "允许附魔投掷武器\nAllows enchanting throw weapons");

        EMUtils.AllowEnchantFixedEquip = base.Config.Bind<bool>("General", "AllowEnchantFixedEquip", false,
"允许附魔固定装备\nAllows enchanting of fixed equipment");

        EMUtils.EnchantSlotLimitSuperior = base.Config.Bind<int>("General", "EnchantSlotLimitSuperior", 6, @"
设置奇迹 (Superior) 稀有度装备附魔的最大槽位数。设置为小于0表示无限。
Sets the maximum number of slots that can be enchanted for Superior rarity items. Setting it to less than 0 means unlimited.
".Trim());

        EMUtils.EnchantSlotLimitLegendary = base.Config.Bind<int>("General", "EnchantSlotLimitLegendary", 8, @"
设置神器 (Legendary) 及以上稀有度装备附魔的最大槽位数。设置为小于0表示无限。
Sets the maximum number of slots that can be enchanted for Legendary and above rarity items. Setting it to less than 0 means unlimited.
".Trim());

    }
}

public static class EMUtils{
    public static ConfigEntry<bool> AllowEnchantRangedWeapons;
    public static ConfigEntry<bool> AllowEnchantThrowWeapons;
    public static ConfigEntry<bool> AllowEnchantFixedEquip;
    public static ConfigEntry<int> EnchantSlotLimitSuperior;
    public static ConfigEntry<int> EnchantSlotLimitLegendary;

    public static string DispelPowderID = "sosarciel_dispel_powder";
    public static string EnchantGemID = "sosarciel_enchant_gem";
    public static string EjectEnchantTag = "sosarciel_equipmodify_ejectenchant";
    public static List<Element> ListEnchant(Thing t){
        //排除 DV PV DMG HIT
        List<int> excludedIds = new() { 64, 65, 66, 67 };
        return t.elements.ListElements(e =>
            !e.IsGlobalElement && e.vBase>0 &&(
            e.source.category == "skill" ||
            e.source.category == "enchant" ||
            e.source.category == "resist" ||
            e.source.category == "ability" ||
            (e.source.category == "attribute" && !excludedIds.Contains(e.source.id))));
    }

    public static void TryEjectEnchant(Thing t){
        //var _ = nameof(Thing.EjectSockets);

        //排除黑星
        if(IsFixedEquip(t) && !AllowEnchantFixedEquip.Value) return;

        //排除未分解的远程武器
        if(t.sockets!=null){
            for (int i = 0; i < t.sockets.Count; i++){
                int num = t.sockets[i];
                if (num != 0) return;
            }
        }

        var elementList = ListEnchant(t);

        elementList.ForEach(e=>{
            var enchLv = e.vBase;
            if(enchLv<=0) return;
            Thing thing = ThingGen.Create(t.isCopy ? "ash3" : EnchantGemID);
            if (!t.isCopy){
                thing.refVal = e.id;
                thing.encLV = enchLv;
                thing.noSell = true;
            }

            EClass._map.TrySmoothPick(t.pos.IsBlocked ? EClass.pc.pos : t.pos, thing, EClass.pc);
            t.elements.ModBase(thing.refVal, -thing.encLV);
        });
    }

    public static bool IsFixedEquip(Thing t){
        if(t.sourceCard.quality >= 4)
            return true;
        if(t.rarity > Rarity.Legendary)
            return true;
        if(t.source.tag.Contains("godArtifact"))
            return true;
        return false;
    }

    public static bool IsCanBeModify(Thing t){
        if (!t.IsMeleeWeapon && !t.IsEquipment && !t.IsRangedWeapon && !t.IsThrownWeapon)
            return false;

        if(t.IsRangedWeapon && !AllowEnchantRangedWeapons.Value)
            return false;

        if(t.IsRangedWeapon && !AllowEnchantThrowWeapons.Value)
            return false;
        if(IsFixedEquip(t) && !AllowEnchantFixedEquip.Value)
            return false;

        return true;
    }

    public static int GetEnchSlotCount(Thing t){
        if(t.rarity < Rarity.Superior) return 0;
        if(t.rarity == Rarity.Superior)
            return EnchantSlotLimitSuperior.Value < 0
                ? Int16.MaxValue : EnchantSlotLimitSuperior.Value;
        if(t.rarity >= Rarity.Legendary)
            return EnchantSlotLimitLegendary.Value < 0
                ? Int16.MaxValue : EnchantSlotLimitLegendary.Value;
        return 0;
    }
}


[HarmonyPatch(typeof(InvOwnerMod))]
[HarmonyPatch(nameof(InvOwnerMod.ShouldShowGuide))]
[HarmonyPatch(new [] { typeof(Thing)})]
public static class InvOwnerMod_ShouldShowGuide_Patch{
    private static bool Prefix(InvOwnerMod __instance, Thing t, ref bool __result){
        var owner = __instance.owner;
        if(owner.id != EMUtils.EnchantGemID) return true;

        var enchId = owner.refVal;
        var enchLv = owner.encLV;

        if(!EMUtils.IsCanBeModify(t)){
            __result = false;
            return false;
        }

        if(t.elements.Has(enchId) && t.elements.GetElement(enchId).vBase > 0){
            //原附魔值大于改造附魔值的物品
            if(t.elements.GetElement(enchId).vBase >= enchLv){
                __result = false;
                return false;
            }
            //原附魔是全局效果
            if(t.elements.GetElement(enchId).IsGlobalElement){
                __result = false;
                return false;
            }
        }

        //并非重复附魔且满槽位
        if( !t.elements.Has(enchId) ||
            (t.elements.Has(enchId) && t.elements.GetElement(enchId).vBase <= 0)){
            if(EMUtils.ListEnchant(t).Count >= EMUtils.GetEnchSlotCount(t)){
                __result = false;
                return false;
            }
        }

        __result = true;

        return false;
    }
}


[HarmonyPatch(typeof(InvOwnerMod))]
[HarmonyPatch(nameof(InvOwnerMod._OnProcess))]
[HarmonyPatch(new [] { typeof(Thing)})]
public static class InvOwnerMod__OnProcess_Patch{
    private static bool Prefix(InvOwnerMod __instance, Thing t){
        var owner = __instance.owner;
        if(owner.id != EMUtils.EnchantGemID) return true;

        SE.Play("reloaded");
        EClass.pc.PlayEffect("identify");
        Msg.Say("modded", t, owner);
        t.elements.SetBase(owner.refVal, owner.encLV);
        owner.Destroy();
        return false;
    }
}

[HarmonyPatch(typeof(Trait))]
[HarmonyPatch(nameof(Trait.OnBarter))]
class Trait_OnBarter_Patch {
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions){
        EquipModify.Logger.LogInfo("EquipModify Trait_OnBarter_Patch");
        var codes = new List<CodeInstruction>(instructions);
        //foreach (var code in codes)
        //    EquipModify.Logger.LogInfo(code.ToString());

        var matcher = new CodeMatcher(codes);

        // 手动获取正确的MethodInfo
        // 使用 Harmony 的 AccessTools 来获取局部函数
        var addThingMethod = AccessTools.DeclaredMethod(typeof(Trait), "<OnBarter>g__AddThing|349_1");
        if (addThingMethod == null) EquipModify.Logger.LogInfo("addThingMethod Failed");

        matcher
            .MatchForward(false,
                new (OpCodes.Pop),
                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldstr, "break_powder"),
                new (OpCodes.Call, AccessTools.Method(typeof(ThingGen), "CreateRecipe")),
                new (OpCodes.Ldc_I4, 1000),
                new (OpCodes.Callvirt, AccessTools.Method(typeof(Card), "SetPriceFix")),
                new (OpCodes.Ldloca_S, null),//new (OpCodes.Ldloca_S, 0),
                new (OpCodes.Call, null)//new (OpCodes.Call, typeof(Trait).GetMethod("<OnBarter>g__AddThing|349_1")),
            )
            .Advance(1)
            .InsertAndAdvance(
                //new (OpCodes.Pop),
                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldstr, EMUtils.DispelPowderID),
                new (OpCodes.Call, AccessTools.Method(typeof(ThingGen), "CreateRecipe")),
                new (OpCodes.Ldc_I4, 1000),
                new (OpCodes.Callvirt, AccessTools.Method(typeof(Card), "SetPriceFix")),
                new (OpCodes.Ldloca_S, 0),
                new (OpCodes.Call, addThingMethod)
            );

        return matcher.InstructionEnumeration();
    }
}


[HarmonyPatch(typeof(TraitCrafter))]
[HarmonyPatch(nameof(TraitCrafter.Craft))]
[HarmonyPatch(new [] { typeof(AI_UseCrafter) })]
class TraitCrafter_Craft_Patch {
	public static bool Prefix(TraitGrindstone __instance, AI_UseCrafter ai, ref Thing __result){
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
        switch (mixType){
            case TraitCrafter.MixType.Grind:
                if (source.tag.Contains(EMUtils.EjectEnchantTag)){
                    EMUtils.TryEjectEnchant(ai.ings[1]);
                    ai.ings[0].ModNum(-1);
                    return false;
                }
                break;
        }
        return true;
    }
}
