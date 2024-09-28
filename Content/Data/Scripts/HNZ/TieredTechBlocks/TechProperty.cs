using System;
using System.Xml.Serialization;

namespace HNZ.TieredTechBlocks
{
    [Serializable]
    public class TechProperty
    {
        [XmlAttribute]
        public float Chance;

        [XmlAttribute]
        public int MinAmount;

        [XmlAttribute]
        public int MaxAmount;

        // by meters
        //  0 -> no gps
        // -1 -> infinite gps (all players)
        [XmlAttribute]
        public int GpsRadius;
    }
}