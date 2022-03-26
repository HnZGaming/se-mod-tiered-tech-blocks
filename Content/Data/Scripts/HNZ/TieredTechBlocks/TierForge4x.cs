using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;

namespace HNZ.TieredTechBlocks
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CargoContainer), true, "TierForge4x")]
    public sealed class TierForge4x : TierForgeBase
    {
        protected override int ForgeMod => Config.Instance.Rare.ForgeMod;
        protected override int MaxForgeCount => Config.Instance.Rare.MaxForgeCount;
        protected override float GpsRadius => Config.Instance.Rare.GpsRadius;
        protected override float DamageMultiply => Config.Instance.Rare.DamageMultiply;

        protected override bool CanForge(MyItemType itemType, out MyObjectBuilder_PhysicalObject builder)
        {
            builder = DefinitionUtils.TechComp4xBuilder;
            return DefinitionUtils.IsTech4xSource(itemType);
        }
    }
}