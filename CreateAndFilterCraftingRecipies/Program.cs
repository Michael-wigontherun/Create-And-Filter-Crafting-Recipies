using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System.Text.Json;

namespace CreateAndFilterCraftingRecipies
{
    public static class Program
    {
        public static FormKeyHandler KeyHandler = new FormKeyHandler();

        public static Dictionary<FormKey, Reference> References = new Dictionary<FormKey, Reference>();

        public static Dictionary<BipedObjectFlag, FirstPersonFlagData> FirstPersonFlagDatas = new Dictionary<BipedObjectFlag, FirstPersonFlagData>();

        public static List<ConstructibleObject> ConstructibleObjects = new List<ConstructibleObject>();

        public static ConditionGlobal BookCondition = new ConditionGlobal();

        static void Main(string[] args)
        {
            if(args.Length == 2)
            {
                string FilePath = args[0];
                string OutputPath = args[1];

                if(File.Exists(FilePath) && Directory.Exists(OutputPath))
                {
                    try
                    {
                        Run(FilePath, OutputPath);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        Console.WriteLine("Press Enter to Close...");
                        Console.ReadLine();
                    }
                }
                else
                {
                    if (!File.Exists(FilePath))
                    {
                        Console.WriteLine("Invalid: " + FilePath);
                    }
                    if (!Directory.Exists(OutputPath))
                    {
                        Console.WriteLine("Invalid: " + OutputPath);
                    }
                    Console.WriteLine("Press Enter to Close...");
                    Console.ReadLine();
                }
            }
            else
            {
                Console.WriteLine("Arguments are not correct.");
                Console.WriteLine("First Argument is the Absolute path to the main plugin");
                Console.WriteLine("Second Argument is the path to output folder");
                Console.WriteLine("Press Enter to Close...");
                Console.ReadLine();
            }
        }

        public static void Run(string filePath, string outputPath)
        {
            FirstPersonFlagDatas = JsonSerializer.Deserialize<Dictionary<BipedObjectFlag, FirstPersonFlagData>>(File.ReadAllText("Properties\\FirstPersonFlagData.json"))!;

            ISkyrimModGetter mod = SkyrimMod.CreateFromBinary(filePath, SkyrimRelease.SkyrimSE);
            SkyrimMod newMod = new SkyrimMod("[Craft] " + mod.ModKey.FileName, SkyrimRelease.SkyrimSE);

            KeyHandler.ModKey = newMod.ModKey;

            Book craftingBook = new Book(KeyHandler.GetNewFormKey(), SkyrimRelease.SkyrimSE);
            craftingBook.EditorID = "[" + Path.GetFileNameWithoutExtension(mod.ModKey.FileName) + "] Crafting Book";
            craftingBook.Name = "[" + Path.GetFileNameWithoutExtension(mod.ModKey.FileName) + "] Crafting Book";
            craftingBook.Model = new();
            craftingBook.Model.File.RawPath = "Clutter\\Books\\Book02LowPoly.nif";
            craftingBook.BookText.String = "<p align=\"left\">[" + Path.GetFileNameWithoutExtension(mod.ModKey.FileName) + "] Crafting Book</p>";
            craftingBook.Keywords = new();
            craftingBook.Keywords.Add(FormKey.Factory("0937A2:Skyrim.esm"));
            craftingBook.Type = Book.BookType.BookOrTome;
            craftingBook.InventoryArt.FormKey = FormKey.Factory("08ADAF:Skyrim.esm");


            newMod.Books.Add(craftingBook);
            BuildBookCondition(craftingBook);

            GenerateReferences(mod);
            FormKey armorTable = FormKey.Factory("0ADB78:Skyrim.esm");
            FormKey grindstone = FormKey.Factory("088108:Skyrim.esm");
            FormKey smithingForge = FormKey.Factory("088105:Skyrim.esm");

            foreach (Reference reference in References.Values)
            {
                if(reference.PreMadeCraftingRecipe != null)
                {
                    reference.PreMadeCraftingRecipe.Conditions.Insert(0, BookCondition);
                    newMod.ConstructibleObjects.Add(reference.PreMadeCraftingRecipe);
                }
                else
                {
                    ConstructibleObject recipe = new ConstructibleObject(KeyHandler.GetNewFormKey(), SkyrimRelease.SkyrimSE);
                    recipe.EditorID = "Recipe" + mod.ModKey.Name + reference.EDID;
                    recipe.Conditions.Insert(0, BookCondition);
                    recipe.CreatedObject.FormKey = reference.Key;
                    recipe.WorkbenchKeyword.FormKey = smithingForge;
                    if (recipe.Items == null)
                    {
                        recipe.Items = new Noggog.ExtendedList<ContainerEntry>();
                    }
                    for (int i = 0; i < reference.Data.IngotTypes.Count; i++)
                    {
                        ContainerEntry entry = new ContainerEntry();
                        entry.Item.Item.FormKey = reference.Data.IngotTypes[i];
                        entry.Item.Count = reference.Data.IngotBaseAmounts[i];
                        recipe.Items.Add(entry);
                    }
                    newMod.ConstructibleObjects.Add(recipe);
                }

                if (reference.PreMadeCraftingRecipeTemper != null)
                {
                    reference.PreMadeCraftingRecipeTemper.Conditions.Insert(0, BookCondition);
                    newMod.ConstructibleObjects.Add(reference.PreMadeCraftingRecipeTemper);
                }
                else
                {
                    ConstructibleObject recipe = new ConstructibleObject(KeyHandler.GetNewFormKey(), SkyrimRelease.SkyrimSE);
                    recipe.EditorID = "Recipe" + mod.ModKey.Name + reference.EDID + "Temper";
                    recipe.Conditions.Insert(0, BookCondition);
                    recipe.CreatedObject.FormKey = reference.Key;

                    if (reference.ReferenceType == ReferenceTypes.Armor)
                    {
                        recipe.WorkbenchKeyword.FormKey = armorTable;
                    }
                    if (reference.ReferenceType == ReferenceTypes.Weapon)
                    {
                        recipe.WorkbenchKeyword.FormKey = grindstone;
                    }

                    if (recipe.Items == null)
                    {
                        recipe.Items = new Noggog.ExtendedList<ContainerEntry>();
                    }

                    ContainerEntry entry = new ContainerEntry();
                    entry.Item.Item.FormKey = reference.Data.IngotTypes[0];
                    entry.Item.Count = 1;
                    recipe.Items.Add(entry);

                    newMod.ConstructibleObjects.Add(recipe);
                }
            }

            if (KeyHandler.StillESPFE())
            {
                newMod.ModHeader.Flags |= SkyrimModHeader.HeaderFlag.LightMaster;
            }

            newMod.WriteToBinary(Path.Combine(outputPath, newMod.ModKey.FileName));
        }

