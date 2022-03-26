using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

namespace HNZ.TieredTechBlocks
{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_CargoContainer), false)]
	public class CargoUpgradeHandler : UpgradeHandler<IMyCargoContainer, MyCargoContainerDefinition>
	{
		public CargoUpgradeHandler() : base(new Dictionary<string, string>()
        {
/*			{ "LargeBlockSmallContainer", "LargeBlockSmallContainer2x" },
			{ "LargeBlockSmallContainer2x", "LargeBlockSmallContainer4x" },
			{ "LargeBlockSmallContainer4x", "LargeBlockSmallContainer8x" }*/
		}) {}

		protected static bool init = false;
		public override void UpdateOnceBeforeFrame()
		{
			if (!init)
			{
				Buttons();
				init = true;
			}
		}

        public override void TransferCustomSettings(IMyCargoContainer oldBlock, IMyCargoContainer newBlock)
        {
            base.TransferCustomSettings(oldBlock, newBlock);
        }
    }
}