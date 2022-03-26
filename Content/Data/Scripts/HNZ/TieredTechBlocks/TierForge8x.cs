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
        protected override float DamageMultiply => Config.Instance.Exotic.DamageMultiply;
        protected override string TierString => "Exotic";

        protected override bool CanForge(MyItemType itemType, out MyObjectBuilder_PhysicalObject builder)
        {
            builder = DefinitionUtils.TechComp8xBuilder;
            return DefinitionUtils.IsTech8xSource(itemType);
        }
    }
}