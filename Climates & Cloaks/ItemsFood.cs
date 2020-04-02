// Project:         Filling Food mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Ralzar
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Ralzar

using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;

namespace ClimatesCloaks
{
    /// <summary>
    /// Abstract class for all food items common behaviour
    /// </summary>
    public abstract class AbstractItemFood : DaggerfallUnityItem
    {
        // In leu of a real enum.
        public const int StatusFresh = 0;
        public const int StatusStale = 1;
        public const int StatusMouldy = 2;
        public const int StatusRotten = 3;
        public const int StatusPutrid = 4;

        public AbstractItemFood(ItemGroups itemGroup, int templateIndex) : base(itemGroup, templateIndex)
        {
            message = StatusFresh;
        }

        public abstract uint GetCalories();

        public int FoodStatus
        {
            get { return message; }
            set { message = value; }
        }

        public virtual string GetFoodStatus()
        {
            switch (FoodStatus)
            {
                case StatusStale:
                    return "Stale ";
                case StatusMouldy:
                    return "Mouldy ";
                case StatusRotten:
                    return "Rotten ";
                case StatusPutrid:
                    return "Putrid ";
                default:
                    return "";
            }
        }

        // Use template world archive for fresh/stale food, or template index for other states
        public override int InventoryTextureArchive
        {
            get
            {
                if (FoodStatus == StatusFresh || FoodStatus == StatusStale)
                    return WorldTextureArchive;
                else
                    return TemplateIndex;
            }
        }

        // Use template world record for fresh food, or status for other states
        public override int InventoryTextureRecord
        {
            get
            {
                switch (FoodStatus)
                {
                    case StatusFresh:
                    case StatusStale:
                    default:
                        return WorldTextureRecord;
                    case StatusMouldy:
                    case StatusRotten:
                        return 0;
                    case StatusPutrid:
                        return 1;
                }
            }
        }

        public void RotFood()
        {
            if (FoodStatus < StatusPutrid)
            {
                FoodStatus++;
                shortName = GetFoodStatus() + ItemTemplate.name;
            }
        }

        public override bool UseItem(ItemCollection collection)
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
            uint gameMinutes = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
            uint hunger = gameMinutes - playerEntity.LastTimePlayerAteOrDrankAtTavern;

            if (hunger >= GetCalories())
            {
                if (hunger > GetCalories() + 240)
                    playerEntity.LastTimePlayerAteOrDrankAtTavern = gameMinutes - 240;
                else
                    playerEntity.LastTimePlayerAteOrDrankAtTavern += GetCalories();

                collection.RemoveItem(this);
            }
            else
            {
                DaggerfallUI.MessageBox(string.Format("You are not hungry enough to eat the {0} right now.", ItemTemplate.name));
            }
            return true;
        }
    }

    public class ItemApple : AbstractItemFood
    {
        public const int templateIndex = 532;

        public ItemApple() : base(ItemGroups.UselessItems2, templateIndex)
        {
        }

        public override uint GetCalories()
        {
            return 60;
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemApple).ToString();
            return data;
        }
    }

    public class ItemBread : AbstractItemFood
    {
        public const int templateIndex = 534;

        public ItemBread() : base(ItemGroups.UselessItems2, templateIndex)
        {
        }

        public override uint GetCalories()
        {
            return 180;
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemBread).ToString();
            return data;
        }
    }

    public class ItemMeat : AbstractItemFood
    {
        public const int templateIndex = 537;

        public ItemMeat() : base(ItemGroups.UselessItems2, templateIndex)
        {
        }

        public override uint GetCalories()
        {
            return 240;
        }

        public override string GetFoodStatus()
        {
            switch (FoodStatus)
            {
                case StatusStale:
                    return "Smelly ";
                default:
                    return base.GetFoodStatus();
            }
        }

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemMeat).ToString();
            return data;
        }
    }

}

