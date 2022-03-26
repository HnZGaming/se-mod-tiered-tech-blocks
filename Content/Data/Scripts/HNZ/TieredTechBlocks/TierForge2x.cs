using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;

namespace HNZ.TieredTechBlocks
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CargoContainer), true, "TierForge2x")]
    public sealed class TierForge2x : TierForgeBase
    {
        protected override int ForgeMod => Config.Instance.Common.ForgeMod;
        protected override int MaxForgeCount => Config.Instance.Common.MaxForgeCount;
        protected override float GpsRadius => Config.Instance.Common.GpsRadius;
        protected override float DamageMultiply => Config.Instance.Common.DamageMultiply;
        protected override string TierString => "Common";

        protected override bool CanForge(MyItemType itemType, out MyObjectBuilder_PhysicalObject builder)
        {
            builder = DefinitionUtils.TechComp2xBuilder;
            return DefinitionUtils.IsTech2xSource(itemType);
        }
    }
}