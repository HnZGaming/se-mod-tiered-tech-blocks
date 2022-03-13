using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using HNZ.Utils.Logging;
using VRage.Utils;

namespace HNZ.TieredTechBlocks
{
    [Serializable]
    public class Config
    {
        public static Config Instance { get; set; }

        [XmlElement]
        public bool AgreeToKeepThisModPrivate;

        [XmlElement]
        public TechProperty Common;

        [XmlElement]
        public TechProperty Rare;

        [XmlElement]
        public TechProperty Exotic;

        [XmlElement]
        public List<string> ExcludeGridNames;

        [XmlElement]
        public List<CargoReplacement> CargoReplacements;

        [XmlElement]
        public List<LogConfig> LogConfigs;

        public static Config CreateDefault() => new Config
        {
            Common = new TechProperty
            {
                Chance = 0.15f,
                MinAmount = 6,
                MaxAmount = 40,
                ForgeMod = 100,
                GpsRadius = 10,
                MaxForgeCount = 90,
            },
            Rare = new TechProperty
            {
                Chance = 0.07f,
                MinAmount = 3,
                MaxAmount = 20,
                ForgeMod = 200,
                GpsRadius = 10,
                MaxForgeCount = 60,
            },
            Exotic = new TechProperty
            {
                Chance = 0.02f,
                MinAmount = 2,
                MaxAmount = 10,
                ForgeMod = 300,
                GpsRadius = -1,
                MaxForgeCount = 30,
            },
            ExcludeGridNames = new List<string>
            {
                "respawn",
            },
            CargoReplacements = new List<CargoReplacement>
            {
                new CargoReplacement
                {
                    FactionTag = "PEAVER",
                    Tier = 4,
                    Chance = 0.2f,
                },
            },
            LogConfigs = new List<LogConfig>
            {
                new LogConfig
                {
                    Severity = MyLogSeverity.Info,
                    Prefix = "",
                },
            },
        };
    }

    [Serializable]
    public class TechProperty
    {
        [XmlAttribute]
        public float Chance;

        [XmlAttribute]
        public int MinAmount;

        [XmlAttribute]
        public int MaxAmount;

        [XmlAttribute]
        public int ForgeMod;

        // by meters
        //  0 -> no gps
        // -1 -> infinite gps (all players)
        [XmlAttribute]
        public int GpsRadius;

        // by minutes
        //  0 -> infinite
        [XmlAttribute]
        public int MaxForgeCount;

        [XmlAttribute]
        public bool Invincible;
    }

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