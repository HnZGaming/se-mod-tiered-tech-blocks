using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using HNZ.Utils;
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

        [XmlArray]
        [XmlArrayItem("item")]
        public List<string> LootFactionTags;

        [XmlArray]
        [XmlArrayItem("item")]
        public List<TierForgeSpec> ForgeSpecs;

        [XmlArray]
        [XmlArrayItem("item")]
        public List<LogConfig> LogConfigs;

        [XmlElement]
        public string DataPadDescription;

        public void TryInitialize()
        {
            LangUtils.AssertNull(Common);
            LangUtils.AssertNull(Rare);
            LangUtils.AssertNull(Exotic);
            LangUtils.NullOrDefault(ref LootFactionTags, new List<string>());
            LangUtils.NullOrDefault(ref ForgeSpecs, new List<TierForgeSpec>());
            LangUtils.NullOrDefault(ref LogConfigs, new List<LogConfig>());
            LangUtils.NullOrDefault(ref DataPadDescription, "");
        }

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
            LootFactionTags = new List<string>
            {
                "FOO",
            },
            ForgeSpecs = new List<TierForgeSpec>
            {
                TierForgeSpec.CreateDefault(),
            },
            LogConfigs = new List<LogConfig>
            {
                new LogConfig
                {
                    Severity = MyLogSeverity.Info,
                    Prefix = "",
                },
            },
            DataPadDescription = "You can forge your {0} source components into {0} tech components using this block.",
        };
    }
}