using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;

namespace HNZ.TieredTechBlocks
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CargoContainer), true, "TierForge8x")]
    public sealed class TierForge8x : TierForgeBase
    {
        protected override int ForgeMod => Config.Instance.Exotic.ForgeMod;
        protected override int MaxForgeCount => Config.Instance.Exotic.MaxForgeCount;
        protected override float GpsRadius => Config.Instance.Exotic.GpsRadius;
        protected override string TierString => "Exotic";

        protected override bool CanForge(MyItemType itemType, out MyObjectBuilder_PhysicalObject builder)
        {
            if (DefinitionUtils.IsTech8xSource(itemType))
            {
                builder = DefinitionUtils.TechComp8xBuilder;
                return true;
            }

            if (DefinitionUtils.IsTech4xSource(itemType))
            {
                builder = DefinitionUtils.TechComp4xBuilder;
                return true;
            }

            if (DefinitionUtils.IsTech2xSource(itemType))
            {
                builder = DefinitionUtils.TechComp2xBuilder;
                return true;
            }

            builder = null;
            return false;
        }
    }
}