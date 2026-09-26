using System.ComponentModel.Composition;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.PackageDeployment.CrmPackageExtentionBase;

namespace Pillaro.Dataverse.PluginFramework.Examples.MarketplacePackage
{
    /// <summary>
    /// Describes the package to Package Deployer and creates the configuration records the
    /// example plugins read at runtime. The managed solutions carry the tables but not their
    /// rows, so without this step a fresh install fails on the first Contact save with
    /// "Key 'ForbiddenWords' value is null or empty" and on the first Task create with
    /// "Primary autonumbering configuration does not exist for entity 'task'".
    /// </summary>
    [Export(typeof(IImportExtensions))]
    public sealed class PackageImportExtension : ImportExtension
    {
        private const string SettingEntity = "pl_setting";
        private const string AutoNumberingEntity = "pl_autonumbering";

        /// <summary>Active <c>statecode</c>, matching what AutoNumberingService looks for.</summary>
        private const int ActiveStateCode = 0;

        public override string GetImportPackageDataFolderName => "PkgAssets";

        public override string GetNameOfImport(bool plural) => "Pillaro Dataverse Plugin Framework";

        public override string GetLongNameOfImport => "Pillaro Dataverse Plugin Framework with Examples";

        public override string GetImportPackageDescriptionText =>
            "Installs the managed Pillaro Dataverse Plugin Framework and its example solution.";

        public override void InitializeCustomExtension()
        {
        }

        public override bool BeforeImportStage() => true;

        /// <summary>
        /// Creates the example configuration once both solutions are in. Every record is
        /// created only when it is missing, so re-running the package, or installing over an
        /// environment where an administrator already made these records by hand, changes
        /// nothing.
        /// </summary>
        public override bool AfterPrimaryImport()
        {
            // The framework defaults to severity 0 when this setting is absent, which logs
            // everything. Writing it makes the value visible and editable in the app.
            EnsureSetting("MinimalSeverityLevel", "pl_int", 0);

            // Read by the Contact name validation example.
            EnsureSetting("ForbiddenWords", "pl_json", "[\"Admin\",\"Test\"]");

            EnsureTaskAutoNumbering();

            return true;
        }

        private void EnsureSetting(string key, string valueAttribute, object value)
        {
            var query = new QueryByAttribute(SettingEntity) { ColumnSet = new ColumnSet(false), TopCount = 1 };
            query.Attributes.Add("pl_key");
            query.Values.Add(key);

            if (CrmSvc.RetrieveMultiple(query).Entities.Count > 0)
            {
                PackageLog.Log($"Runtime setting '{key}' already exists. Left as it is.");
                return;
            }

            var setting = new Entity(SettingEntity);
            setting["pl_key"] = key;
            setting[valueAttribute] = value;

            CrmSvc.Create(setting);
            PackageLog.Log($"Created runtime setting '{key}'.");
        }

        /// <summary>
        /// Creates the primary autonumbering configuration the Task example needs. The
        /// lookup mirrors AutoNumberingService: a primary configuration is the active record
        /// for the entity that has no parent configuration and no parent lookup.
        /// </summary>
        private void EnsureTaskAutoNumbering()
        {
            const string entityName = "task";

            var query = new QueryExpression(AutoNumberingEntity)
            {
                ColumnSet = new ColumnSet(false),
                Criteria = new FilterExpression(),
                TopCount = 1
            };
            query.Criteria.AddCondition("pl_entityname", ConditionOperator.Equal, entityName);
            query.Criteria.AddCondition("pl_parentautonumberingid", ConditionOperator.Null);
            query.Criteria.AddCondition("pl_parentlookupid", ConditionOperator.Null);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, ActiveStateCode);

            if (CrmSvc.RetrieveMultiple(query).Entities.Count > 0)
            {
                PackageLog.Log($"Primary autonumbering configuration for '{entityName}' already exists. Left as it is.");
                return;
            }

            var configuration = new Entity(AutoNumberingEntity);
            configuration["pl_entityname"] = entityName;
            configuration["pl_formatstring"] = "{date1}-{NUM}";
            configuration["pl_dateformat1"] = "yy-MM-dd";
            configuration["pl_digitcount"] = 6;
            configuration["pl_number"] = 1000;

            CrmSvc.Create(configuration);
            PackageLog.Log($"Created the primary autonumbering configuration for '{entityName}'.");
        }
    }
}