        public static void BuildBookCondition(Book craftingBook)
        {
            FunctionConditionData functionConditionData = new FunctionConditionData();
            functionConditionData.Function = Condition.Function.GetItemCount;
            functionConditionData.ParameterOneRecord.FormKey = craftingBook.FormKey;
            functionConditionData.RunOnType = Condition.RunOnType.Subject;
            functionConditionData.Unknown3 = -1;

            BookCondition.Data = functionConditionData;
            BookCondition.CompareOperator = CompareOperator.GreaterThan;
        }
        
        public static Dictionary<FormKey, CraftingData> GenerateMaterialsList()
        {
            Dictionary<string, CraftingData> MaterialsJson = JsonSerializer.Deserialize<Dictionary<string, CraftingData>>(File.ReadAllText("Properties\\MaterialsList.json"))!;
            Dictionary<FormKey, CraftingData> Materials = new Dictionary<FormKey, CraftingData>();
            foreach (KeyValuePair<string, CraftingData> Material in MaterialsJson)
            {
                Material.Value.FixIngotTypes();
                Materials.Add(FormKey.Factory(Material.Key), Material.Value);
            }
            //Materials.Add(FormKey.Factory("06BBD4:Skyrim.esm"), new CraftingData(FormKey.Factory("05AD9D:Skyrim.esm"), 3)
            //    .AddIngot(FormKey.Factory("03AD5B:Skyrim.esm"), 1));                                                       //ArmorMaterialDaedric
            //Materials.Add(FormKey.Factory("06BBD5:Skyrim.esm"), new CraftingData(FormKey.Factory("03ADA4:Skyrim.esm"), 1));//ArmorMaterialDragonplate
            //Materials.Add(FormKey.Factory("06BBD6:Skyrim.esm"), new CraftingData(FormKey.Factory("03ADA3:Skyrim.esm"), 1));//ArmorMaterialDragonscale
            //Materials.Add(FormKey.Factory("06BBD7:Skyrim.esm"), new CraftingData(FormKey.Factory("0DB8A2:Skyrim.esm"), 1));//ArmorMaterialDwarven
            //Materials.Add(FormKey.Factory("06BBD8:Skyrim.esm"), new CraftingData(FormKey.Factory("05AD9D:Skyrim.esm"), 1));//ArmorMaterialEbony
            //Materials.Add(FormKey.Factory("06BBD9:Skyrim.esm"), new CraftingData(FormKey.Factory("05AD9F:Skyrim.esm"), 1));//ArmorMaterialElven
            //Materials.Add(FormKey.Factory("06BBDA:Skyrim.esm"), new CraftingData(FormKey.Factory("05AD9F:Skyrim.esm"), 1));//ArmorMaterialElvenGilded
            //Materials.Add(FormKey.Factory("06BBDC:Skyrim.esm"), new CraftingData(FormKey.Factory("05ADA1:Skyrim.esm"), 1));//ArmorMaterialGlass
            //Materials.Add(FormKey.Factory("06BBDD:Skyrim.esm"), new CraftingData(FormKey.Factory("0DB5D2:Skyrim.esm"), 2));//ArmorMaterialHide
            //Materials.Add(FormKey.Factory("06BBE2:Skyrim.esm"), new CraftingData(FormKey.Factory("05ACE5:Skyrim.esm"), 3));//ArmorMaterialImperialHeavy
            //Materials.Add(FormKey.Factory("06BBE0:Skyrim.esm"), new CraftingData(FormKey.Factory("05ACE5:Skyrim.esm"), 1));//ArmorMaterialImperialLight
            //Materials.Add(FormKey.Factory("06BBE1:Skyrim.esm"), new CraftingData(FormKey.Factory("05ACE5:Skyrim.esm"), 2));//ArmorMaterialImperialStudded
            //Materials.Add(FormKey.Factory("06BBE3:Skyrim.esm"), new CraftingData(FormKey.Factory("05ACE4:Skyrim.esm"), 1));//ArmorMaterialIron
            //Materials.Add(FormKey.Factory("06BBE4:Skyrim.esm"), new CraftingData(FormKey.Factory("05ACE4:Skyrim.esm"), 2));//ArmorMaterialIronBanded
            //Materials.Add(FormKey.Factory("06BBDB:Skyrim.esm"), new CraftingData(FormKey.Factory("0DB5D2:Skyrim.esm"), 1));//ArmorMaterialLeather
            //Materials.Add(FormKey.Factory("06BBE5:Skyrim.esm"), new CraftingData(FormKey.Factory("05AD99:Skyrim.esm"), 1));//ArmorMaterialOrcish
            //Materials.Add(FormKey.Factory("06BBDE:Skyrim.esm"), new CraftingData(FormKey.Factory("0DB5D2:Skyrim.esm"), 3));//ArmorMaterialScaled
            //Materials.Add(FormKey.Factory("06BBE6:Skyrim.esm"), new CraftingData(FormKey.Factory("05ACE5:Skyrim.esm"), 2));//ArmorMaterialSteel
            //Materials.Add(FormKey.Factory("06BBE7:Skyrim.esm"), new CraftingData(FormKey.Factory("05ACE5:Skyrim.esm"), 5));//ArmorMaterialSteelPlate
            //Materials.Add(FormKey.Factory("0AC13A:Skyrim.esm"), new CraftingData(FormKey.Factory("0DB5D2:Skyrim.esm"), 2));//ArmorMaterialStormcloak
            //Materials.Add(FormKey.Factory("06BBDF:Skyrim.esm"), new CraftingData(FormKey.Factory("0DB5D2:Skyrim.esm"), 5));//ArmorMaterialStudded
            //Materials.Add(FormKey.Factory("01E71F:Skyrim.esm"), new CraftingData(FormKey.Factory("05AD9D:Skyrim.esm"), 1)
            //    .AddIngot(FormKey.Factory("03AD5B:Skyrim.esm"), 1));                                                       //WeapMaterialDaedric
            //Materials.Add(FormKey.Factory("0C5C01:Skyrim.esm"), new CraftingData(FormKey.Factory("05ACE5:Skyrim.esm"), 2));//WeapMaterialDraugr
            //Materials.Add(FormKey.Factory("0C5C02:Skyrim.esm"), new CraftingData(FormKey.Factory("05ACE5:Skyrim.esm"), 3));//WeapMaterialDraugrHoned
            //Materials.Add(FormKey.Factory("01E71A:Skyrim.esm"), new CraftingData(FormKey.Factory("0DB8A2:Skyrim.esm"), 1));//WeapMaterialDwarven
            //Materials.Add(FormKey.Factory("01E71E:Skyrim.esm"), new CraftingData(FormKey.Factory("05AD9D:Skyrim.esm"), 1));//WeapMaterialEbony
            //Materials.Add(FormKey.Factory("01E71B:Skyrim.esm"), new CraftingData(FormKey.Factory("05AD9F:Skyrim.esm"), 1));//WeapMaterialElven
            //Materials.Add(FormKey.Factory("0C5C03:Skyrim.esm"), new CraftingData(FormKey.Factory("03AD57:Skyrim.esm"), 1));//WeapMaterialFalmer
            //Materials.Add(FormKey.Factory("0C5C04:Skyrim.esm"), new CraftingData(FormKey.Factory("03AD57:Skyrim.esm"), 2));//WeapMaterialFalmerHoned
            //Materials.Add(FormKey.Factory("01E71D:Skyrim.esm"), new CraftingData(FormKey.Factory("05ADA1:Skyrim.esm"), 1));//WeapMaterialGlass
            //Materials.Add(FormKey.Factory("0C5C00:Skyrim.esm"), new CraftingData(FormKey.Factory("05ACE5:Skyrim.esm"), 1));//WeapMaterialImperial
            //Materials.Add(FormKey.Factory("01E718:Skyrim.esm"), new CraftingData(FormKey.Factory("05ACE4:Skyrim.esm"), 1));//WeapMaterialIron
            //Materials.Add(FormKey.Factory("01E71C:Skyrim.esm"), new CraftingData(FormKey.Factory("05AD99:Skyrim.esm"), 1));//WeapMaterialOrcish
            //Materials.Add(FormKey.Factory("10AA1A:Skyrim.esm"), new CraftingData(FormKey.Factory("05ACE3:Skyrim.esm"), 1));//WeapMaterialSilver
            //Materials.Add(FormKey.Factory("01E719:Skyrim.esm"), new CraftingData(FormKey.Factory("05ACE5:Skyrim.esm"), 1));//WeapMaterialSteel
            //Materials.Add(FormKey.Factory("01E717:Skyrim.esm"), new CraftingData(FormKey.Factory("06F993:Skyrim.esm"), 1));//WeapMaterialWood
            return Materials;
        }

