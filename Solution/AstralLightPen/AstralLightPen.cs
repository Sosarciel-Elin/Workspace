using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

[BepInPlugin("sosarciel.astrallightpen", "AstralLightPen", "1.0.0.0")]
public class AstralLightPen : BaseUnityPlugin{
    public static new ManualLogSource Logger;
    public void Awake(){
        Logger = base.Logger;
        Initialize();
        var harmony = new Harmony("AstralLightPen");
        harmony.PatchAll();
        AstralLightPen.Logger.LogInfo("Awake");
    }
    public void OnStartCore() {
        var dir = Path.GetDirectoryName(Info.Location);
        var excel = dir + "/Item/AstralLightPenThing.xlsx";
        var sources = Core.Instance.sources;
        ModUtil.ImportExcel(excel, "Thing", sources.things);
    }
    void Start(){
        AstralLightPen.Logger.LogInfo("Start");
        AstralLightPen.Logger.LogInfo("Try trans to CN");
        if(Lang.langCode == Lang.LangCode.CN.ToString()){
            var sources = Core.Instance.sources.things;
            var row = sources.GetRow("sosarciel_astral_light_pen");
            row.name = "星芒光笔";
            row.unit = "支";
            row.detail = "带着星光的羽毛笔。对这个世界的生命存在记录——阿卡夏记录造成干涉，可将对象的记录重写为具有另一个完全相同的存在。该存在具有相同的记忆以及人格，同时可以从原本的责任以及使命当中解放。但是，要求使用者最低也要高于对象一般的等级。对应对象级别的魔法墨水是必不可少的，不仅如此，如果对象并没有从心里期待这样，同样也会失败。";
        }
    }
    void Initialize(){
        ALPUtils.TargetAffinity = base.Config.Bind<int>("General", "TargetAffinity", 200,
            "要求目标好感度\nRequires Target Affinity");
        ALPUtils.PlayerLevelHalfTarget = base.Config.Bind<bool>("General", "PlayerLevelHalfTarget", true,
            "要求玩家等级达到目标的一半\nRequires Player Level to be Half of Target");
        ALPUtils.MultiplicationFactor = base.Config.Bind<float>("General", "MultiplicationFactor", 1,@"
指定用于计算复制后目标属性与技能的乘数因子, 默认为1。
这意味着属性值和技能值将通过 attr * m 公式进行初步处理, 其中 m 是这个乘数因子。
Specifies the multiplication factor used for calculating the duplicated target's attributes and skills, default is 1.
This means the values are initially processed using the formula attr * m, where m is this multiplier.
".Trim());
        ALPUtils.PowExponent = base.Config.Bind<float>("General", "PowExponent", 0.5f,@"
指定用于计算复制后目标属性与技能的幂指数, 默认为0.5。
这意味着属性值和技能值将通过 pow(attr, p) 公式计算, 其中 p 是这个幂指数。
Specifies the exponent used for calculating the duplicated target's attributes and skills, default is 0.5.
This means the values are calculated using the formula pow(attr, p), where p is this exponent.
".Trim());
        ALPUtils.ClampFeatLevel = base.Config.Bind<bool>("General", "ClampFeatLevel", true,@"
指定当专长等级超过最大值时是否将其调整为最大值, 默认为true。
如果启用, 超过最大值的专长等级将被钳制到允许的最大值。
Specifies whether to clamp the feat level to its maximum value if it exceeds the maximum, default is true.
If enabled, the feat level that exceeds the maximum will be clamped to the allowed maximum value.
".Trim());
        ALPUtils.DecayThresholdLevel = base.Config.Bind<int>("General", "DecayThresholdLevel", 300, @"
指定复制原目标等级达到多少之后才会应用衰减算法, 默认为300。
这意味着当目标等级小于该值时, 不会应用上面的属性和技能的衰减算法。
Specifies the threshold level at which the decay algorithm will be applied to the duplicated target's attributes and skills, default is 300.
This means that the decay algorithm for attributes and skills will not be applied if the target's level is below this value.
".Trim());
    }
}

public static class ALPUtils{
    public static ConfigEntry<int> TargetAffinity;
    public static ConfigEntry<bool> PlayerLevelHalfTarget;
    public static ConfigEntry<float> MultiplicationFactor;
    public static ConfigEntry<float> PowExponent;
    public static ConfigEntry<bool> ClampFeatLevel;
    public static ConfigEntry<int> DecayThresholdLevel;

    public static void ModAttr(Chara c){
        if(c.LV<ALPUtils.DecayThresholdLevel.Value) return;

        var mul = MultiplicationFactor.Value;
        var pow = PowExponent.Value;
        var clamp = ClampFeatLevel.Value;
        Func<int, int> mattr = (int val) =>{
            var sign = Math.Sign(val);
            var abs = Math.Abs(val);
            var calc = (int)Math.Pow(abs * mul, pow);
            return calc*sign;
        };

        c.LV = mattr(c.LV);

        var baseAttrs = c.elements.ListElements((Element e) =>
            e.source.category == "skill" ||
            (e.source.category == "attribute" && e.source.tag.Contains("primary")));//||
            //(e.source.category =="feat" && e.source.categorySub == "god"));
            //e.source.category == "attribute");
        baseAttrs.ForEach(e => {
            //Msg.Say("value Base " + e.source.id + " " + e.source.name + " is " + e.vBase + "; ");
            //Msg.Say("value Exp  " + e.source.id + " " + e.source.name + " is " + e.vExp + "; ");
            //Msg.Say("value Pote " + e.source.id + " " + e.source.name + " is " + e.vPotential + "; ");
            //Msg.Say("value Temp " + e.source.id + " " + e.source.name + " is " + e.vTempPotential + "; ");
            //Msg.Say("value Link " + e.source.id + " " + e.source.name + " is " + e.vLink + "; ");
            //Msg.Say("value Sour " + e.source.id + " " + e.source.name + " is " + e.vSource + "; ");
            //Msg.Say("value Sour " + e.source.id + " " + e.source.name + " is " + e.vSourcePotential + "; ");
            //Msg.Say("value Eval " + e.source.id + " " + e.source.name + " eval is " + c.Evalue(e.source.id) + "; ");
            //Msg.Say("value Valu " + e.source.id + " " + e.source.name + " eval is " + e.Value + "; ");
            //Msg.Say("value Valu " + e.source.id + " " + e.source.name + " eval is " + e.ValueWithoutLink + "; ");
            e.vSource = mattr(e.vSource);
            e.vBase = mattr(e.vBase);
        });

        if (clamp) {
            var feats = c.elements.ListElements((Element e)=>
                e.source.category =="feat");
            feats.ForEach(f=>{
                var max = f.source.max;
                if(max < 0) return;
                f.vBase = f.vBase > max
                    ? max : f.vBase;
            });
        }

        c.RefreshFaithElement();
        c.Refresh();
        c.CalculateMaxStamina();
    }
}

[HarmonyPatch(typeof(TraitStethoscope))]
[HarmonyPatch(nameof(TraitStethoscope.TrySetHeldAct))]
[HarmonyPatch(new[] { typeof(ActPlan)})]
public static class TraitStethoscope_TrySetHeldAct_Patch{
    private static bool Prefix(TraitStethoscope __instance, ActPlan p){
        var owner = __instance.owner;
        if(owner.id != "sosarciel_astral_light_pen") return true;
        var reqLvl = ALPUtils.PlayerLevelHalfTarget.Value;
        var reqAff = ALPUtils.TargetAffinity.Value;

        p.pos.ListCards().ForEach(delegate (Card a){
            Chara c = a.Chara;
            if (c != null && p.IsSelfOrNeighbor && EClass.pc.CanSee(a) && c != EClass.pc){
            if( (!reqLvl || EClass.pc.LV >= c.LV/2) && c._affinity >= reqAff ){
                p.TrySetAct("actInvestigate", delegate{
                    EClass.pc.Say("use_scope", c, owner);
                    EClass.pc.Say("use_scope2", c);
                    c.Talk("pervert2");

                    var cloneChar = c.Duplicate();
                    foreach (KeyValuePair<int, Element> item in c.elements.dict){
                        Element element = cloneChar.elements.GetElement(item.Key);
                        if (element != null){
                            element.vBase = item.Value.vBase;
                            element.vSource = item.Value.vSource;
                        }
                    }
                    cloneChar._affinity = c._affinity;
                    ALPUtils.ModAttr(cloneChar);
                    //cloneChar.id = "salpc"+cloneChar.id;
                    //cloneChar.quest
                    EClass._zone.AddCard(cloneChar,c.pos);
                    cloneChar.MakePartyMemeber();

                    EClass.pc.Say("spellbookCrumble", owner);
                    owner.Destroy();

                    return false;
                }, c, null, 1, isHostileAct: false, localAct: true, canRepeat: false);
            }}
        });

        return false;
    }
}

[HarmonyPatch(typeof(TraitStethoscope))]
[HarmonyPatch(nameof(TraitStethoscope.HasCharges))]
[HarmonyPatch(MethodType.Getter)]
public class TraitStethoscope_HasCharges_Patch{
    public static bool Prefix(TraitStethoscope __instance, ref bool __result){
        if(__instance.owner.id != "sosarciel_astral_light_pen") return true;
        __result = false;
        return false;
    }
}