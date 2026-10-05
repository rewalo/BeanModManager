using System;
using System.Collections.Generic;
using System.Linq;

namespace BeanModManager.Helpers
{
    /// <summary>
    /// Canonical game channels and helpers for parsing mod bundle labels.
    ///
    /// Since Among Us moved to x64, mod releases can ship a single bundle for
    /// Steam/Epic Games/Microsoft Store and a separate (older) bundle for itch.io.
    /// Older releases shipped per-store bundles (e.g. "Steam", "Epic/MS Store").
    /// Bundle compatibility is derived from keywords in the asset file name or,
    /// for keyword-free names, from the registry filter that selected the asset.
    /// </summary>
    public static class GameChannels
    {
        public const string Steam = "Steam";
        public const string EpicGames = "Epic Games";
        public const string MicrosoftStore = "Microsoft Store";
        public const string ItchIo = "itch.io";

        public const string LegacySteamItch = "Steam/Itch.io";
        public const string LegacyEpicMs = "Epic/MS Store";

        [Flags]
        public enum BundleChannels
        {
            None = 0,
            Steam = 1,
            Epic = 2,
            Microsoft = 4,
            Itch = 8,
            Universal = 16
        }

        private static readonly string[] NeutralLabels = { "DLL Only", "Custom", "Installed" };

        public static bool IsNeutralLabel(string gameVersionLabel)
        {
            return string.IsNullOrEmpty(gameVersionLabel) || NeutralLabels.Any(l =>
                string.Equals(l, gameVersionLabel, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Normalizes a stored channel value (which may be a legacy grouped value)
        /// to one of the four canonical channels. When the value is ambiguous the
        /// channel detected from the game path wins; otherwise a sane default.
        /// </summary>
        public static string NormalizeChannel(string storedChannel, string pathDetectedChannel)
        {
            if (string.IsNullOrEmpty(storedChannel))
            {
                return !string.IsNullOrEmpty(pathDetectedChannel) ? pathDetectedChannel : Steam;
            }

            switch (storedChannel)
            {
                case Steam:
                case EpicGames:
                case MicrosoftStore:
                case ItchIo:
                    return storedChannel;

                case LegacySteamItch:
                    if (pathDetectedChannel == ItchIo)
                    {
                        return ItchIo;
                    }

                    if (pathDetectedChannel == EpicGames || pathDetectedChannel == MicrosoftStore)
                    {
                        return pathDetectedChannel;
                    }

                    return Steam;

                case LegacyEpicMs:
                    if (pathDetectedChannel == MicrosoftStore || pathDetectedChannel == EpicGames)
                    {
                        return pathDetectedChannel;
                    }

                    return EpicGames;

                default:
                    return !string.IsNullOrEmpty(pathDetectedChannel) ? pathDetectedChannel : Steam;
            }
        }

        /// <summary>
        /// Extracts the channels a release asset supports from keywords in its
        /// file name (e.g. "EHR.v8.0.2_Steam_Epic-Games_Microsoft-Store.zip").
        /// </summary>
        public static BundleChannels ParseChannelsFromAssetName(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return BundleChannels.None;
            }

            string name = assetName.ToLowerInvariant();
            BundleChannels channels = BundleChannels.None;

            if (name.Contains("steam"))
            {
                channels |= BundleChannels.Steam;
            }

            if (name.Contains("epic"))
            {
                channels |= BundleChannels.Epic;
            }

            if (name.Contains("itch"))
            {
                channels |= BundleChannels.Itch;
            }

            if (name.Contains("microsoft") || name.Contains("msstore") || name.Contains("ms store") ||
                name.Contains("ms-store") || name.Contains("ms_store") || ContainsMsToken(name))
            {
                channels |= BundleChannels.Microsoft;
            }

            return channels;
        }

        // Matches a standalone "ms" token (e.g. "NewMod-MS.zip") without
        // matching the "ms" inside words like "games".
        private static bool ContainsMsToken(string lowerName)
        {
            string[] tokens = lowerName.Split(new[] { '-', '_', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
            return tokens.Any(t => t == "ms");
        }

        /// <summary>
        /// Formats a channel set as a display label, e.g. "Steam/Epic/MS Store",
        /// "Itch.io", "Steam", "Epic/MS Store", "Steam/Itch.io", "Universal".
        /// </summary>
        public static string GetBundleLabel(BundleChannels channels)
        {
            if (channels.HasFlag(BundleChannels.Universal))
            {
                return "Universal";
            }

            List<string> parts = new List<string>(4);
            if (channels.HasFlag(BundleChannels.Steam))
            {
                parts.Add("Steam");
            }

            if (channels.HasFlag(BundleChannels.Epic))
            {
                parts.Add("Epic");
            }

            if (channels.HasFlag(BundleChannels.Microsoft))
            {
                parts.Add("MS Store");
            }

            if (channels.HasFlag(BundleChannels.Itch))
            {
                parts.Add("Itch.io");
            }

            return parts.Count > 0 ? string.Join("/", parts) : null;
        }

        public static BundleChannels GetChannelsForLabel(string gameVersionLabel)
        {
            return ParseChannelsFromAssetName(gameVersionLabel);
        }

        /// <summary>
        /// True when a bundle label is compatible with the given canonical channel.
        /// Neutral labels (null, "DLL Only", "Custom", "Installed") match everything.
        /// </summary>
        public static bool LabelSupportsChannel(string gameVersionLabel, string channel)
        {
            if (IsNeutralLabel(gameVersionLabel))
            {
                return true;
            }

            BundleChannels labelChannels = GetChannelsForLabel(gameVersionLabel);
            return labelChannels == BundleChannels.None || ChannelMatches(channel, labelChannels);
        }

        public static bool ChannelMatches(string channel, BundleChannels channels)
        {
            switch (channel)
            {
                case EpicGames:
                    return channels.HasFlag(BundleChannels.Epic);
                case MicrosoftStore:
                    return channels.HasFlag(BundleChannels.Microsoft);
                case ItchIo:
                    return channels.HasFlag(BundleChannels.Itch);
                case Steam:
                default:
                    return channels.HasFlag(BundleChannels.Steam);
            }
        }

        /// <summary>
        /// Whether an available bundle may update an installed one under the current
        /// channel. Neutral labels keep legacy exact-match behavior; channel labels
        /// must support the current channel (so e.g. an old "(Steam)" install updates
        /// to a newer "(Steam/Epic/MS Store)" bundle, but itch users never pick it).
        /// </summary>
        public static bool IsCompatibleUpdate(string installedLabel, string availableLabel, string channel)
        {
            if (string.Equals(installedLabel, availableLabel, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (IsNeutralLabel(installedLabel) || IsNeutralLabel(availableLabel))
            {
                return false;
            }

            if (!LabelSupportsChannel(availableLabel, channel))
            {
                return false;
            }

            BundleChannels installedChannels = GetChannelsForLabel(installedLabel);
            BundleChannels availableChannels = GetChannelsForLabel(availableLabel);

            // Universal bundles (no channel keywords) update anything.
            return availableChannels == BundleChannels.None || (installedChannels & availableChannels) != BundleChannels.None ||
                   ChannelMatches(channel, availableChannels);
        }
    }
}
