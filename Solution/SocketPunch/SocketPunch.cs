using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;


[BepInPlugin("sosarciel.socketpunch", "SocketPunch", "1.0.0.0")]
[BepInProcess("Elin.exe")]
public class SocketPunch : BaseUnityPlugin {
    public static new ManualLogSource Logger;

    void Awake() {
        Logger = base.Logger;
        Logger.LogInfo("Awake");
        Initialize();
        var harmony = new Harmony("SocketPunch");
        harmony.PatchAll();
        Logger.LogInfo("End Awake");
    }
    public void OnStartCore() {
        Logger.LogInfo("OnStartCore");
        var dir = Path.GetDirectoryName(Info.Location);
        var sources = Core.Instance.sources;

        var tableDir = dir + "/SocketPunchTable.xlsx";
        ModUtil.ImportExcel(tableDir, "things", sources.things);
		ModUtil.ImportExcel(tableDir, "recipes", sources.recipes);
        Logger.LogInfo("End OnStartCore");
    }
    void Start(){
        Logger.LogInfo("Start");
        Logger.LogInfo("Try trans to CN");
        if(Lang.langCode == Lang.LangCode.CN.ToString()){
            var things = Core.Instance.sources.things;

            var dispelPowder = things.GetRow(SPUtils.SocketPunchID);
            dispelPowder.name = "开孔工具";
            dispelPowder.unit = "个";
            dispelPowder.detail = "这是一种专为装备打孔的工具，允许你在装备上添加改造道具的插槽。";
        }
        Logger.LogInfo("End Start");
    }
    void Initialize(){
        SPUtils.AllowSocketForNoSocketWeapons = base.Config.Bind<bool>("General", "AllowSocketForNoSocketWeapons", false,
            "允许为无孔武器开孔\nAllows socketing for weapons without sockets");
        SPUtils.AllowSocketForEquipments = base.Config.Bind<bool>("General", "AllowSocketForEquipments", false,
            "允许为可穿戴装备开孔 (不包括近战武器) \nAllows socketing for wearable equipments (excluding melee weapons)");
        SPUtils.SocketSlotLimitSuperior = base.Config.Bind<int>("General", "SocketSlotLimitSuperior", 4, @"
设置优质品 (Superior) 稀有度装备开孔的最大槽位数。设置为小于0表示无限。
Sets the maximum number of slots that can be socketed for Superior rarity items. Setting it to less than 0 means unlimited.
".Trim());
        SPUtils.SocketSlotLimitLegendary = base.Config.Bind<int>("General", "SocketSlotLimitLegendary", 5, @"
设置奇迹 (Legendary) 稀有度装备开孔的最大槽位数。设置为小于0表示无限。
Sets the maximum number of slots that can be socketed for Legendary rarity items. Setting it to less than 0 means unlimited.
".Trim());
        SPUtils.SocketSlotLimitMythical = base.Config.Bind<int>("General", "SocketSlotLimitMythical", 6, @"
设置神器 (Mythical) 稀有度装备开孔的最大槽位数。设置为小于0表示无限。
Sets the maximum number of slots that can be socketed for Mythical rarity items. Setting it to less than 0 means unlimited.
".Trim());
        SPUtils.SocketSlotLimitArtifact = base.Config.Bind<int>("General", "SocketSlotLimitArtifact", 2, @"
设置黑星 (Artifact) 稀有度装备开孔的最大槽位数。设置为小于0表示无限。
Sets the maximum number of slots that can be socketed for Artifact rarity items. Setting it to less than 0 means unlimited.
".Trim());
        SPUtils.SocketSlotLimitNonRangedWeapons = base.Config.Bind<int>("General", "SocketSlotLimitNonRangedWeapons", 2, @"
设置非远程武器 (包括近战武器和投掷武器) 开孔的最大槽位数。设置为小于0表示无限。
此值会在稀有度约束与类型约束之间取最低值。
Sets the maximum number of slots that can be socketed for non-ranged weapons (including melee and thrown weapons). Setting it to less than 0 means unlimited.
This value will take the minimum between rarity constraints and catrgory constraints.
".Trim());
        SPUtils.SocketSlotLimitWearableEquipments = base.Config.Bind<int>("General", "SocketSlotLimitWearableEquipments", 1, @"
设置穿戴装备 (不包括近战武器) 开孔的最大槽位数。设置为小于0表示无限。
此值会在稀有度约束与类型约束之间取最低值。
Sets the maximum number of slots that can be socketed for wearable equipments (excluding melee weapons). Setting it to less than 0 means unlimited.
This value will take the minimum between rarity constraints and catrgory constraints.
".Trim());
    }
}

public static class SPUtils{
    public static ConfigEntry<bool> AllowSocketForNoSocketWeapons;
    public static ConfigEntry<bool> AllowSocketForEquipments;
    public static ConfigEntry<int> SocketSlotLimitSuperior;
    public static ConfigEntry<int> SocketSlotLimitLegendary;
    public static ConfigEntry<int> SocketSlotLimitMythical;
    public static ConfigEntry<int> SocketSlotLimitArtifact;
    public static ConfigEntry<int> SocketSlotLimitNonRangedWeapons;
    public static ConfigEntry<int> SocketSlotLimitWearableEquipments;

    public static string SocketPunchID = "sosarciel_socket_punch";

    public static int GetEnchSlotCountForRarity(Thing t){
        if(t.rarity < Rarity.Superior) return 0;
        if(t.rarity == Rarity.Superior)
            return SocketSlotLimitSuperior.Value < 0
                ? Int16.MaxValue : SocketSlotLimitSuperior.Value;
        if(t.rarity == Rarity.Legendary)
            return SocketSlotLimitLegendary.Value < 0
                ? Int16.MaxValue : SocketSlotLimitLegendary.Value;
        if(t.rarity == Rarity.Mythical)
            return SocketSlotLimitMythical.Value < 0
                ? Int16.MaxValue : SocketSlotLimitMythical.Value;
        if(t.rarity >= Rarity.Mythical)
            return SocketSlotLimitArtifact.Value < 0
                ? Int16.MaxValue : SocketSlotLimitArtifact.Value;
        return 0;
    }
    public static int GetEnchSlotCount(Thing t){
        int baseVal = GetEnchSlotCountForRarity(t);
        if(t.IsRangedWeapon && !t.IsMeleeWeapon) return baseVal;
        if(t.IsMeleeWeapon || t.IsThrownWeapon)
            return Math.Min(baseVal, SocketSlotLimitNonRangedWeapons.Value);
        if(t.IsEquipment)
            Math.Min(baseVal, SocketSlotLimitWearableEquipments.Value);
        return 0;
    }
    public static bool CanPunchSocket(Thing t){
        if(!t.IsMeleeWeapon && !t.IsRangedWeapon && !t.IsThrownWeapon && !t.IsEquipment)
            return false;
        if(t.IsEquipment && !t.IsMeleeWeapon && !SPUtils.AllowSocketForEquipments.Value)
            return false;
        if(t.sockets==null && !SPUtils.AllowSocketForNoSocketWeapons.Value)
            return false;
        if((GetEnchSlotCount(t) - (t.sockets==null ? 0 : t.sockets.Count))<=0)
            return false;
        return true;
    }
    public static void TryPunchSocket(Thing t){
        if(!CanPunchSocket(t))
            return;
        t.AddSocket();
    }
}

[HarmonyPatch(typeof(Trait))]
[HarmonyPatch(nameof(Trait.OnBarter))]
class Trait_OnBarter_Patch {
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions){
        try{
        SocketPunch.Logger.LogInfo("SocketPunch Trait_OnBarter_Patch");
        var codes = new List<CodeInstruction>(instructions);
        //foreach (var code in codes)
        //    SocketPunch.Logger.LogInfo(code.ToString());

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

        SocketPunch.Logger.LogInfo("match to: "+extractedMethodName);
        var addThingMethod = AccessTools.DeclaredMethod(typeof(Trait), extractedMethodName);
        if (addThingMethod == null) SocketPunch.Logger.LogInfo("DeclaredMethod addThingMethod Failed");
        #endregion

        #region mod il
        var matcher = new CodeMatcher(codes);
        matcher
            .MatchForward(false,
                //new (OpCodes.Pop),
                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldstr, "ic"),
                new (OpCodes.Call, AccessTools.Method(typeof(ThingGen), nameof(ThingGen.CreateRecipe))),
                new (OpCodes.Ldc_I4, null),
                new (OpCodes.Callvirt, AccessTools.Method(typeof(Card), nameof(Card.SetPriceFix))),
                new (OpCodes.Ldloca_S, null),//new (OpCodes.Ldloca_S, 0),
                new (OpCodes.Call, null)//new (OpCodes.Call, typeof(Trait).GetMethod("<OnBarter>g__AddThing|349_1")),
            );

        //matcher.MatchForward(false, new CodeMatch(OpCodes.Call, null));
        //var addThingMethodInfo = matcher.Operand as MethodInfo;
        //if (addThingMethodInfo == null)
        //    SocketPunch.Logger.LogInfo("Match addThingMethodInfo Failed");
        //string methodName = addThingMethodInfo.Name;

        matcher.Advance(1)
            .InsertAndAdvance(
                //new (OpCodes.Pop),
                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldstr, SPUtils.SocketPunchID),
                new (OpCodes.Call, AccessTools.Method(typeof(ThingGen), nameof(ThingGen.CreateRecipe))),
                new (OpCodes.Ldc_I4, 1000),
                new (OpCodes.Callvirt, AccessTools.Method(typeof(Card), nameof(Card.SetPriceFix))),
                new (OpCodes.Ldloca_S, 0),
                new (OpCodes.Call, addThingMethod)
            );
        #endregion

        return matcher.InstructionEnumeration();
        }catch(Exception e){
            SocketPunch.Logger.LogError(e);
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
                if (source.tag.Contains(SPUtils.SocketPunchID)){
                    SPUtils.TryPunchSocket(ai.ings[1]);
                    ai.ings[0].ModNum(-1);
                    return false;
                }
                break;
        }
        return true;
    }
}
