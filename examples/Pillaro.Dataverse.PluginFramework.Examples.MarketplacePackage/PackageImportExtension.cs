using System.ComponentModel.Composition;
using Microsoft.Xrm.Tooling.PackageDeployment.CrmPackageExtentionBase;

namespace Pillaro.Dataverse.PluginFramework.Examples.MarketplacePackage
{
    /// <summary>
    /// Describes the package to Package Deployer. The package intentionally runs no custom
    /// deployment code and only imports the managed solutions declared by the project.
    /// </summary>
    [Export(typeof(IImportExtensions))]
    public sealed class PackageImportExtension : ImportExtension
    {
        public override string GetImportPackageDataFolderName => "PkgAssets";

        public override string GetNameOfImport(bool plural) => "Pillaro Dataverse Plugin Framework";

        public override string GetLongNameOfImport => "Pillaro Dataverse Plugin Framework with Examples";

        public override string GetImportPackageDescriptionText =>
            "Installs the managed Pillaro Dataverse Plugin Framework and its example solution.";

        public override void InitializeCustomExtension()
        {
        }

        public override bool BeforeImportStage() => true;

        public override bool AfterPrimaryImport() => true;
    }
}