        public static Dictionary<FormKey, int> GenerateWeaponTypeList()
        {
            Dictionary<string, int> WeaponTypesJson = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText("Properties\\WeaponsList.json"))!;
            Dictionary<FormKey, int> WeaponTypes = new Dictionary<FormKey, int>();
            foreach(KeyValuePair<string, int> WeaponType in WeaponTypesJson)
            {
                WeaponTypes.Add(FormKey.Factory(WeaponType.Key), WeaponType.Value);
            }
            //WeaponTypes.Add(FormKey.Factory("01E711:Skyrim.esm"), 2);//WeapTypeSword
            //WeaponTypes.Add(FormKey.Factory("01E712:Skyrim.esm"), 3);//WeapTypeWarAxe
            //WeaponTypes.Add(FormKey.Factory("01E713:Skyrim.esm"), 1);//WeapTypeDagger
            //WeaponTypes.Add(FormKey.Factory("01E714:Skyrim.esm"), 3);//WeapTypeMace
            //WeaponTypes.Add(FormKey.Factory("01E715:Skyrim.esm"), 2);//WeapTypeBow
            //WeaponTypes.Add(FormKey.Factory("06D930:Skyrim.esm"), 4);//WeapTypeWarhammer
            //WeaponTypes.Add(FormKey.Factory("06D931:Skyrim.esm"), 3);//WeapTypeGreatsword
            //WeaponTypes.Add(FormKey.Factory("06D932:Skyrim.esm"), 4);//WeapTypeBattleaxe
            return WeaponTypes;
        }

