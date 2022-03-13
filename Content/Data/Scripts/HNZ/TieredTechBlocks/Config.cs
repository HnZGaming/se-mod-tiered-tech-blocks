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
        public TechProperty Common;

        [XmlElement]
        public TechProperty Rare;

        [XmlElement]
        public TechProperty Exotic;

        [XmlElement]
        public List<string> ExcludeGridNames;

        [XmlElement]
        public bool AgreeToKeepThisModPrivate;

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
                GpsRadius = 10000,
                LifeSpanMinutes = 30,
            },
            Rare = new TechProperty
            {
                Chance = 0.07f,
                MinAmount = 3,
                MaxAmount = 20,
                ForgeMod = 200,
                GpsRadius = 10000,
                LifeSpanMinutes = 30,
            },
            Exotic = new TechProperty
            {
                Chance = 0.02f,
                MinAmount = 2,
                MaxAmount = 10,
                ForgeMod = 300,
                GpsRadius = -1,
                LifeSpanMinutes = 60,
            },
            ExcludeGridNames = new List<string>
            {
                "respawn",
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
        [XmlElement]
        public float Chance;

        [XmlElement]
        public int MinAmount;

        [XmlElement]
        public int MaxAmount;

        [XmlElement]
        public int ForgeMod;

        // by meters
        //  0 -> no gps
        // -1 -> infinite gps (all players)
        [XmlElement]
        public int GpsRadius;

        // by minutes
        //  0 -> infinite lifespan and invincible
        // -1 -> can be destroyed
        [XmlElement]
        public float LifeSpanMinutes;

        [XmlElement]
        public float DestroyOnSpawnChance;
    }
}