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
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Serialization;

namespace FillingFood
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

            itemHelper.RegisterCustomItem(531, ItemGroups.UselessItems2);
            itemHelper.RegisterItemUseHander(531, EatFood);
            itemHelper.RegisterCustomItem(532, ItemGroups.UselessItems2);
            itemHelper.RegisterItemUseHander(532, EatFood);
            itemHelper.RegisterCustomItem(533, ItemGroups.UselessItems2);
            itemHelper.RegisterItemUseHander(533, EatFood);
            //itemHelper.RegisterCustomItem(534, ItemGroups.UselessItems2);
            //itemHelper.RegisterItemUseHander(534, EatFood);

            itemHelper.RegisterCustomItem(ItemBread.templateIndex, ItemGroups.UselessItems2, typeof(ItemBread));

            itemHelper.RegisterCustomItem(535, ItemGroups.UselessItems2);
            itemHelper.RegisterItemUseHander(535, EatFood);
            itemHelper.RegisterCustomItem(536, ItemGroups.UselessItems2);
            itemHelper.RegisterItemUseHander(536, EatFood);
            itemHelper.RegisterCustomItem(537, ItemGroups.UselessItems2);
            itemHelper.RegisterItemUseHander(537, EatFood);
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
        static private bool hungry = true;
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
                EntityEffectBroker.OnNewMagicRound += FoodEffects_OnNewMagicRound;
                DaggerfallUI.AddHUDText("You feel invigorated by the meal.");
                Debug.Log("[FillingFood Food] Registering OnNewMagicRound");
            }
        }


        public static bool EatFood(DaggerfallUnityItem item, ItemCollection collection)
        {
            uint cal = 240;
            if (item.TemplateIndex == 531) //Rations
            {
                cal -= 120;
            }
            else if (item.TemplateIndex == 532 || item.TemplateIndex == 533) //Apple or Orange
            {
                cal -= 60;
            }
            else if (item.TemplateIndex == 534) //Bread
            {
                cal -= 120;
            }
            else if (item.TemplateIndex == 535) //Fish
            {
                cal -= 180;
            }
            else if (item.TemplateIndex == 536) //Salted Fish
            {
                cal -= 120;
            }
            else if (item.TemplateIndex == 537) //Meat
            {
                cal -= 240;
            }

            if (hunger > 240)
            {
                GameManager.Instance.PlayerEntity.LastTimePlayerAteOrDrankAtTavern = gameMinutes - cal;
                collection.RemoveItem(item);
            }
            else if (hunger > 240-cal )
            {
                GameManager.Instance.PlayerEntity.LastTimePlayerAteOrDrankAtTavern += (240 - cal);
                collection.RemoveItem(item);
            }
            else
            {
                DaggerfallUI.MessageBox(HardStrings.youAreNotHungry);
            }
            return true;
        }

        static private void FoodRot()
        {
            bool rotted = false;
            int rotChance = Random.Range(1,100);
            foreach (ItemCollection playerItems in new ItemCollection[] { GameManager.Instance.PlayerEntity.Items, GameManager.Instance.PlayerEntity.WagonItems })
            {
                for (int i = 0; i < playerItems.Count; i++)
                {
                    DaggerfallUnityItem item = playerItems.GetItem(i);
                    if (item.TemplateIndex == ItemBread.templateIndex)
                    {
                        ItemBread food = item as ItemBread;
                        food.RotFood();
                    }
                    /*
                    if (item.TemplateIndex > 531 && item.TemplateIndex < 538)
                    {
                        if (item.PlayerTextureArchive < 532)
                        {
                            rotted = true;
                            item.shortName = "Expired " + item.ItemName;
                            item.PlayerTextureArchive = item.TemplateIndex;
                            item.WorldTextureRecord = 0;
                            Debug.LogFormat("[Filling Food] {0} expired", item.shortName);
                        }
                        else if (item.WorldTextureArchive > 531 && item.WorldTextureRecord == 0)
                        {
                            rotted = true;
                            item.shortName = "Rotten " + item.ItemName;
                            item.WorldTextureRecord = 1;
                            Debug.LogFormat("[Filling Food] {0} rotted", item.shortName);
                        }
                    }
                    */
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
                if (rotCounter > 3)//50)
                {
                    FoodRot();
                    rotCounter = 0;
                    Debug.Log("[Filling Food] Food rotted");
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