        public static CraftingData GetArmorCraftingData(IArmorGetter armor, Dictionary<FormKey, CraftingData> Materials)
        {
            CraftingData data = new();
            int leather = 0;
            int leatherStrips = 0;
            int linen = 1;
            int armorMaterial;
            ArmorType? type = armor.BodyTemplate?.ArmorType;
            switch (type)
            {
                case ArmorType.Clothing:
                    armorMaterial = 1;
                    break;
                case ArmorType.LightArmor:
                    armorMaterial = 1;
                    break;
                case ArmorType.HeavyArmor:
                    armorMaterial = 2;
                    leather += 1;
                    leatherStrips += 3;
                    break;
                case null:
                    armorMaterial = 1;
                    break;
                default:
                    armorMaterial = 1;
                    break;
            }
            if(armor.BodyTemplate != null)
            {
                foreach(KeyValuePair<BipedObjectFlag, FirstPersonFlagData> flg in FirstPersonFlagDatas)
                {
                    if (armor.BodyTemplate.FirstPersonFlags.HasFlag(flg.Key))
                    {
                        armorMaterial += flg.Value.armorMaterial;
                        if (type != ArmorType.Clothing)
                        {
                            leather += flg.Value.leather;
                            leatherStrips += flg.Value.leatherStrips;
                        }
                        linen += flg.Value.linen;
                    }
                }
            }

            if (armor.Keywords != null)
            {
                foreach (IFormLinkGetter<IKeywordGetter>? keyword in armor.Keywords)
                {
                    if (Materials.TryGetValue(keyword.FormKey, out CraftingData? d))
                    {
                        data.ImportData(d);
                        data.IngotBaseAmounts[0] += armorMaterial; 
                        if (leather > 0) data.AddIngot(FormKey.Factory("0DB5D2:Skyrim.esm"), leather);
                        if (leatherStrips > 0) data.AddIngot(FormKey.Factory("0800e4:Skyrim.esm"), leatherStrips);
                    }
                }
            }

            if (data.IngotTypes.Count == 0)
            {
                int floorDiv()
                {
                    double d = leather / 3.0;
                    return (int)Math.Floor(d);
                }
                data.AddIngot(FormKey.Factory("034cd6:Skyrim.esm"), linen);
                if (leather > 0) 
                { 
                    data.AddIngot(FormKey.Factory("0DB5D2:Skyrim.esm"), floorDiv());
                }
            }
            return data;
        }

