// Project:         Filling Food mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Ralzar
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Ralzar


using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;
using System.Collections.Generic;

namespace ClimatesCloaks
{
    public class FillingFood : MonoBehaviour
    {

        static Mod mod;

        [Invoke(StateManager.StateTypes.Game, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<FillingFood>();

            EntityEffectBroker.OnNewMagicRound += FoodRot_OnNewMagicRound;

            ItemHelper itemHelper = DaggerfallUnity.Instance.ItemHelper;

            itemHelper.RegisterCustomItem(ItemApple.templateIndex, ItemGroups.UselessItems2, typeof(ItemApple));
            itemHelper.RegisterCustomItem(ItemApple.templateIndex, ItemGroups.UselessItems2, typeof(ItemOrange));
            itemHelper.RegisterCustomItem(ItemBread.templateIndex, ItemGroups.UselessItems2, typeof(ItemBread));
            itemHelper.RegisterCustomItem(ItemBread.templateIndex, ItemGroups.UselessItems2, typeof(ItemFish));
            itemHelper.RegisterCustomItem(ItemBread.templateIndex, ItemGroups.UselessItems2, typeof(ItemSaltedFish));
            itemHelper.RegisterCustomItem(ItemMeat.templateIndex, ItemGroups.UselessItems2, typeof(ItemMeat));
        }

        DaggerfallUnity dfUnity;
        PlayerEnterExit playerEnterExit;

        //Hunting code WIP
        //bool ambientText = false;
        //float lastTickTime;
        //float tickTimeInterval;
        //int huntChance = 80;
        //int textSpecificChance = 50;
        //float stdInterval = 10f;
        //float postTextInterval = 60f;
        //int textDisplayTime = 3;


        //Hunting code WIP
        //void Start()
        //{
        //    dfUnity = DaggerfallUnity.Instance;
        //    playerEnterExit = GameManager.Instance.PlayerEnterExit;
        //    lastTickTime = Time.unscaledTime;
        //    tickTimeInterval = stdInterval;
        //}

        void Awake()
        {
            mod.IsReady = true;
            Debug.Log("[FillingFood Food] Mod Is Ready");
        }

        //Hunting Quest test
        static PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
        static public bool hungry = true;
        static public bool starving = false;
        static private uint starvation = 0;
        static private int starvCounter = 0;
        static public bool rations = RationsToEat();
        static private int foodCount = 0;
        static public uint gameMinutes = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
        static public uint ateTime = GameManager.Instance.PlayerEntity.LastTimePlayerAteOrDrankAtTavern;
        static public uint hunger = gameMinutes - ateTime;

        void Update()
        {
            //if (!dfUnity.IsReady || !playerEnterExit || GameManager.IsGamePaused)
            //    return;

            ////Hunting code WIP
            //if (!GameManager.Instance.PlayerGPS.IsPlayerInLocationRect
            //    && Time.unscaledTime > lastTickTime + tickTimeInterval
            //    && DaggerfallUnity.Instance.WorldTime.Now.IsDay
            //    && !GameManager.IsGamePaused
            //    )
            //{
            //    lastTickTime = Time.unscaledTime;
            //    tickTimeInterval = stdInterval;
            //    Debug.Log("[Filling Food] Rolling for hunting chance.");

            //    if (Dice100.SuccessRoll(huntChance) && !GameManager.Instance.AreEnemiesNearby())
            //    {
            //        Debug.Log("[Filling Food] Hunting Success");
            //        QuestMachine.Instance.StartQuest("HFQ00");
            //        tickTimeInterval = postTextInterval;

            //    }
            //}

            gameMinutes = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
            ateTime = GameManager.Instance.PlayerEntity.LastTimePlayerAteOrDrankAtTavern;
            hunger = gameMinutes - ateTime;
            if (hunger <= 240 && hungry)
            {
                hungry = false;
                starving = false;
                EntityEffectBroker.OnNewMagicRound += FoodEffects_OnNewMagicRound;
                DaggerfallUI.AddHUDText("You feel invigorated by the meal.");
                Debug.Log("[FillingFood Food] Registering OnNewMagicRound");
            }
            if (hunger > 1440 && !starving)
            {
                starving = true;
                DaggerfallUI.AddHUDText("You are starving...");
                EntityEffectBroker.OnNewMagicRound += Starvation_OnNewMagicRound;
            }
        }

        static private void Starvation_OnNewMagicRound()
        {
            if (!SaveLoadManager.Instance.LoadInProgress && DaggerfallUI.Instance.FadeBehaviour.FadeInProgress && GameManager.IsGamePaused)
            starvation = (hunger / 1440);
            rations = RationsToEat();
            if (hunger > 240 && starving && rations)
            {
                rations = RationsToEat();
                List<DaggerfallUnityItem> sacks = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.UselessItems2, ClimateCloaks.templateIndex_Rations);
                foreach (DaggerfallUnityItem sack in sacks)
                {
                    if (sack.weightInKg > 0.1)
                    {
                        sack.weightInKg -= 0.1f;
                        Debug.LogFormat("[Filling Food] {0} eat {1}", sack.shortName, rations);
                        if (sack.weightInKg <= 0.1)
                        {
                            sack.shortName = "Empty Sack";
                            DaggerfallUI.AddHUDText("You empty your ration sack.");
                        }
                        break;
                    }
                }
            }
            else if (!rations && starving)
            {

                playerEntity.DecreaseFatigue((int)starvation);
            }
            else if (!starving)
            {
                starvation = 0;
                EntityEffectBroker.OnNewMagicRound -= Starvation_OnNewMagicRound;
            }
        }

