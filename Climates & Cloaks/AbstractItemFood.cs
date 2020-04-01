// Project:         Filling Food mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Ralzar
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Ralzar

using DaggerfallWorkshop.Game.Items;

namespace FillingFood
{
    public class AbstractItemFood : DaggerfallUnityItem
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

        public void RotFood()
        {
            if (FoodStatus < StatusPutrid)
            {
                FoodStatus++;
                shortName = GetFoodStatus() + ItemTemplate.name;
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
    }
}

