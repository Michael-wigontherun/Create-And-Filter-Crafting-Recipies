using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CreateAndFilterCraftingRecipies
{
    public enum ReferenceTypes
    {
        Armor,
        Weapon
    }
    
    public class Reference
    {
        public ReferenceTypes ReferenceType { get; set; }
        public string EDID = string.Empty;
        public FormKey Key { get; set; }
        public ConstructibleObject? PreMadeCraftingRecipe { get; set; }
        public ConstructibleObject? PreMadeCraftingRecipeTemper { get; set; }
        public CraftingData Data { get; set; } = new CraftingData();
    }

    public class CraftingData
    {
        [JsonIgnore]
        public List<FormKey> IngotTypes = new List<FormKey>();
        [JsonInclude]
        public List<string> IngotTypesString = new List<string>();

        [JsonInclude]
        public List<int> IngotBaseAmounts = new List<int>();

        public CraftingData() { }

        public CraftingData(FormKey ingotType, int baseAmount)
        {
            IngotTypes.Add(ingotType);
            IngotBaseAmounts.Add(baseAmount);
        }

        public CraftingData AddIngot(FormKey ingotType, int baseAmount)
        {
            bool added = false;
            for(int i = 0; i < IngotTypes.Count; i++)
            {
                if (IngotTypes[i].Equals(ingotType))
                {
                    IngotBaseAmounts[i] += baseAmount;
                    added = true;
                }
            }

            if (!added)
            {
                IngotTypes.Add(ingotType);
                IngotBaseAmounts.Add(baseAmount);
            }

            return this;
        }

        public void ImportData(CraftingData additionalData)
        {
            for (int i = 0; i < additionalData.IngotTypes.Count; i++)
            {
                IngotTypes.Add(additionalData.IngotTypes[i]);
                IngotBaseAmounts.Add(additionalData.IngotBaseAmounts[i]);
            }
        }

        public void FixIngotTypes()
        {
            foreach(string ingotTypeString in IngotTypesString)
            {
                IngotTypes.Add(FormKey.Factory(ingotTypeString));
            }
        }

    }

    public class FirstPersonFlagData
    {
        [JsonInclude]
        public int leather = 0;
        [JsonInclude]
        public int leatherStrips = 0;
        [JsonInclude]
        public int linen = 0;
        [JsonInclude]
        public int armorMaterial = 0;
    }

}
