using Mutagen.Bethesda.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateAndFilterCraftingRecipies
{
    public class FormKeyHandler
    {
        public uint ID { get; private set; } = 0x800;

        public ModKey ModKey { get; set; }

        public FormKeyHandler(){}

        public FormKeyHandler(ModKey modKey)
        {
            ModKey = modKey;
        }

        public bool StillESPFE()
        {
            return ID <= 0xfff;
        }

        public FormKey GetNewFormKey()
        {
            FormKey key = new FormKey(ModKey, ID);
            ID++;
            if(ID > 0xfff)
            {
                ID = 0x1;
            }
            return key;
        }
    }
}
