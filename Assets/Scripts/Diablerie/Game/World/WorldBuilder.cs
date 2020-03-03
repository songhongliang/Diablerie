﻿using System.Collections;
using Diablerie.Engine;
using Diablerie.Engine.Datasheets;
using Diablerie.Engine.Entities;
using Diablerie.Engine.World;
using Diablerie.Game.AI;
using Diablerie.Game.UI;
using UnityEngine;

namespace Diablerie.Game.World
{
    public class WorldBuilder : MonoBehaviour
    {
        public static string className = "Sorceress";
        
        private Act _currentAct;
        private static WorldBuilder instance;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            StartCoroutine(LoadActCoroutine(1));
        }

        private IEnumerator LoadActCoroutine(int actNumber)
        {
            PlayerController.instance.enabled = false;
            MouseSelection.instance.enabled = false;
            LoadingScreen.Show(0.5f);  // It's not zero due to preceding Unity scene load
            ScreenFader.SetToBlack();
            yield return null;
            
            if (_currentAct != null)
                Destroy(_currentAct.root);
            _currentAct = CreateAct(actNumber);
            
            LoadingScreen.Show(0.75f);
            yield return null;

            if (WorldState.instance.Player == null)
            {
                WorldState.instance.Player = new Player(className, _currentAct.entry);
                PlayerController.instance.SetPlayer(WorldState.instance.Player);
            }
            else
            {
                WorldState.instance.Player.character.InstantMove(Iso.MapToIso(Iso.MapTileToWorld(_currentAct.entry)));
            }

            LoadingScreen.Show(0.9f);
            yield return null; // Workaround to load first DCC while screen is black to avoid visible spikes
            
            LoadingScreen.Show(1.0f);
            yield return null;
            
            PlayerController.instance.enabled = true;
            MouseSelection.instance.enabled = true;
            LoadingScreen.Hide();
            ScreenFader.FadeToClear();
        }

        static Act CreateAct(int actNumber)
        {
            WorldState.instance.Grid.Reset();
            if (actNumber == 1)
            {
                return new Act1();
            }
            if (actNumber == 2)
            {
                return new Act2();
            }
            if (actNumber == 3)
            {
                return new Act3();
            }
            if (actNumber == 4)
            {
                return new Act4();
            }
            if (actNumber == 5)
            {
                return new Act5();
            }

            return new Act1();
        }

        public static void LoadAct(int actNumber)
        {
            AudioManager.instance.Play("player_townportal_enter");
            instance.StartCoroutine(instance.LoadActCoroutine(actNumber));
        }

        public static Character SpawnMonster(string id, Vector3 pos, Transform parent = null, Character summoner = null)
        {
            MonStat monStat = MonStat.Find(id);
            if (monStat == null)
            {
                Debug.LogWarning("Monster id not found: " + id);
                return null;
            }
            return SpawnMonster(monStat, pos, parent, summoner);
        }

        public static Character SpawnMonster(MonStat monStat, Vector3 pos, Transform parent = null, Character summoner = null)
        {
            pos = Iso.MapToIso(pos);
            if (!CollisionMap.Fit(pos, out pos, monStat.ext.sizeX))
            {
                return null;
            }

            var monster = new GameObject(monStat.nameStr);
            monster.transform.SetParent(parent);
            monster.transform.position = Iso.MapToWorld(pos);
        
            CollisionMap.Move(pos, pos, monStat.ext.sizeX, monster);

            var character = monster.AddComponent<Character>();
            character.monStat = monStat;
            character.title = monStat.name;
            character.basePath = @"data\global\monsters";
            character.token = monStat.code;
            character.weaponClass = monStat.ext.baseWeaponClass;
            character.run = false;
            character.walkSpeed = monStat.speed;
            character.runSpeed = monStat.runSpeed;
            character.size = monStat.ext.sizeX;
            character.killable = monStat.killable;

            var monLvl = MonLvl.Find(monStat.level[0]);
            if (monLvl != null && !monStat.noRatio)
                character.health = Random.Range(monLvl.hp[0] * monStat.stats[0].minHP, monLvl.hp[0] * monStat.stats[0].maxHP + 1) / 100;
            else
                character.health = Random.Range(monStat.stats[0].minHP, monStat.stats[0].maxHP + 1);
            character.maxHealth = character.health;

            var animator = character.GetComponent<COFAnimator>();
            animator.equip = new string[monStat.ext.gearVariants.Length];
            for (int i = 0; i < animator.equip.Length; ++i)
            {
                var variants = monStat.ext.gearVariants[i];
                if (variants == null)
                    continue;
                animator.equip[i] = variants[Random.Range(0, variants.Length)];
            }

            if (summoner != null)
            {
                character.party = summoner.party;
                var petController = monster.AddComponent<PetController>();
                petController.owner = summoner;
            }
            else if (monStat.ai == "Npc" || monStat.ai == "Towner" || monStat.ai == "Vendor" || monStat.ai == "Hireable")
            {
                character.party = Party.Good;
                monster.AddComponent<NpcController>();
            }
            else if (monStat.ai != "Idle" && monStat.ai != "NpcStationary")
            {
                character.party = Party.Evil;
                monster.AddComponent<MonsterController>();
            }

            var body = monster.AddComponent<Rigidbody2D>();
            body.isKinematic = true;
            var collider = monster.AddComponent<CircleCollider2D>();
            collider.radius = monStat.ext.sizeX * Iso.tileSizeY;

            return character;
        }

        public static StaticObject SpawnObject(ObjectInfo objectInfo, Vector3 pos, bool fit = false, Transform parent = null)
        {
            if (fit)
            {
                pos = Iso.MapToIso(pos);
                if (!CollisionMap.Fit(pos, out pos, objectInfo.sizeX))
                {
                    return null;
                }
                pos = Iso.MapToWorld(pos);
            }

            var gameObject = new GameObject(objectInfo.description);
            gameObject.transform.position = pos;

            var staticObject = gameObject.AddComponent<StaticObject>();
            staticObject.objectInfo = objectInfo;
            staticObject.title = objectInfo.name;

            gameObject.transform.SetParent(parent, true);

            return staticObject;
        }

        public static StaticObject SpawnObject(string token, Vector3 worldPos, bool fit = false)
        {
            ObjectInfo objectInfo = ObjectInfo.Find(token);
            if (objectInfo == null)
            {
                Debug.LogWarning("ObjectInfo with token'" + token + "' not found");
                return null;
            }
            return SpawnObject(objectInfo, worldPos, fit: fit);
        }
    }
}
