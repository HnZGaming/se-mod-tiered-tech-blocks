using System;
using System.Xml.Serialization;
using HNZ.Utils;

namespace HNZ.TieredTechBlocks
{
    [Serializable]
    public class TierForgeSpec
    {
        [XmlAttribute]
        public string FactionTag;

        [XmlAttribute]
        public string SpawnGroupId;

        [XmlAttribute]
        public int Tier;

        [XmlAttribute]
        public float Chance;

        public static TierForgeSpec CreateDefault() => new TierForgeSpec
        {
            FactionTag = "FOO",
            SpawnGroupId = "Bababooey",
            Tier = 4,
            Chance = 0.2f,
        };
    }
}