using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[BepInPlugin("sosarciel.removefaithlimitforfactionequipmentpersonalversion", "RemoveFaithLimitForFactionEquipmentPersonalVersion", "1.0.0.0")]
public class RemoveFaithLimitForFactionEquipmentPersonalVersion : BaseUnityPlugin {
	private void Start() {
		var harmony = new Harmony("RemoveFaithLimitForFactionEquipmentPersonalVersion");
		harmony.PatchAll();
	}
}

//public bool IsGlobalElement{
//	get{
//		return this.vExp == -1 || this.vExp == -2;
//	}
//}
//public bool IsFactionWideElement{
//	get{
//		return this.vExp == -1;
//	}
//}
//public bool IsPartyWideElement{
//	get{
//		return this.vExp == -2;
//	}
//}


static class RFLFFEPVUtil{
    static public void ProcThing(Thing thing,int vExp){
        //Religion r
        //r.Reforge
        if(!thing.HasTag(CTAG.godArtifact)) return;

        foreach (Element value in thing.elements.dict.Values){
            if (value.id == 66 || value.id == 67 || value.id == 64 || value.id == 65 || value.id == 92)
                continue;
            switch (thing.id.ToLower()){
                case "cloak_mani":
                    if (value.id == 427 || value.id == 957 || value.id == 105 || value.id == 466 || value.id == 664)
                        value.vExp = vExp;
                    break;
                case "scythe_kumi":
                    if (value.id == 6650 || value.id == 480 || value.id == 959 || value.id == 428 || value.id == 640 || value.id == 665)
                        value.vExp = vExp;
                    break;
                case "blunt_earth":
                    if (value.id == 70 || value.id == 55 || value.id == 56 || value.id == 954 || value.id == 423 || value.id == 421)
                        value.vExp = vExp;
                    break;
                case "luckydagger":
                    if (value.id != 426)
                        value.vExp = vExp;
                    break;
                case "staff_element":
                    if (value.id == 411 || (value is Resistance && value.id != 959))
                        value.vExp = vExp;
                    break;
                case "shirt_wind":
                    if (!(value is Resistance) && value.id != 226 && value.id != 152 && value.id != 77)
                        value.vExp = vExp;
                    break;
                case "pole_holy":
                    if (value.id == 60 || value.id == 461 || value.id == 423)
                        value.vExp = vExp;
                    break;
                case "sword_muramasa2":
                    if (value.id == 401 || value.id == 916 || value.id == 661)
                        value.vExp = vExp;
                    break;
                case "kogitsunemaru":
                    if (value.id != 656)
                        value.vExp = vExp;
                    break;
                case "warmonger":
                    if (value.id == 423 || value.id == 463 || value.id == 460 || value.id == 464 || value.id == 465)
                        value.vExp = vExp;
                    break;
            }
        }
    }
}

