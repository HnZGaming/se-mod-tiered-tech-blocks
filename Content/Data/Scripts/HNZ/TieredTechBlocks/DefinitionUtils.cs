using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;

namespace HNZ.TieredTechBlocks
{
    public static class DefinitionUtils
    {
        public static readonly MyObjectBuilder_PhysicalObject TechComp2xBuilder;
        public static readonly MyObjectBuilder_PhysicalObject TechComp4xBuilder;
        public static readonly MyObjectBuilder_PhysicalObject TechComp8xBuilder;

        public static readonly MyObjectBuilder_Component TechSource2xBuilder;
        public static readonly MyObjectBuilder_Component TechSource4xBuilder;
        public static readonly MyObjectBuilder_Component TechSource8xBuilder;

        static DefinitionUtils()
        {
            TechSource2xBuilder = new MyObjectBuilder_Component { SubtypeName = "Tech2xSource" };
            TechSource4xBuilder = new MyObjectBuilder_Component { SubtypeName = "Tech4xSource" };
            TechSource8xBuilder = new MyObjectBuilder_Component { SubtypeName = "Tech8xSource" };

            foreach (var itemDefinition in MyDefinitionManager.Static.GetPhysicalItemDefinitions())
            {
                var typeId = itemDefinition.Id.TypeId.ToString().Split('_')[1];
                var subtypeId = itemDefinition.Id.SubtypeName;
                if (typeId == "Component")
                {
                    switch (subtypeId)
                    {
                        case "Tech2x":
                        {
                            TechComp2xBuilder = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemDefinition.Id);
                            break;
                        }
                        case "Tech4x":
                        {
                            TechComp4xBuilder = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemDefinition.Id);
                            break;
                        }
                        case "Tech8x":
                        {
                            TechComp8xBuilder = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemDefinition.Id);
                            break;
                        }
                    }
                }
            }
        }

        public static bool IsTech2xSource(MyItemType itemType)
        {
            return itemType.TypeId == "MyObjectBuilder_Component" && itemType.SubtypeId == "Tech2xSource";
        }

        public static bool IsTech4xSource(MyItemType itemType)
        {
            return itemType.TypeId == "MyObjectBuilder_Component" && itemType.SubtypeId == "Tech4xSource";
        }

        public static bool IsTech8xSource(MyItemType itemType)
        {
            return itemType.TypeId == "MyObjectBuilder_Component" && itemType.SubtypeId == "Tech8xSource";
        }
    }
}