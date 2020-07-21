using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using Modding;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.InControl;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NGPlus
{
    public class NGPlus : Mod
    {
        public Dictionary<string, GameObject> dict = new Dictionary<string, GameObject>();

        public override string GetVersion() => "1.1";

        public NGPlus() : base("NG+ (King's Pass)") { }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            IL.HeroController.SoulGain += OnSoulGain;
            ModHooks.Instance.SoulGainHook += GainSoul;
            On.HeroController.NailParry += OnParry;
            ModHooks.Instance.RecordKillForJournalHook += OnKill;
            ModHooks.Instance.LanguageGetHook += OnLangGet;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;

            AddObject(preloadedObjects, "spikes_sprite", "Tutorial_01", "_Props/Cave Spikes (7)");
            AddObject(preloadedObjects, "spikes", "Tutorial_01", "_Props/Cave Spikes");
            AddObject(preloadedObjects, "platform", "Tutorial_01", "_Scenery/plat_float_07");
            AddObject(preloadedObjects, "hazard", "Tutorial_01", "_Markers/Hazard Respawn Trigger v2 (2)");
            AddObject(preloadedObjects, "gate", "Crossroads_03", "_Props/Toll Gate");
            AddObject(preloadedObjects, "lever", "Crossroads_03", "_Props/Toll Gate Switch");
            AddObject(preloadedObjects, "tiktik", "Crossroads_01", "_Enemies/Climber");
            AddObject(preloadedObjects, "shieldguy", "Crossroads_15", "_Enemies/Zombie Shield");
            AddObject(preloadedObjects, "bigguy", "Crossroads_48", "Zombie Guard");
            AddObject(preloadedObjects, "totem", "Crossroads_45", "Soul Totem 5");
            AddObject(preloadedObjects, "bench", "Town", "RestBench");
        }
        
        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>()
            {
                ("Tutorial_01","_Props/Cave Spikes (7)"),
                ("Tutorial_01","_Props/Cave Spikes"),
                ("Tutorial_01","_Scenery/plat_float_07"),
                ("Tutorial_01","_Markers/Hazard Respawn Trigger v2 (2)"),
                ("Crossroads_03","_Props/Toll Gate"),
                ("Crossroads_03","_Props/Toll Gate Switch"),
                ("Crossroads_01","_Enemies/Climber"),
                ("Crossroads_15","_Enemies/Zombie Shield"),
                ("Crossroads_45","Soul Totem 5"),
                ("Crossroads_48","Zombie Guard"),
                ("Town", "RestBench")
            };
        }
        
        private void AddObject(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects, string objName, string scene, string obj)
        {
            var go = UnityEngine.Object.Instantiate(preloadedObjects[scene][obj]);
            UnityEngine.Object.DontDestroyOnLoad(go);
            dict.Add(objName, go);
        }

        //prevents soul vessel from flashing when you hit enemies
        private void OnSoulGain(ILContext il)
        {
            var cursor = new ILCursor(il).Goto(0);

            cursor.GotoNext(MoveType.After, i => i.MatchCallvirt<PlayerData>("AddMPCharge"), i => i.MatchPop());

            cursor.Emit(OpCodes.Ldloc_1);
            cursor.Emit(OpCodes.Ldc_I4_0);
            cursor.Emit(OpCodes.Beq, cursor.Instrs[cursor.Instrs.Count - 1]);
        }

        //only let's you gain soul in OnParry and OnKill
        private int GainSoul(int num)
        {
            return _soul ? num : 0;
        }

        //gain soul when you parry
        private void OnParry(On.HeroController.orig_NailParry orig, HeroController self)
        {
            _soul = true;
            HeroController.instance.SoulGain();
            _soul = false;

            orig(self);
        }
        
        //gain soul when you kill an enemy
        private void OnKill(EnemyDeathEffects enemydeatheffects, string playerdataname, string killedboolplayerdatalookupkey, string killcountintplayerdatalookupkey, string newdataboolplayerdatalookupkey)
        {
            _soul = true;
            HeroController.instance.SoulGain();
            _soul = false;
        }

        //changes tablet text
        private string OnLangGet(string key, string sheet)
        {
            string orig_text = Language.Language.GetInternal(key, sheet);

            //Log($"Request language {sheet}, {key} - {orig_text}");

            if (key == "PROMPT_FOCUS_EXPLAINER1A")
            {
                return "<color=orange> Collect SOUL by killing enemies or parrying.</color>";
            }

            if (key == "TUT_TAB_02")
            {
                return "<color=orange>This is a preview for my NG+ mod.<br><br>You start with the mantis claw, but don't have the ability to pogo yet.<br><br>All of King's Pass is possible with wall climb, no glitches, exploits, or outside abilities needed.<br><br>Read the other tablets closely for more information.</color>";
            }

            if (key == "TUT_TAB_03")
            {
                return "Higher beings, these words are for you alone.<br><br> <color=orange>Levers will reset after you die in this level.</color>";
            }
            return orig_text;
        }

        //adds and removes things from King's Pass
        private void OnSceneChange(Scene oldScene, Scene newScene)
        {
            On.HeroController.Bounce += NoBounce;
            PlayerData.instance.hasWalljump = true;

            if (newScene.name == "Tutorial_01")
            {
                //Destroying objects
                foreach (var gameobject in newScene.GetRootGameObjects())
                {
                    if (gameobject.name == "_Enemies")
                    {
                        var children = gameobject.GetComponentsInChildren<Transform>();
                        foreach (var child in children)
                        {
                            if (child.name.Contains("Crawler") || child.name.Contains("Buzzer"))
                            {
                                UnityEngine.Object.Destroy(child.gameObject);
                            }
                        }
                    }

                    if (gameobject.name == "_Props")
                    {
                        var children = gameobject.GetComponentsInChildren<Transform>();
                        foreach (var child in children)
                        {
                            if (child.name == "Geo Rock 1" || child.name == "white_grass42" || child.name == "white_grass40" || child.name == "white_grass39" || child.name == "white_grass32" || child.name.Contains("white_grass46") || child.name == "white_grass47" || child.name == "Geo Rock 2" || child.name == "white_grass38" || child.name == "Health Cocoon" || child.name == "Geo Rock 4" || child.name == "Geo Rock 5")
                            {
                                UnityEngine.Object.Destroy(child.gameObject);
                            }

                            if (child.name == "Tut_tablet_top (2)")
                            {
                                var children1 = child.GetComponentsInChildren<SpriteRenderer>();
                                foreach (var child1 in children1)
                                {
                                    if (child1.name == "lit_tablet")
                                    {
                                        child1.gameObject.GetComponent<SpriteRenderer>().color = new Color(1f, 0.5f, 0f);
                                    }
                                }
                            }

                            if (child.name == "Tut_tablet_top (1)")
                            {
                                var children1 = child.GetComponentsInChildren<SpriteRenderer>();
                                foreach (var child1 in children1)
                                {
                                    if (child1.name == "lit_tablet")
                                    {
                                        child1.gameObject.GetComponent<SpriteRenderer>().color = new Color(1f, 0.5f, 0f);
                                    }
                                }
                            }

                            if (child.name == "Tut_tablet_top")
                            {
                                var children1 = child.GetComponentsInChildren<SpriteRenderer>();
                                foreach (var child1 in children1)
                                {
                                    if (child1.name == "lit_tablet")
                                    {
                                        child1.gameObject.GetComponent<SpriteRenderer>().color = new Color(1f, 0.5f, 0f);
                                    }
                                }
                            }
                        }
                    }

                    if (gameobject.name == "_Scenery")
                    {
                        var children = gameobject.GetComponentsInChildren<Transform>();
                        foreach (var child in children)
                        {
                            if (child.name == "cd_FG_rock_20 (144)" || child.name == "spikes0001(22)" || child.name == "cd_FG_rock_20 (160)" || child.name == "cd_FG_rock_20 (145)" || child.name == "Crossroads Statue Shell" || child.name == "Break Floor 1" || child.name == "Crossroads Statue Shell 2" || (child.name == "plat_float_07" && child.position.x > 140f) || (child.name.Contains("plat_float_01") && child.position.x < 95f) || child.name.Contains("Cocoon Plant") || child.name == "Tute Pole 13" || child.name == "plat_float_17")
                            {
                                UnityEngine.Object.Destroy(child.gameObject);
                            }
                        }
                    }

                    if (gameobject.name == "mask_container")
                    {
                        var children = gameobject.GetComponentsInChildren<Transform>();
                        foreach (var child in children)
                        {
                            if (child.name == "Secret Mask Top" || child.name == "fury charm_remask")
                            {
                                var children1 = child.GetComponentsInChildren<Transform>();
                                foreach (var child1 in children1)
                                {
                                    UnityEngine.Object.Destroy(child1.gameObject);
                                }
                            }
                        }
                    }

                    if (gameobject.name == "_Markers")
                    {
                        var children = gameobject.GetComponentsInChildren<Transform>();
                        foreach (var child in children)
                        {
                            if (child.name == "Hazard Respawn Trigger v2" || child.name == "Hazard Respawn Trigger v2 (2)" || child.name == "Hazard Respawn Marker 3" || child.name == "Hazard Respawn Marker 1")
                            {
                                UnityEngine.Object.Destroy(child.gameObject);
                            }
                        }
                    }

                    if (gameobject.name == "_Areas")
                    {
                        var children = gameobject.GetComponentsInChildren<Transform>();
                        foreach (var child in children)
                        {
                            if (child.name == "Hazard Respawn Trigger 3" || child.name == "Hazard Respawn Trigger 1")
                            {
                                UnityEngine.Object.Destroy(child.gameObject);
                            }
                        }
                    }

                    if (gameobject.name == "First Crawler" || gameobject.name.Contains("Cocoon Plant"))
                    {
                        UnityEngine.Object.Destroy(gameobject);
                    }
                }

                //Creating objects
                #region Creating objects
                GameManager.instance.StartCoroutine(AddKP());

                #region Spikes
                var spikes1 = AddSpikes(true, new Vector3(60.6f, 29.4f));
                var spikes1_hit = AddSpikes(false, new Vector3(57.8f, 29.4f), 0.12f, 0.9f);
                
                var spikes2 = AddSpikes(true, new Vector3(67.0f, 32.6f));
                var spikes2_1 = AddSpikes(true, new Vector3(68.6f, 32.6f));
                var spikes2_hit = AddSpikes(false, new Vector3(65.1f, 32.6f), 0.25f, 0.9f);

                var spikes3 = AddSpikes(true, new Vector3(62.1f, 37.0f));
                var spikes3_1 = AddSpikes(true, new Vector3(62.79f, 37.0f), 0.9f, 0.8f);
                var spikes3_hit = AddSpikes(false, new Vector3(59.7f, 37.0f), 0.19f, 0.9f);

                var spikes4 = AddSpikes(true, new Vector3(67.1f, 41.6f));
                var spikes4_1 = AddSpikes(true, new Vector3(68.3f, 41.6f), 1f, 0.8f);
                var spikes4_2 = AddSpikes(true, new Vector3(69.3f, 41.6f));
                var spikes4_hit = AddSpikes(false, new Vector3(65.4f, 41.6f), 0.3f, 0.9f);

                var spikes5 = AddSpikes(true, new Vector3(91f, 35.7f), -90f);
                var spikes5_hit = AddSpikes(false, new Vector3(91.4f, 38.2f), 0.15f, 0.9f, -90f);

                var spikes6 = AddSpikes(true, new Vector3(89.5f, 37.4f));
                var spikes6_hit = AddSpikes(false, new Vector3(86.9f, 37.6f), 0.15f, 0.9f);

                var spikes7 = AddSpikes(true, new Vector3(83.55f, 34.2f));
                var spikes7_hit = AddSpikes(false, new Vector3(80.95f, 34.4f), 0.13f, 0.9f);

                var spikes8 = AddSpikes(true, new Vector3(82.2f, 32.7f), 90f);
                var spikes8_hit = AddSpikes(false, new Vector3(82.0f, 30.1f), 0.15f, 0.9f, 90f);

                var spikes9 = AddSpikes(true, new Vector3(82.6f, 26.5f));
                var spikes9_1 = AddSpikes(true, new Vector3(84.5f, 26.5f));
                var spikes9_2 = AddSpikes(true, new Vector3(86.3f, 26.5f, -0.1f));
                var spikes9_3 = AddSpikes(true, new Vector3(87.6f, 26.5f), 0.9f, 0.8f);
                var spikes9_hit = AddSpikes(false, new Vector3(82.4f, 26.8f), 0.5f, 0.9f);

                //spikes10 deleted

                var spikes11 = AddSpikes(true, new Vector3(110.1f, 8.7f), -1f, 1f);
                var spikes11_1 = AddSpikes(true, new Vector3(112.9f, 8.6f), 1f, 1f);
                var spikes11_2 = AddSpikes(true, new Vector3(111.4f, 8.6f, -0.1f), -1f, 1f);
                var spikes11_hit = AddSpikes(false, new Vector3(108.9f, 8.5f), 0.4f, 0.9f);

                var spikes12 = AddSpikes(true, new Vector3(139.1f, 6.5f, -0.1f), 0.9f, 0.8f);
                var spikes12_1 = AddSpikes(true, new Vector3(138f, 6.5f, -0.2f));
                var spikes12_2 = AddSpikes(true, new Vector3(136.2f, 6.5f, -0.1f));
                var spikes12_hit = AddSpikes(false, new Vector3(134.6f, 6.7f), 0.4f, 0.9f);

                var spikes13 = AddSpikes(true, new Vector3(154.2f, 7.7f), -90f);
                var spikes13_hit = AddSpikes(false, new Vector3(154.4f, 10.4f), 0.15f, 0.9f, -90f);

                var spikes14 = AddSpikes(true, new Vector3(152.6f, 9.3f));
                var spikes14_hit = AddSpikes(false, new Vector3(150f, 9.5f), 0.15f, 0.9f);

                var spikes15 = AddSpikes(true, new Vector3(145.5f, 12f), 90f);
                var spikes15_hit = AddSpikes(false, new Vector3(145.3f, 9.4f), 0.15f, 0.9f, 90f);

                var spikes16 = AddSpikes(true, new Vector3(147.1f, 13.6f));
                var spikes16_hit = AddSpikes(false, new Vector3(144.2f, 13.8f), 0.15f, 0.9f);

                var spikes17 = AddSpikes(true, new Vector3(142f, 27.3f));
                var spikes17_hit = AddSpikes(false, new Vector3(139.3f, 27.5f), 0.15f, 0.9f);

                var spikes18 = AddSpikes(true, new Vector3(138.7f, 21.1f));
                var spikes18_hit = AddSpikes(false, new Vector3(135.95f, 21.3f), 0.13f, 0.9f);

                var spikes19 = AddSpikes(true, new Vector3(134.6f, 21f), -90f);
                var spikes19_1 = AddSpikes(true, new Vector3(134.6f, 19.1f), -90f);
                var spikes19_2 = AddSpikes(true, new Vector3(134.6f, 17.1f), 1f, 0.8f, -90f);
                var spikes19_hit = AddSpikes(false, new Vector3(134.8f, 21.7f), 0.43f, 0.9f, -90f);

                var spikes20 = AddSpikes(true, new Vector3(173.95f, 40.1f));
                var spikes20_hit = AddSpikes(false, new Vector3(171.35f, 40.3f), 0.13f, 0.9f);

                var spikes21 = AddSpikes(true, new Vector3(154f, 54.6f, -0.1f), -1f, 0.8f);
                var spikes21_1 = AddSpikes(true, new Vector3(156f, 54.6f, -0.2f), -1f, 0.8f);
                var spikes21_hit = AddSpikes(false, new Vector3(152.3f, 54.7f), 0.29f, 0.8f);

                var spikes22 = AddSpikes(true, new Vector3(144.8f, 39.9f), 1.1f, 0.8f);
                var spikes22_hit = AddSpikes(false, new Vector3(142.05f, 40.1f), 0.17f, 0.5f);

                var spikes23 = AddSpikes(true, new Vector3(189.8f, 39.8f), -90f);
                var spikes23_1 = AddSpikes(true, new Vector3(189.7f, 37.85f), 1f, 0.8f, -90f);
                var spikes23_hit = AddSpikes(false, new Vector3(189.9f, 41.5f), 0.28f, 0.8f, -90f);
                #endregion

                #region Platforms
                var plat1 = AddFromDict("platform", new Vector3(108.8f, 13.1f), 8, new Vector3(-1f, 1f, 1f));
                var plat2 = AddFromDict("platform", new Vector3(142.1f, 25f), 8);
                var plat3 = AddFromDict("platform", new Vector3(174.1f, 38f), 8);
                var plat4 = AddFromDict("platform", new Vector3(188.2f, 38.4f), 8, new Vector3(-1f, 1.5f, 1f));
                var plat5 = AddFromDict("platform", new Vector3(123.8f, 58f), 8);
                #endregion

                #region Soul Totems
                var totem1 = AddFromDict("totem", new Vector3(111.7f, 63.6f, 0.01f), 19, new Vector3(-0.7f, 0.7f));
                var totem2 = AddFromDict("totem", new Vector3(160.5f, 36.8f, 0.01f), 19);
                var totem2fsm = totem2.LocateMyFSM("soul_totem");
                //totem2fsm.FsmVariables.GetFsmInt("Value").Value = int.MaxValue;
                totem2fsm.RemoveAction("Hit",9);
                #endregion

                #region Gates
                var gate1 = AddGate("gate1", new Vector3(82.7f, 49.8f), new Vector3(1f, 2f));
                var gate2 = AddGate("gate2", new Vector3(116.7f, 55.4f));
                var gate3 = AddGate("gate3", new Vector3(144.7f, 61.7f), new Vector3(1f, -1.4f), 90f);
                var gate4 = AddGate("gate5", new Vector3(108.9f, 16.7f), new Vector3(-1f, 1f));
                var gate5 = AddGate("gate4", new Vector3(116.7f, 64.4f), new Vector3(1f, 1.5f));
                #endregion

                #region Levers
                var lever1 = AddLever(gate1.name, new Vector3(60.5f, 46.4f));
                var lever2 = AddLever(gate2.name, new Vector3(57.6f, 58.4f));
                var lever3 = AddLever(gate3.name, new Vector3(108f, 55.4f));
                var lever4 = AddLever(gate4.name, new Vector3(115.8f, 11.4f));
                #endregion

                #region Hazard Respawns
                var hazard1 = AddFromDict("hazard", new Vector3(133.7f, 13.4f), 0);
                var hazard2 = AddFromDict("hazard", new Vector3(189.9f, 38.4f), 0);
                var hazard3 = AddFromDict("hazard", new Vector3(91.9f, 29.4f), 0);
                var hazard4 = AddFromDict("hazard", new Vector3(69f, 27.7f), 0);
                var hazard5 = AddFromDict("hazard", new Vector3(156.7f, 47.4f), 0);
                var hazard6 = AddFromDict("hazard", new Vector3(155.5f, 7.4f), 0);
                var hazard7 = AddFromDict("hazard", new Vector3(131.7f, 25.4f), 0);
                var hazard8 = AddFromDict("hazard", new Vector3(118.1f, 13.4f), 0);
                var hazard9 = AddFromDict("hazard", new Vector3(105f, 13.4f), 0);
                var hazard10 = AddFromDict("hazard", new Vector3(180.1f, 38.4f), 0);
                #endregion

                #region Enemies
                var shieldGuy1 = AddFromDict("shieldguy", new Vector3(95.3f, 11.4f), 11);
                var shieldGuy2 = AddFromDict("shieldguy", new Vector3(97.6f, 63.4f), 11);
                var tiktik1 = AddFromDict("tiktik", new Vector3(86.4f, 43.7f), 11);
                var bigGuy1 = AddFromDict("bigguy", new Vector3(163.9f, 64.6f), 11);
                var bigGuy1fsm = bigGuy1.LocateMyFSM("Zombie Guard");
                bigGuy1fsm.RemoveAction("Wake", 1);
                bigGuy1fsm.RemoveAction("Alert", 1);
                bigGuy1fsm.RemoveAction("Return Right", 1);
                bigGuy1fsm.RemoveAction("Return Left", 1);
                bigGuy1fsm.RemoveAction("Face Hero", 1);
                bigGuy1fsm.RemoveAction("Startle", 1);
                #endregion

                #region Bench
                var bench1 = AddFromDict("bench", new Vector3(163.5f, 5.6f, 0.01f), 13, new Vector3(-1f, 1f));
                #endregion
                #endregion
            }
        }

        private IEnumerator AddKP()
        {
            yield return null;
            var kpspikes = GameObject.Find("Cave Spikes (1)");
            var kpspikes1 = GameObject.Find("Cave Spikes (10)");
            var kpspikes2 = GameObject.Find("Cave Spikes (11)");
            var kpspikes3 = GameObject.Find("Cave Spikes (12)");
            var kpspikes4 = GameObject.Find("Cave Spikes (13)");
            kpspikes.transform.position += new Vector3(0f, -0.4f);
            kpspikes1.transform.position += new Vector3(0f, -0.4f);
            kpspikes2.transform.position += new Vector3(0f, -0.4f);
            kpspikes3.transform.position += new Vector3(0f, -0.4f);
            kpspikes4.transform.position += new Vector3(0f, -0.4f);

            var kpshade = GameObject.Find("Hollow_Shade Marker 10");
            kpshade.transform.position += new Vector3(-12f, -1f);

            var kptablet1 = GameObject.Find("Tut_tablet_top (1)");
            kptablet1.transform.position = new Vector3(94.5f, 12.5f, 1.7f);

            var kptablet2 = GameObject.Find("Tut_tablet_top");
            var kptablet2fsm = kptablet2.LocateMyFSM("Inspection");
            kptablet2fsm.RemoveAction("Blanker Up", 1);
        }

        private GameObject AddFromDict(string name, Vector3 pos, int layer, float rot = 0f)
        {
            var go = UnityEngine.Object.Instantiate(dict[name], pos, Quaternion.identity);
            go.SetActive(true);
            go.layer = layer;
            go.transform.Rotate(0f, 0f, rot);
            return go;
        }

        private GameObject AddFromDict(string name, Vector3 pos, int layer, Vector3 scale, float rot = 0f)
        {
            var go = AddFromDict(name, pos, layer, rot);
            go.transform.localScale = scale;
            return go;
        }

        private GameObject AddSpikes(bool sprite, Vector3 pos, float rot = 0f)
        {
            string name;
            if (sprite)
                name = "spikes_sprite";
            else
                name = "spikes";
            var spikes = AddFromDict(name, pos, 17, rot);
            return spikes;
        }

        private GameObject AddSpikes(bool sprite, Vector3 pos, float scaleX, float scaleY, float rot = 0f)
        {
            var spikes = AddSpikes(sprite, pos, rot);
            if (sprite)
                spikes.transform.localScale = new Vector3(scaleX, scaleY);
            else
                spikes.GetComponent<BoxCollider2D>().size = new Vector2(scaleX, scaleY);
            return spikes;
        }

        private GameObject AddGate(string name, Vector3 pos, float rot = 0f)
        {
            var gate = AddFromDict("gate", pos, 8, rot);
            gate.name = name;
            gate.LocateMyFSM("Toll Gate").SetState("Idle");
            return gate;
        }

        private GameObject AddGate(string name, Vector3 pos, Vector3 scale, float rot = 0f)
        {
            var gate = AddGate(name, pos, rot);
            gate.transform.localScale = scale;
            return gate;
        }

        private GameObject AddLever(string gate, Vector3 pos, float rot = 0f)
        {
            var lever = AddFromDict("lever", pos, 8, rot);
            var leverfsm = lever.LocateMyFSM("toll switch");
            leverfsm.RemoveAction("Initiate", 4);
            leverfsm.SetState("Pause");
            leverfsm.GetAction<FindGameObject>("Initiate", 2).objectName = gate;
            leverfsm.RemoveAction("Initiate", 3);
            return lever;
        }

        private GameObject AddLever(string gate, Vector3 pos, Vector3 scale, float rot = 0f)
        {
            var lever = AddLever(gate, pos, rot);
            lever.transform.localScale = scale;
            return lever;
        }

        //prevents pogo
        private void NoBounce(On.HeroController.orig_Bounce orig, HeroController self) { }

        private bool _soul;
    }
}
