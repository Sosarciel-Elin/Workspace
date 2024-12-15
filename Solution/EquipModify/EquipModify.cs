using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
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
        EMUtils.AllowSellEnchantmentGems = base.Config.Bind<bool>("General", "AllowSellEnchantmentGems", true,
            "允许出售附魔石\nAllows selling enchantment gems");

        EMUtils.AllowEnchantThrowWeapons = base.Config.Bind<bool>("General", "AllowEnchantThrowWeapons", false,
            "允许附魔投掷武器\nAllows enchanting throw weapons");

        EMUtils.AllowEnchantRangedWeapons = base.Config.Bind<bool>("General", "AllowEnchantRangedWeapons", false,
            "允许附魔远程武器\nAllows enchanting ranged weapons");

        EMUtils.AllowEnchantThrowWeapons = base.Config.Bind<bool>("General", "AllowEnchantThrowWeapons", false,
            "允许附魔投掷武器\nAllows enchanting throw weapons");

        EMUtils.AllowEnchantFixedEquip = base.Config.Bind<bool>("General", "AllowEnchantFixedEquip", false,
"允许附魔固定装备\nAllows enchanting of fixed equipment");

        EMUtils.EnchantSlotLimitSuperior = base.Config.Bind<int>("General", "EnchantSlotLimitSuperior", 3, @"
设置优质品 (Superior) 稀有度装备附魔的最大槽位数。设置为小于0表示无限。
Sets the maximum number of slots that can be enchanted for Superior rarity items. Setting it to less than 0 means unlimited.
".Trim());

        EMUtils.EnchantSlotLimitLegendary = base.Config.Bind<int>("General", "EnchantSlotLimitLegendary", 4, @"
设置奇迹 (Legendary) 稀有度装备附魔的最大槽位数。设置为小于0表示无限。
Sets the maximum number of slots that can be enchanted for Legendary rarity items. Setting it to less than 0 means unlimited.
".Trim());

        EMUtils.EnchantSlotLimitMythical = base.Config.Bind<int>("General", "EnchantSlotLimitMythical", 5, @"
设置神器 (Mythical) 及以上稀有度装备附魔的最大槽位数。设置为小于0表示无限。
Sets the maximum number of slots that can be enchanted for Mythical and above rarity items. Setting it to less than 0 means unlimited.
".Trim());
    }
}

public static class EMUtils{
    public static ConfigEntry<bool> AllowSellEnchantmentGems;
    public static ConfigEntry<bool> AllowEnchantRangedWeapons;
    public static ConfigEntry<bool> AllowEnchantThrowWeapons;
    public static ConfigEntry<bool> AllowEnchantFixedEquip;
    public static ConfigEntry<int> EnchantSlotLimitSuperior;
    public static ConfigEntry<int> EnchantSlotLimitLegendary;
    public static ConfigEntry<int> EnchantSlotLimitMythical;

    public static string DispelPowderID = "sosarciel_dispel_powder";
    public static string EnchantGemID = "sosarciel_enchant_gem";
    public static string EjectEnchantTag = "sosarciel_equipmodify_ejectenchant";
    public static List<Element> ListEnchant(Thing t){
        //排除 DV PV DMG HIT
        List<int> excludedIds = new() { 64, 65, 66, 67 };
        return t.elements.ListElements(e =>
            !e.IsGlobalElement &&
            GetEnchLvExcludeSocket(t,e.id)>0 && (
            e.source.category == "skill" ||
            e.source.category == "enchant" ||
            e.source.category == "resist" ||
            e.source.category == "ability" ||
            (e.source.category == "attribute" && !excludedIds.Contains(e.source.id))));
    }

    public static bool TryEjectEnchant(Thing t){
        //var _ = nameof(Thing.EjectSockets);

        //排除黑星
        if(IsFixedEquip(t) && !AllowEnchantFixedEquip.Value) return false;

        var ejected = false;
        var elementList = ListEnchant(t);

        elementList.ForEach(e=>{
            var enchId = e.id;
            var enchLv = GetEnchLvExcludeSocket(t, enchId);

            if(enchLv<=0) return;
            Thing thing = ThingGen.Create(t.isCopy ? "ash3" : EnchantGemID);
            if (!t.isCopy){
                thing.refVal = enchId;
                thing.encLV = enchLv;
                if(!AllowSellEnchantmentGems.Value)
                    thing.noSell = true;
            }

            EClass._map.TrySmoothPick(t.pos.IsBlocked ? EClass.pc.pos : t.pos, thing, EClass.pc);
            t.elements.ModBase(thing.refVal, -thing.encLV);
            ejected = true;
        });
        return ejected;
    }

    public static bool IsFixedEquip(Thing t){
        if(t.sourceCard.quality >= 4)
            return true;
        if(t.rarity > Rarity.Mythical)
            return true;
        if(t.source.tag.Contains("godArtifact"))
            return true;
        return false;
    }

    public static bool IsCanBeModify(Thing t){
        if (!t.IsMeleeWeapon && !t.IsEquipment && !t.IsRangedWeapon && !t.IsThrownWeapon)
            return false;

        if(t.IsRangedWeapon && !t.IsMeleeWeapon && !AllowEnchantRangedWeapons.Value)
            return false;

        if(t.IsThrownWeapon && !AllowEnchantThrowWeapons.Value)
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
        if(t.rarity == Rarity.Legendary)
            return EnchantSlotLimitLegendary.Value < 0
                ? Int16.MaxValue : EnchantSlotLimitLegendary.Value;
        if(t.rarity >= Rarity.Mythical)
            return EnchantSlotLimitMythical.Value < 0
                ? Int16.MaxValue : EnchantSlotLimitMythical.Value;
        return 0;
    }

    public static Dictionary<int,int> GetSocketEnch(Thing t){
        var sockets = t.sockets;
        //var _ = nameof(Thing.EjectSockets);
        Dictionary<int,int> enchMap = new();
        if(sockets==null) return enchMap;
        for (int i = 0; i < sockets.Count; i++){
            int num = sockets[i];
            if (num != 0){
                int enchId = num / 100;
                int enchLv = num % 100;
                if(!enchMap.ContainsKey(enchId))
                    enchMap.Add(enchId, enchLv);
                else enchMap[enchId] += enchLv;
            }
        }
        return enchMap;
    }
    public static int GetEnchLvExcludeSocket(Thing t, int enchId){
        //var _ = nameof(AttackProcess.Perform);
        var enchMap = GetSocketEnch(t);
        var vbase = t.elements.Has(enchId)
            ? t.elements.GetElement(enchId).vBase : 0;
        if(enchMap.ContainsKey(enchId))
            return vbase - enchMap[enchId];
        return vbase;
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

        if(t.elements.Has(enchId) && EMUtils.GetEnchLvExcludeSocket(t,enchId) > 0){
            //原附魔值大于改造附魔值的物品
            if(EMUtils.GetEnchLvExcludeSocket(t,enchId) >= enchLv){
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
            (t.elements.Has(enchId) && EMUtils.GetEnchLvExcludeSocket(t,enchId) <= 0)){
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
        var enchId = owner.refVal;
        var enchLv = owner.encLV + EMUtils.GetEnchLvExcludeSocket(t,owner.refVal);

        t.elements.SetBase(enchId, enchLv);
        owner.Destroy();
        return false;
    }
}

[HarmonyPatch(typeof(Trait))]
[HarmonyPatch(nameof(Trait.OnBarter))]
class Trait_OnBarter_Patch {
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions){
        try{
        EquipModify.Logger.LogInfo("EquipModify Trait_OnBarter_Patch");
        var codes = new List<CodeInstruction>(instructions);
        //foreach (var code in codes)
        //    EquipModify.Logger.LogInfo(code.ToString());

        #region get token
        List<string> codeStrings = codes.Select(code => code.ToString()).ToList();

        string methodPattern = @"<OnBarter>g__AddThing";
        Regex methodRegex = new(methodPattern);

        string signaturePattern = @"Trait::(.+)\(";
        Regex signatureRegex = new(signaturePattern);

        string extractedMethodName = codeStrings
            .Where(codeString => methodRegex.IsMatch(codeString))
            .Select(codeString => signatureRegex.Match(codeString))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .FirstOrDefault();

        EquipModify.Logger.LogInfo("match to: "+extractedMethodName);
        var addThingMethod = AccessTools.DeclaredMethod(typeof(Trait), extractedMethodName);
        if (addThingMethod == null) EquipModify.Logger.LogInfo("DeclaredMethod addThingMethod Failed");
        #endregion

        #region mod il
        var matcher = new CodeMatcher(codes);
        matcher
            .MatchForward(false,
                //new (OpCodes.Pop),
                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldstr, "break_powder"),
                new (OpCodes.Call, AccessTools.Method(typeof(ThingGen), nameof(ThingGen.CreateRecipe))),
                new (OpCodes.Ldc_I4, 1000),
                new (OpCodes.Callvirt, AccessTools.Method(typeof(Card), nameof(Card.SetPriceFix))),
                new (OpCodes.Ldloca_S, null),//new (OpCodes.Ldloca_S, 0),
                new (OpCodes.Call, null)//new (OpCodes.Call, typeof(Trait).GetMethod("<OnBarter>g__AddThing|349_1")),
            );

        //matcher.MatchForward(false, new CodeMatch(OpCodes.Call, null));
        //var addThingMethodInfo = matcher.Operand as MethodInfo;
        //if (addThingMethodInfo == null)
        //    EquipModify.Logger.LogInfo("Match addThingMethodInfo Failed");
        //string methodName = addThingMethodInfo.Name;

        matcher.Advance(1)
            .InsertAndAdvance(
                //new (OpCodes.Pop),
                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldstr, EMUtils.DispelPowderID),
                new (OpCodes.Call, AccessTools.Method(typeof(ThingGen), nameof(ThingGen.CreateRecipe))),
                new (OpCodes.Ldc_I4, 1000),
                new (OpCodes.Callvirt, AccessTools.Method(typeof(Card), nameof(Card.SetPriceFix))),
                new (OpCodes.Ldloca_S, 0),
                new (OpCodes.Call, addThingMethod)
            );
        #endregion

        return matcher.InstructionEnumeration();
        }catch(Exception e){
            EquipModify.Logger.LogError(e);
            return instructions;
        }
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
                    if(EMUtils.TryEjectEnchant(ai.ings[1])){
                        var copyT = ai.ings[0].Duplicate(1);
                        EClass._map.TrySmoothPick(copyT.pos.IsBlocked ? EClass.pc.pos : copyT.pos, copyT, EClass.pc);
                    }
                    ai.ings[0].ModNum(-1);
                    return false;
                }
                break;
        }
        return true;
    }
}
