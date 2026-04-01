namespace Pillaro.Dataverse.PluginFramework.Plugins.Features.Autonumbering
{
    public partial class AutoNumberFormatRenderer
    {
        #region Nested types

        public sealed class TokenInfo
        {
            public TokenInfo(string raw, string attributeName, string formatKey, TokenType type, string parentLookupAttribute)
            {
                Raw = raw;
                AttributeName = attributeName;
                FormatKey = formatKey;
                Type = type;
                ParentLookupAttribute = parentLookupAttribute;
            }

            public string Raw { get; }
            public string AttributeName { get; }
            public string FormatKey { get; }
            public TokenType Type { get; }
            public string ParentLookupAttribute { get; }
        }

        #endregion
    }
}