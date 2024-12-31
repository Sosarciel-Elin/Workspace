using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;


[BepInPlugin("sosarciel.curemutationbodypart", "CureMutationBodyPart", "1.0.0.0")]
public class CureMutationBodyPart : BaseUnityPlugin {

    private void Start() {
        var harmony = new Harmony("CureMutationBodyPart");
        harmony.PatchAll();
    }
}
public static class CMBPUtils {
    public static void RemoveBodyPart(CharaBody cb,BodySlot bp){
        var slots = cb.slots;

        int num = slots.FindIndex((BodySlot a) => a == bp);
        if (num != -1){
            BodySlot bodySlot = slots[num];
            if (bodySlot.thing != null)
                cb.Unequip(bodySlot);

            if (cb.slotMainHand == bodySlot)
                cb.slotMainHand = null;

            if (cb.slotOffHand == bodySlot)
                cb.slotOffHand = null;

            if (cb.slotRange == bodySlot)
                cb.slotRange = null;

            slots.RemoveAt(num);
        }

        ResetEquipIndex(cb);
    }
    public static void ResetEquipIndex(CharaBody cb){
        cb.slots.ForEach(bp=>{
            var index = cb.slots.FindIndex(s=>s==bp);
            if(index!=-1 && bp.thing!=null)
                bp.thing.c_equippedSlot = index+1;
        });
    }
    public static void SubMutateBodyFeat(Chara c){
        if(c.elements.GetElement(1644).Value>5){
            //大于5特殊处理
            c.elements.GetElement(60).vBase+=3;
            c.elements.GetElement(79).vBase+=5;
            c.elements.GetElement(77).vBase+=3;
        }
        c.elements.GetElement(1644).vBase--;
        c.feat+=1;
    }
}

[HarmonyPatch(typeof(Chara))]
[HarmonyPatch(nameof(Chara.MutateRandom))]
[HarmonyPatch(new[] { typeof(int), typeof(int), typeof(bool), typeof(BlessedState) })]
class MutateRandomPatch {
    static bool Prefix(Chara __instance,int vec, int tries, bool ether, BlessedState state,ref bool __result) {
        try{
            //Msg.Say(vec+" " + tries+" " + ether+" " + state);
            //含有变异体 正在删除变异
            if(__instance.elements.Has("featGrowParts") && vec<0){
                //Msg.Say("Has featGrowParts");
                IEnumerable<SourceElement.Row> ie = from a in EClass.sources.elements.rows
                    where a.category == (ether ? "ether" : "mutation")
                    select a;
                bool notHasDeletableMutation = ie.All(e=>!__instance.elements.Has(e.id));

                var bps = __instance.body.slots
                    .Where(bp=>bp.elementId!=45)//排除光源
                    .Where(bp=>bp.elementId!=44)//排除工具袋
                    .ToList();

                var vaildBpcount = bps.Count();

                //尝试保留1手1身
                var excludeIds = new List<int> { 35, 32 };
                bps = bps.Where(bp=>{
                    if(excludeIds.Contains(bp.elementId)){
                        excludeIds.Remove(bp.elementId);
                        return false;
                    }
                    return true;
                }).ToList();;

                //Msg.Say(string.Join(",", bpids));
                //Msg.Say(" ");
                //Msg.Say(notHasDeletableMutation+" "+vaildBpcount+" "+__instance.elements.Has(1644));

                //没有可治疗变异 至少有两个肢体 有变异肉体
                if (notHasDeletableMutation &&
                    vaildBpcount > 2 &&
                    __instance.elements.Has(1644)){

                    //优先尝试删除无装备肢体
                    var noEquipBps = bps.Where(bp=>bp.thing==null).ToList();
                    if(noEquipBps.Count()>0)
                        CMBPUtils.RemoveBodyPart(__instance.body,noEquipBps.RandomItem());
                    else __instance.body.RemoveBodyPart(bps.RandomItem().elementId);

                    //解除专长
                    CMBPUtils.SubMutateBodyFeat(__instance);
                    if (__instance.Chara.IsPC && WidgetEquip.Instance){
                        WidgetEquip.Instance.Rebuild();
                        EClass.ui.CloseLayers();
                        //EClass.core.ui = Util.Instantiate<UI>("UI/UI", (UnityEngine.Component)null);
                        if (EClass.ui.IsInventoryOpen)
                            EClass.ui.ToggleInventory(false);
                    }

                    __instance.Chara.body.RefreshBodyParts();

                    if (EClass.core.IsGameStarted && __instance.pos != null){
                        __instance.PlaySound(ether ? "mutation_ether" : "mutation");
                        __instance.PlayEffect("mutation");
                        Msg.SetColor(Msg.colors.MutateGood);
                        var text = (Lang.langCode == Lang.LangCode.CN.ToString())
                            ? "你的变异肢体被还原了。" : "Your mutated limb has been restored.";
                        Msg.Say(text);
                    }
                    __result = false;
                    return false;
                }
            }
        }catch(System.Exception e){
            Msg.Say("CureMutationBodyPart.MutateRandomPatch error: "+e.ToString());
        }
        return true;
    }
}

[HarmonyPatch(typeof(CharaBody))]
[HarmonyPatch(nameof(CharaBody.RemoveBodyPart))]
[HarmonyPatch(new[] { typeof(int) })]
class CharaBody_RemoveBodyPart_Patch {
    static void Postfix(CharaBody __instance,int ele){
        CMBPUtils.ResetEquipIndex(__instance);
    }
}

[HarmonyPatch(typeof(Feat))]
[HarmonyPatch(nameof(Feat.Apply))]
[HarmonyPatch(new[] { typeof(int), typeof(ElementContainer), typeof(bool) })]
class FeatApplyPatch{
    static void Postfix(Feat __instance,int a, ElementContainer owner, bool hint){
        //if (__instance.id==1644 && owner.Chara.IsPC && WidgetEquip.Instance){
        //    //Msg.Say("FeatApplyPatch.FeatApplyPatch");
        if (__instance.id==1644)
            owner.Chara.body.RefreshBodyParts();
        //}
    }
}