using System;
using System.Xml.Serialization;

namespace HNZ.TieredTechBlocks
{
    [Serializable]
    public class CargoReplacement
    {
        [XmlAttribute]
        public string FactionTag;

        [XmlAttribute]
        public int Tier;

        [XmlAttribute]
        public float Chance;
    }
}