        static private bool RationsToEat()
        {
            List<DaggerfallUnityItem> sacks = GameManager.Instance.PlayerEntity.Items.SearchItems(ItemGroups.UselessItems2, ClimateCloaks.templateIndex_Rations);
            foreach (DaggerfallUnityItem sack in sacks)
            {
                if (sack.weightInKg > 0.1)
                {
                    Debug.Log("[Climates & Cloaks] WaterToDrink = true");
                    return true;
                }
            }
            return false;
        }

        static private void FoodRot()
        {
            bool rotted = false;
            int rotChance = Random.Range(1,100);
            Debug.Log("[Filling Food] rotChance = " + rotChance.ToString());
            foreach (ItemCollection playerItems in new ItemCollection[] { GameManager.Instance.PlayerEntity.Items, GameManager.Instance.PlayerEntity.WagonItems })
            {
                for (int i = 0; i < playerItems.Count; i++)
                {
                    DaggerfallUnityItem item = playerItems.GetItem(i);
                    if (item is AbstractItemFood)
                    {
                        AbstractItemFood food = item as AbstractItemFood;
                        if (rotChance > food.maxCondition && food.FoodStatus < 4)
                        { 
                            food.RotFood();
                            rotted = true;
                            Debug.Log("[Filling Food] Food Rotted: " + food.shortName);
                        }
                    }
                }
            }
            if (rotted)
            {
                rotted = false;
                DaggerfallUI.AddHUDText("Your food is getting a bit ripe...");
            }
        }

        private static int rotCounter = 0;

        private static void FoodRot_OnNewMagicRound()
        {
            if (!SaveLoadManager.Instance.LoadInProgress)
            {
                rotCounter++;
                Debug.Log("[Filling Food] rotCounter = " + rotCounter.ToString());
                if (rotCounter > 1)//50)
                {
                    FoodRot();
                    rotCounter = 0;
                }
            }
        }

        private static void FoodEffects_OnNewMagicRound()
        {
            Debug.Log("[FillingFood Food] Round Start");
            Debug.Log("[Filling Food] Hunger = " + hunger.ToString());
            if ( hunger <= 239)
            {
                foodCount += (200 - (int)hunger);
                Debug.Log(foodCount.ToString());
                if (foodCount >= 500)
                {
                    playerEntity.IncreaseFatigue(1, true);
                    foodCount = 0;
                    Debug.Log("[FillingFood Food] +1 Fatigue");
                }
            }
            else
            {
                Debug.Log("[FillingFood Food] Hungry");
                hungry = true;
                DaggerfallUI.AddHUDText("Your stomach rumbles...");
                EntityEffectBroker.OnNewMagicRound -= FoodEffects_OnNewMagicRound;
                Debug.Log("[FillingFood Food] De-registering from OnNewMagicRound");
            }
            Debug.Log("[FillingFood Food] Round End");
        }
    }
}
