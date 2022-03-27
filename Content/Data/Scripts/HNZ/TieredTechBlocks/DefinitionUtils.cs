using System;
using HNZ.Utils;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;

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

            ObjectBuilderUtils.TryCreatePhysicalObjectBuilder("Component", "Tech2x", out TechComp2xBuilder);
            ObjectBuilderUtils.TryCreatePhysicalObjectBuilder("Component", "Tech4x", out TechComp4xBuilder);
            ObjectBuilderUtils.TryCreatePhysicalObjectBuilder("Component", "Tech8x", out TechComp8xBuilder);
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

        public static string ForgeBlockSubtypeName(int tier)
        {
            switch (tier)
            {
                case 2: return "TierForge2x";
                case 4: return "TierForge4x";
                case 8: return "TierForge8x";
                default: throw new InvalidOperationException($"invalid tier: {tier}");
            }
        }
    }
}