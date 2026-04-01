namespace Pillaro.Dataverse.PluginFramework.Plugins.Features.Autonumbering
{
    public partial class AutoNumberFormatRenderer
    {
        #region Nested types

        public sealed class FormatConfig
        {
            public FormatConfig(int digitCount, string dateFormat1, string dateFormat2, string dateFormat3)
            {
                DigitCount = digitCount;
                DateFormat1 = dateFormat1;
                DateFormat2 = dateFormat2;
                DateFormat3 = dateFormat3;
            }

            public int DigitCount { get; }
            public string DateFormat1 { get; }
            public string DateFormat2 { get; }
            public string DateFormat3 { get; }
        }

        #endregion
    }
}