using System;
using System.Collections.Generic;

namespace Pillaro.Dataverse.PluginFramework.Plugins.Features.Autonumbering
{
    public partial class AutoNumberFormatRenderer
    {
        #region Nested types

        public sealed class RenderPlan
        {
            internal RenderPlan(string partialFormat, FormatConfig config)
            {
                PartialFormat = partialFormat;
                Config = config;
            }

            public string PartialFormat { get; }
            internal FormatConfig Config { get; }
            public List<TokenInfo> Tokens { get; } = new List<TokenInfo>();
            public HashSet<string> RootAttributes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, HashSet<string>> ParentLookups { get; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            public bool HasDynamicTokens => Tokens.Count > 0;
        }

        #endregion
    }
}