using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;





[BepInPlugin("sosarciel.wishingforchara", "WishingForChara", "1.0.0.0")]
[BepInProcess("Elin.exe")]
public class WishingForChara : BaseUnityPlugin {
    public static new ManualLogSource Logger;

    void Awake() {
        Logger = base.Logger;
        Initialize();
        var harmony = new Harmony("WishingForChara");
        harmony.PatchAll();
        Logger.LogInfo("Awake");
    }
    void Initialize(){
//        WFCUtils.AllowUnique = base.Config.Bind<bool>("General", "AllowUniqueCharacters", false, @"
//允许生成Unique角色。
//Specifies whether to allow the generation of unique characters.
//".Trim());
    }
}

public static class WFCUtils{
    public static ConfigEntry<bool> AllowUnique;
}

class WishItem{
    public string n;

    public int score;

    public Action action;
}


[HarmonyPatch(typeof(ActEffect))]
[HarmonyPatch(nameof(ActEffect.Wish))]
[HarmonyPatch(new [] { typeof(string),typeof(string),typeof(int) ,typeof(BlessedState)})]
public static class ActEffect_Wish_Patch{
    private static bool Prefix(ActEffect __instance, string s, string name, int power, BlessedState state){
        string _s = s.ToLower();

        List<string> prefixes = new() { "chara:", "角色:" };
        //Msg.Say(_s);

        if (!prefixes.Any(prefix => _s.StartsWith(prefix))) return true;

        string matchingPrefix = prefixes.FirstOrDefault(prefix => _s.StartsWith(prefix));
        if (matchingPrefix != null) _s = _s.Substring(matchingPrefix.Length);

        List<WishItem> list = new();
        string netMsg = GameLang.Parse("wish".langGame(), thirdPerson: true, name, s);
        bool net = EClass.core.config.net.enable && EClass.core.config.net.sendEvent;
        int wishLv = 10 + power / 4;
        //Msg.Say(_s);
        foreach (CardRow r in EClass.sources.cards.rows){
            if (!r.isChara) continue;
            //if(!WFCUtils.AllowUnique.Value && r.idActor.Contains("unique")) continue;
            string text = r.name == "*r"
                ? r.GetText("aka").ToLower()
                : r.GetName().ToLower();
            if(text=="*r") continue;
            //Msg.Say(text);
            int score = ActEffect.Compare(_s, text);
            if (score == 0) continue;

            list.Add(new WishItem{
                score = score,
                n = text,
                action = delegate{
                    WishingForChara.Logger.LogInfo("wish chara: "+r.id);

                    Chara spawnC = new();
                    spawnC.Create(r.id);
                    EClass._zone.AddCard(spawnC, EClass.pc.pos);

                    netMsg = netMsg + Lang.space + GameLang.Parse("wishNet".langGame(), Msg.IsThirdPerson(spawnC), Msg.GetName(spawnC).ToTitleCase());
                    if (net) Net.SendChat(name, netMsg, ChatCategory.Wish, Lang.langCode);
                }
            });
        }

        if (list.Count == 0) return true;

        list.Sort((WishItem a, WishItem b) => b.score - a.score);

        list[0].action();
        return false;
    }
}