using Pillaro.Dataverse.PluginFramework.Testing.Domain.Models;

namespace Pillaro.Dataverse.PluginFramework.Testing.Shared.Extensions;

public static class AsyncProcessExtension
{
    public static bool IsNewestProcessValid(this IList<AsyncProcessResult> results)
    {
        return GetNewest(results).StatusCode != 31;
    }

    public static bool IsNewestProcessInvalid(this IList<AsyncProcessResult> results)
    {
        return GetNewest(results).StatusCode == 31;
    }

    public static bool AreAllProcessesValid(this IList<AsyncProcessResult> results)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));
        if (results.Count == 0)
            throw new InvalidOperationException("No async processes available.");

        return results.All(x => x.StatusCode != 31);
    }

    private static AsyncProcessResult GetNewest(IList<AsyncProcessResult> results)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));
        if (results.Count == 0)
            throw new InvalidOperationException("No async processes available.");

        return results.OrderByDescending(x => x.CreatedOn).First();
    }
}