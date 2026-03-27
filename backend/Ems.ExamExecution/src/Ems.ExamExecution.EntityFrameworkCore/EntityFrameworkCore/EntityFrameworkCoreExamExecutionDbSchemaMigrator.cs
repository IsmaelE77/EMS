using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ems.ExamExecution.Data;
using Volo.Abp.DependencyInjection;

namespace Ems.ExamExecution.EntityFrameworkCore;

public class EntityFrameworkCoreExamExecutionDbSchemaMigrator
    : IExamExecutionDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreExamExecutionDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the ExamExecutionDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<ExamExecutionDbContext>()
            .Database
            .MigrateAsync();
    }
}
