using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact
{
    public class UpdateAddressLabel : TaskBase<Logic.Contact>
    {
        private static readonly string[] AddressAttributes =
        {
            nameof(Logic.Contact.Address1_Line1),
            nameof(Logic.Contact.Address1_Line2),
            nameof(Logic.Contact.Address1_Line3),
            nameof(Logic.Contact.Address1_City),
            nameof(Logic.Contact.Address1_PostalCode),
            nameof(Logic.Contact.Address1_StateOrProvince),
            nameof(Logic.Contact.Address1_Country)
        };

        public UpdateAddressLabel(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Preoperation)
                .WithMessages(new[] { "Create", "Update" })
                .ForEntity(ContextEntity.LogicalName)
                .HasPreImageWhen(ctx => ctx.Message.Equals("Update", StringComparison.InvariantCultureIgnoreCase))
                .EntityWithAtLeastOneAttribute(ContextEntity, AddressAttributes);
        }

        protected override void DoExecute()
        {
            var line1 = Normalize(GetValue(nameof(Logic.Contact.Address1_Line1), x => x.Address1_Line1));
            var line2 = Normalize(GetValue(nameof(Logic.Contact.Address1_Line2), x => x.Address1_Line2));
            var line3 = Normalize(GetValue(nameof(Logic.Contact.Address1_Line3), x => x.Address1_Line3));
            var city = Normalize(GetValue(nameof(Logic.Contact.Address1_City), x => x.Address1_City));
            var postalCode = Normalize(GetValue(nameof(Logic.Contact.Address1_PostalCode), x => x.Address1_PostalCode));
            var state = Normalize(GetValue(nameof(Logic.Contact.Address1_StateOrProvince), x => x.Address1_StateOrProvince));
            var country = Normalize(GetValue(nameof(Logic.Contact.Address1_Country), x => x.Address1_Country));

            var addressLabel = BuildAddressLabel(line1, line2, line3, city, postalCode, state, country);

            if (string.Equals(ContextEntity.Address1_Name, addressLabel, StringComparison.InvariantCulture))
                return;

            AddLogMessageLine($"Updating {nameof(ContextEntity.Address1_Name)} to '{addressLabel}'.");

            ContextEntity.Address1_Name = addressLabel;
        }

        private string GetValue(string attributeName, Func<Logic.Contact, string> selector)
        {
            if (ContextEntity.Attributes.ContainsKey(attributeName.ToLowerInvariant())) 
                return selector(ContextEntity);

            var preImage = ContextEntity;
            if(TaskContext.Message.Equals("Update"  , StringComparison.InvariantCultureIgnoreCase))
                preImage = GetPreImage();

            return preImage == null ? null : selector(preImage);
        }

        private static string BuildAddressLabel(string line1, string line2, string line3, string city, string postalCode, string state, string country)
        {
            var result = line1;

            if (!string.IsNullOrWhiteSpace(line2))
                result = string.IsNullOrWhiteSpace(result) ? line2 : $"{result}, {line2}";

            if (!string.IsNullOrWhiteSpace(line3))
                result = string.IsNullOrWhiteSpace(result) ? line3 : $"{result}, {line3}";

            if (!string.IsNullOrWhiteSpace(city))
                result = string.IsNullOrWhiteSpace(result) ? city : $"{result}, {city}";

            if (!string.IsNullOrWhiteSpace(state))
                result = string.IsNullOrWhiteSpace(result) ? state : $"{result}, {state}";

            if (!string.IsNullOrWhiteSpace(postalCode))
                result = string.IsNullOrWhiteSpace(result) ? postalCode : $"{result} {postalCode}";

            if (!string.IsNullOrWhiteSpace(country))
                result = string.IsNullOrWhiteSpace(result) ? country : $"{result}, {country}";

            return string.IsNullOrWhiteSpace(result) ? "Primary Address" : result;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}