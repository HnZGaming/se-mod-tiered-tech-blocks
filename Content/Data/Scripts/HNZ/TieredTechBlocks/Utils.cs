using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace HNZ.TieredTechBlocks
{
    public static class Utils
    {
        public static List<IMyCargoContainer> GetCargoBlocks(IMyCubeGrid grid)
        {
            var blocks = new List<IMySlimBlock>();
            var cargoBlocks = new List<IMyCargoContainer>();

            grid.GetBlocks(blocks);
            foreach (var block in blocks)
            {
                IMyCargoContainer cargoBlock;
                if (TryGetCargoBlock(block, out cargoBlock))
                {
                    cargoBlocks.Add(cargoBlock);
                }
            }

            return cargoBlocks;
        }

        static bool TryGetCargoBlock(IMySlimBlock block, out IMyCargoContainer cargo)
        {
            cargo = block.FatBlock as IMyCargoContainer;
            if (cargo == null) return false;
            if (cargo.MarkedForClose) return false;
            if (!cargo.IsWorking) return false;
            return true;
        }

        public static bool TryGetSmallCargo(IMyCargoContainer cargoBlock, out IMyCargoContainer smallCargo)
        {
            if (cargoBlock.SlimBlock.BlockDefinition.Id.SubtypeName == "LargeBlockSmallContainer") // nonalloc
            {
                smallCargo = cargoBlock;
                return true;
            }

            smallCargo = default(IMyCargoContainer);
            return false;
        }
    }
}