        public static CraftingData GetWeaponCraftingData(IWeaponGetter weapon, Dictionary<FormKey, CraftingData> Materials, Dictionary<FormKey, int> WeaponTypes)
        {
            CraftingData data = new CraftingData();
            int leatherStrips = 1;
            int weaponMaterial = 0;

            if (weapon.Keywords != null)
            {
                foreach (IFormLinkGetter<IKeywordGetter>? keyword in weapon.Keywords)
                {
                    if (WeaponTypes.TryGetValue(keyword.FormKey, out weaponMaterial))
                    {
                        leatherStrips += weaponMaterial / 3;
                    }
                }
                foreach (IFormLinkGetter<IKeywordGetter>? keyword in weapon.Keywords)
                {
                    if (Materials.TryGetValue(keyword.FormKey, out CraftingData? craftingData))
                    {
                        data = craftingData;
                        data.IngotBaseAmounts[0] = data.IngotBaseAmounts[0] + weaponMaterial;
                        data.AddIngot(FormKey.Factory("0800e4:Skyrim.esm"), leatherStrips);
                    }
                }
            }

            if (data.IngotTypes.Count() == 0)
            {
                data.AddIngot(FormKey.Factory("05ACE5:Skyrim.esm"), weaponMaterial);
                data.AddIngot(FormKey.Factory("0DB5D2:Skyrim.esm"), leatherStrips);
            }

            return data;
        }

        public static void GenerateReferences(ISkyrimModGetter mod)
        {
            Dictionary<FormKey, CraftingData> Materials = GenerateMaterialsList();
            Dictionary<FormKey, int> WeaponTypes = GenerateWeaponTypeList();

            foreach (IArmorGetter? a in mod.Armors)
            {
                Reference reference = new Reference();
                reference.EDID = (a.EditorID ?? a.Name?.String) ?? "EmptyName";
                reference.ReferenceType = ReferenceTypes.Armor;
                reference.Key = a.FormKey;
                reference.Data = GetArmorCraftingData(a, Materials);
                if(reference.Data.IngotTypes.Count() > 0) References.Add(a.FormKey, reference);
            }

            foreach (IWeaponGetter? w in mod.Weapons)
            {
                Reference reference = new Reference();
                reference.EDID = (w.EditorID ?? w.Name?.String) ?? "EmptyName";
                reference.ReferenceType = ReferenceTypes.Weapon;
                reference.Key = w.FormKey;
                reference.Data = GetWeaponCraftingData(w, Materials, WeaponTypes);
                if (reference.Data.IngotTypes.Count() > 0) References.Add(w.FormKey, reference);
            }

            FormKey armorTable = FormKey.Factory("0ADB78:Skyrim.esm");
            FormKey grindstone = FormKey.Factory("088108:Skyrim.esm");

            foreach(IConstructibleObjectGetter? recipe in mod.ConstructibleObjects)
            {
                if (References.ContainsKey(recipe.CreatedObject.FormKey))
                {
                    if(recipe.WorkbenchKeyword.FormKey.Equals(armorTable) || recipe.WorkbenchKeyword.FormKey.Equals(grindstone))
                    {
                        References[recipe.CreatedObject.FormKey].PreMadeCraftingRecipeTemper = recipe.DeepCopy();
                    }
                    else
                    {
                        References[recipe.CreatedObject.FormKey].PreMadeCraftingRecipe = recipe.DeepCopy();
                    }
                }
                else
                {
                    ConstructibleObjects.Add(recipe.DeepCopy());
                }

            }

        }
    }
}