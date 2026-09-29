using System;
using System.Collections;
using System.Collections.Generic;

namespace MusicShuffle
{
	public static class Lists
	{
		public static List<(string, string)> PreloadNames = [
            ("Abyss_06_Core","_SceneManager"),
            ("Abyss_19","Infected Knight"),
            ("Cliffs_02_boss","Ghost Warrior NPC"), //dream warriors
            ("Crossroads_01","_SceneManager"),
            ("Crossroads_10_boss", "Battle Scene/False Knight New"),
            ("Deepnest_02","_SceneManager"),
            ("Deepnest_32", "Mimic Spider"),
            ("Deepnest_East_11", "_SceneManager"), //edge
            ("Deepnest_Spider_Town", "Music Region"),
            ("Dream_04_White_Defender", "White Defender"),
            ("Dream_Final_Boss", "Boss Control"), //Radiance, Soul Master 2 and Furious Gods
            ("Dream_Guardian_Monomon", "_SceneManager"),
            ("Dream_Guardian_Monomon", "Play Music Strings and Choir"),
            ("Dream_Mighty_Zote", "Grey Prince"),
            ("Dream_Room_Believer_Shrine", "_SceneManager"),
            ("Fungus1_02","_SceneManager"),
            ("Fungus1_04_boss","Hornet Boss 1"), //includes godhome version
            ("Fungus2_02","_SceneManager"), //safety
            ("Fungus2_05","_SceneManager"),
            ("Fungus3_01","_SceneManager"), //fog canyon
            ("Fungus3_21","_SceneManager"), //queen's gardens
            ("GG_Atrium","_SceneManager"),
            ("GG_Brooding_Mawlek", "_SceneManager"),
            ("GG_Crystal_Guardian", "_SceneManager"),
            ("GG_Hollow_Knight", "Battle Scene"),
            ("GG_Mage_Knight", "_SceneManager"),
            ("GG_Mantis_Lords", "_SceneManager"), //mantis lords and sisters of battle
            ("Grimm_Main_Tent_boss","Grimm Boss"),
            ("Grimm_Nightmare", "Grimm Control"),
            ("Hive_01","_SceneManager"),
            ("Hive_05", "Battle Scene/Hive Knight"),
            ("RestingGrounds_04","_SceneManager"),
            ("Room_Colosseum_Bronze", "Colosseum Manager"),
            ("Room_Final_Boss_Atrium","_SceneManager"),
            ("Room_Queen","Music Region"),
            ("Ruins1_24","_SceneManager"), //soul sanctum
            ("Ruins1_24_boss","Mage Lord"), //soul master
            ("Ruins2_01","_SceneManager"), //city
            ("Ruins2_11_boss","Battle Scene/Jar Collector"),
            ("Town", "Music Region"),
            ("Waterways_05","_SceneManager"),
            ("Waterways_05_boss", "Dung Defender"),
            ("White_Palace_04","_SceneManager"),
            ("White_Palace_20","_SceneManager"), //path of pain
        ];

        public static string[] BossThemes = [
            "Boss1",
            "BossHornet",
            "BossIK",
            "BossMageLord",
            "BossMantisLords",
            "DreamFight",
            "DungDefender",
            "EnemyBattle",
            "GG Elegant",
            "GG Heavy",
            "GG Hornet",
            "GG Mantis",
            "GG Normal",
            "GG Sad",
            "Grey Prince",
            "Grimm",
            "HiveKnight",
            "HollowKnightPrime",
            "MageLord2",
            "MimicSpider",
            "NightmareGrimm",
            "Radiance",
            "WhiteDefender",
        ];

        public static string[] Snapshots = [
            "Normal",
            "Action", //Action + Main
            "Action and Sub",
            "Sub Area",
            "Tension Only",
            "Action Only",
            "Main Only",
        ];
    }
}