[HarmonyPatch(typeof(ElementContainer))]
[HarmonyPatch(nameof(ElementContainer.AddNote))]
[HarmonyPatch(new [] { typeof(UINote), typeof(Func<Element, bool>), typeof(Action), typeof(ElementContainer.NoteMode), typeof(bool), typeof(Func<Element, string, string>), typeof(Action<UINote, Element>)})]
class ElementContainer_AddNote_Patch_PV {
	public static void Postfix(ElementContainer __instance, UINote n, Func<Element, bool> isValid, Action onAdd, ElementContainer.NoteMode mode, bool addRaceFeat, Func<Element, string, string> funcText, Action<UINote, Element> onAddNote){
		//Msg.Say("ElementContainer_AddNote_Patch Post Patch");
		List<Element> list = new List<Element>();
        foreach (Element value2 in __instance.dict.Values){
            if ((isValid == null || isValid(value2)) && (mode != ElementContainer.NoteMode.CharaMake || value2.ValueWithoutLink != 0) && (value2.Value != 0 || mode == ElementContainer.NoteMode.CharaMakeAttributes) && (!value2.HasTag("hidden") || EClass.debug.showExtra)){
                list.Add(value2);
            }
        }

        if (addRaceFeat){
            Element element = Element.Create(29, 1);
            element.owner = __instance;
            list.Add(element);
        }

        if (list.Count == 0) return;

        switch (mode){
            case ElementContainer.NoteMode.CharaMake:
            case ElementContainer.NoteMode.CharaMakeAttributes:
                list.Sort((Element a, Element b) => a.GetSortVal(UIList.SortMode.ByElementParent) - b.GetSortVal(UIList.SortMode.ByElementParent));
                break;
            case ElementContainer.NoteMode.Trait:
                list.Sort((Element a, Element b) => ElementContainer.GetSortVal(b) - ElementContainer.GetSortVal(a));
                break;
            default:
                list.Sort((Element a, Element b) => a.SortVal() - b.SortVal());
                break;
        }

        string text = "";
        foreach (Element e in list){
			if (!e.IsGlobalElement) continue;
            switch (mode){
				case ElementContainer.NoteMode.Domain:
					continue;
                case ElementContainer.NoteMode.Default:
                case ElementContainer.NoteMode.Trait:
                    {
                        bool flag = e.source.tag.Contains("common");
                        string categorySub = e.source.categorySub;
                        bool flag2 = false;
                        bool flag3 = (e.source.tag.Contains("neg") ? (e.Value > 0) : (e.Value < 0));
                        int num = Mathf.Abs(e.Value);
                        bool flag4 = __instance.Card != null && __instance.Card.ShowFoodEnc;
                        bool flag5 = __instance.Card != null && __instance.Card.IsWeapon && e is Ability;
                        if (e.IsTrait || (flag4 && e.IsFoodTrait))
                        {
                            string[] textArray = e.source.GetTextArray("textAlt");
                            int num2 = Mathf.Clamp(e.Value / 10 + 1, (e.Value < 0 || textArray.Length <= 2) ? 1 : 2, textArray.Length - 1);
                            text = "altEnc".lang(textArray[0].IsEmpty(e.Name), textArray[num2], EClass.debug.showExtra ? (e.Value + " " + e.Name) : "");
                            flag3 = num2 <= 1 || textArray.Length <= 2;
                            flag2 = true;
                        }
                        else if (flag5)
                        {
                            text = "isProc".lang(e.Name);
                            flag3 = false;
                        }
                        else if (categorySub == "resist")
                        {
                            text = ("isResist" + (flag3 ? "Neg" : "")).lang(e.Name);
                        }
                        else if (categorySub == "eleAttack")
                        {
                            text = "isEleAttack".lang(e.Name);
                        }
                        else if (!e.source.textPhase.IsEmpty() && e.Value > 0)
                        {
                            text = e.source.GetText("textPhase");
                        }
                        else
                        {
                            string name = e.Name;
                            bool flag6 = e.source.category == "skill" || (e.source.category == "attribute" && !e.source.textPhase.IsEmpty());
                            bool flag7 = e.source.category == "enchant";
                            if (e.source.tag.Contains("multiplier"))
                            {
                                flag6 = (flag7 = false);
                                name = EClass.sources.elements.alias[e.source.aliasRef].GetName();
                            }

                            flag2 = !(flag6 || flag7);
                            text = (flag6 ? "textEncSkill" : (flag7 ? "textEncEnc" : "textEnc")).lang(name, num + (e.source.tag.Contains("ratio") ? "%" : ""), ((e.Value > 0) ? "encIncrease" : "encDecrease").lang());
                        }

                        int num3 = ((!(e is Resistance)) ? 1 : 0);
                        if (!flag && !flag2 && !e.source.tag.Contains("flag"))
                        {
                            text = text + " [" + "*".Repeat(Mathf.Clamp(num * e.source.mtp / 5 + num3, 1, 5)) + ((num * e.source.mtp / 5 + num3 > 5) ? "+" : "") + "]";
                        }

                        if (e.HasTag("hidden"))
                        {
                            text = "(debug)" + text;
                        }

                        FontColor color = (flag ? FontColor.Default : (flag3 ? FontColor.Bad : FontColor.Good));
                        if (e.IsGlobalElement){
							var suffix = (Lang.langCode == Lang.LangCode.CN.ToString()) ? "(个人)" : "(Personal)";
                            text = text + " " + suffix;//(e.IsFactionWideElement ? "_factionWide" : "_partyWide").lang();
                            //if (__instance.Card != null && !__instance.Card.c_idDeity.IsEmpty() && __instance.Card.c_idDeity != EClass.pc.idFaith)
                            if (__instance.Card != null && !__instance.Card.c_idDeity.IsEmpty() && __instance.Card.c_idDeity == EClass.pc.idFaith)
                                continue;

                            color = FontColor.Myth;
                        }

                        if (flag4 && e.IsFoodTrait && !e.IsFoodTraitMain)
                            color = FontColor.FoodMisc;

                        if (e.id == 2 && e.Value >= 0)
                            color = FontColor.FoodQuality;

                        if (funcText != null)
                            text = funcText(e, text);

                        n.AddText("NoteText_prefwidth", text, color);
                        onAddNote?.Invoke(n, e);
                        continue;
                    }
            }

            UIItem uIItem = n.AddTopic("TopicAttribute", e.Name, "".TagColor((e.ValueWithoutLink > 0) ? SkinManager.CurrentColors.textGood : SkinManager.CurrentColors.textBad, e.ValueWithoutLink.ToString() ?? ""));
            if ((bool)(UnityEngine.Object)(object)uIItem.button1)
            {
                uIItem.button1.tooltip.onShowTooltip = delegate (UITooltip t){
                    e.WriteNote(t.note, EClass.pc.elements);
                };
            }

			e.SetImage(uIItem.image1);
            Image image = uIItem.image2;
            int value = (e.Potential - 80) / 20;
            ((Behaviour)(object)image).enabled = e.Potential != 80;
            image.sprite = EClass.core.refs.spritesPotential[Mathf.Clamp(Mathf.Abs(value), 0, EClass.core.refs.spritesPotential.Count - 1)];
            ((Graphic)image).color = ((e.Potential - 80 >= 0) ? Color.white : new Color(1f, 0.7f, 0.7f));
        }
    }
}

[HarmonyPatch(typeof(CharaBody))]
[HarmonyPatch(nameof(CharaBody.Equip))]
[HarmonyPatch(new [] { typeof(Thing),typeof(BodySlot),typeof(bool)})]
class CharaBody_Equip_Patch{
	public static void Prefix(CharaBody __instance, Thing thing, BodySlot slot, bool msg){
        if(!thing.c_idDeity.IsEmpty() && EClass.pc.faith.id != thing.c_idDeity)
            RFLFFEPVUtil.ProcThing(thing,0);
	}
}

[HarmonyPatch(typeof(CharaBody))]
[HarmonyPatch(nameof(CharaBody.Unequip))]
[HarmonyPatch(new [] { typeof(Thing),typeof(bool)})]
class CharaBody_Unequip_Patch{
	public static void Postfix(CharaBody __instance, Thing thing, bool refresh){
        if(refresh) RFLFFEPVUtil.ProcThing(thing,-1);
	}
}
