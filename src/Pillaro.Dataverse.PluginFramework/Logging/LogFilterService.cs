using Pillaro.Dataverse.PluginFramework.Logging.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pillaro.Dataverse.PluginFramework.Logging;

public class LogFilterService
{
    public List<Log> GetFilteredLogs(IEnumerable<LogFilterModel> filters, IList<Log> allLogs)
    {
        if (allLogs == null || !allLogs.Any())
            return [];

        if (filters == null || !filters.Any())
            return [];

        List<Log> filteredLogs = [];

        foreach (var filter in filters)
        {
            if (filter.Entity == null && (filter.TaskNames == null || !filter.TaskNames.Any()))
                throw new InvalidOperationException($"Log filter must define at least one of these fields: {nameof(filter.Entity)}, {nameof(filter.TaskNames)}.");

            IEnumerable<Log> logAdd = allLogs.Select(item => (Log)item.Clone()).ToList();

            if (filter.Entity != null)
                logAdd = logAdd.Where(x => string.Equals(x.Entity, filter.Entity, StringComparison.OrdinalIgnoreCase));

            if (filter.TaskNames != null && filter.TaskNames.Any())
                logAdd = logAdd.Where(x => x.TaskName != null && filter.TaskNames.Contains(x.TaskName));

            if (filter.MinimalSeverity != null)
                logAdd = logAdd.Where(x => (int)x.LogSeverity >= (int)filter.MinimalSeverity);

            if (filter.UserIds?.Any() ?? false)
                logAdd = logAdd.Where(x => x.User != null && filter.UserIds.Contains(x.User.Id));

            if (filter.InitiatingUserIds?.Any() ?? false)
                logAdd = logAdd.Where(x => x.InitiatingUser != null && filter.InitiatingUserIds.Contains(x.InitiatingUser.Id));

            filteredLogs = filteredLogs.Union(logAdd, new LogEqualityComparer()).ToList();
        }

        return filteredLogs;
    }

}