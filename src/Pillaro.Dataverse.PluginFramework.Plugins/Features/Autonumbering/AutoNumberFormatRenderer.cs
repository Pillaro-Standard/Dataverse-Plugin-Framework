using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Pillaro.Dataverse.PluginFramework.Plugins.Features.Autonumbering
{
    public partial class AutoNumberFormatRenderer
    {
        private static readonly Regex TokenRegex = new Regex("(?<={).+?(?=})", RegexOptions.Compiled);
        private static readonly Regex NumTokenRegex = new Regex("{NUM}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
  
        public RenderPlan Analyze(string formatString, FormatConfig config, int nextNumber)
        {
            if (string.IsNullOrWhiteSpace(formatString))
                throw new InvalidPluginExecutionException("Format string is empty.");
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var partialFormat = ReplaceStaticTokens(formatString, config, nextNumber);
            return BuildRenderPlan(partialFormat, config);
        }

        public string Render(RenderPlan plan, Entity rootEntity, IDictionary<string, Entity> parentEntities)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (!plan.HasDynamicTokens)
                return plan.PartialFormat;

            var number = plan.PartialFormat;
            foreach (var token in plan.Tokens)
            {
                var replacement = ResolveTokenValue(token, rootEntity, parentEntities, plan.Config) ?? string.Empty;
                number = Regex.Replace(number, "{" + Regex.Escape(token.Raw) + "}", replacement, RegexOptions.IgnoreCase);
            }
            return number;
        }

        private string ReplaceStaticTokens(string format, FormatConfig config, int nextNumber)
        {
            var result = NumTokenRegex.Replace(format, nextNumber.ToString().PadLeft(config.DigitCount, '0'));
            result = ReplaceDateToken(result, "{date1}", config.DateFormat1);
            result = ReplaceDateToken(result, "{date2}", config.DateFormat2);
            result = ReplaceDateToken(result, "{date3}", config.DateFormat3);
            return result;
        }

        private string ReplaceDateToken(string number, string token, string format)
        {
            if (number.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
                return number;

            if (string.IsNullOrWhiteSpace(format))
                throw new InvalidPluginExecutionException($"The format contains token {token}, but the corresponding date format configuration is empty.");

            return Regex.Replace(number, Regex.Escape(token), DateTime.UtcNow.ToString(format), RegexOptions.IgnoreCase);
        }

        private static RenderPlan BuildRenderPlan(string partialFormat, FormatConfig config)
        {
            var plan = new RenderPlan(partialFormat, config);

            foreach (Match match in TokenRegex.Matches(partialFormat))
            {
                var raw = match.Groups[0].Value;

                if (raw.Equals("NUM", StringComparison.OrdinalIgnoreCase) || raw.StartsWith("date", StringComparison.OrdinalIgnoreCase))
                    continue;

                var token = ParseToken(raw);
                plan.Tokens.Add(token);

                if (token.Type == TokenType.ParentAttribute)
                {
                    plan.RootAttributes.Add(token.ParentLookupAttribute);

                    if (!plan.ParentLookups.TryGetValue(token.ParentLookupAttribute, out var attrs))
                    {
                        attrs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        plan.ParentLookups[token.ParentLookupAttribute] = attrs;
                    }

                    attrs.Add(token.AttributeName);
                }
                else
                {
                    plan.RootAttributes.Add(token.AttributeName);
                }
            }

            return plan;
        }

        private static TokenInfo ParseToken(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidPluginExecutionException("Empty token detected in auto number format.");

            if (raw.Contains("."))
            {
                var parts = raw.Split(new[] { '.' }, 2);
                var (attr, fmt) = SplitAttribAndFormat(parts[1]);
                return new TokenInfo(raw, attr.ToLowerInvariant(), fmt, TokenType.ParentAttribute, parts[0].ToLowerInvariant());
            }

            var (rootAttr, rootFmt) = SplitAttribAndFormat(raw);
            return new TokenInfo(raw, rootAttr.ToLowerInvariant(), rootFmt, TokenType.RootAttribute, null);
        }

        private static (string attribute, string format) SplitAttribAndFormat(string part)
        {
            if (string.IsNullOrWhiteSpace(part))
                return (part, null);

            var i = part.IndexOf(':');
            return i < 0 ? (part, null) : (part.Substring(0, i), part.Substring(i + 1));
        }

        private static string ResolveTokenValue(TokenInfo token, Entity entity, IDictionary<string, Entity> parentEntities, FormatConfig config)
        {
            switch (token.Type)
            {
                case TokenType.RootAttribute:
                    return ResolveRootToken(token, entity, config);
                case TokenType.ParentAttribute:
                    return ResolveParentToken(token, parentEntities, config);
                default:
                    throw new InvalidPluginExecutionException($"Unknown token type '{token.Type}' for token '{token.Raw}'.");
            }
        }

        private static string ResolveRootToken(TokenInfo token, Entity entity, FormatConfig config)
        {
            if (!entity.Contains(token.AttributeName) || entity[token.AttributeName] == null)
                throw new InvalidPluginExecutionException($"The format contains '{token.Raw}', but attribute '{token.AttributeName}' is not present or is empty on entity '{entity.LogicalName}' Id='{entity.Id}'.");

            return ConvertValue(entity[token.AttributeName], token.FormatKey, config, token.Raw);
        }

        private static string ResolveParentToken(TokenInfo token, IDictionary<string, Entity> parentEntities, FormatConfig config)
        {
            if (!parentEntities.TryGetValue(token.ParentLookupAttribute, out var parentEntity))
                throw new InvalidPluginExecutionException($"The format contains '{token.Raw}', but lookup '{token.ParentLookupAttribute}' is not present or is empty on the source record.");

            if (!parentEntity.Contains(token.AttributeName) || parentEntity[token.AttributeName] == null)
                throw new InvalidPluginExecutionException($"Parent entity '{parentEntity.LogicalName}' Id='{parentEntity.Id}' does not contain attribute '{token.AttributeName}' for token '{token.Raw}'.");

            return ConvertValue(parentEntity[token.AttributeName], token.FormatKey, config, token.Raw);
        }

        private static string ConvertValue(object value, string formatKey, FormatConfig config, string token)
        {
            if (string.IsNullOrWhiteSpace(formatKey))
                return ConvertToString(value);

            if (value is AliasedValue aliased)
                value = aliased.Value;

            if (!(value is DateTime dateTime))
                throw new InvalidPluginExecutionException($"Token '{token}' uses format key '{formatKey}', but the resolved value is not a DateTime. Actual type: {value?.GetType().FullName ?? "null"}");

            var format = GetDateFormatString(formatKey, config);
            if (string.IsNullOrWhiteSpace(format))
                throw new InvalidPluginExecutionException($"Token '{token}' uses format key '{formatKey}', but the corresponding pl_DateFormat value is empty in the configuration.");

            return dateTime.ToString(format);
        }

        private static string ConvertToString(object value)
        {
            if (value == null)
                return null;
            if (value is AliasedValue aliased)
                return ConvertToString(aliased.Value);
            if (value is EntityReference entityRef)
                return entityRef.Name ?? entityRef.Id.ToString();
            if (value is Money money)
                return money.Value.ToString(CultureInfo.InvariantCulture);
            if (value is OptionSetValue optionSet)
                return optionSet.Value.ToString(CultureInfo.InvariantCulture);
            if (value is DateTime dateTime)
                return dateTime.ToString("o", CultureInfo.InvariantCulture);
            return value.ToString();
        }

        private static string GetDateFormatString(string key, FormatConfig config) => (key ?? string.Empty).ToLowerInvariant() switch
        {
            "date1" => config.DateFormat1,
            "date2" => config.DateFormat2,
            "date3" => config.DateFormat3,
            _ => null
        };
    }
}