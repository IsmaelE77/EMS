using Aspire.Hosting.Azure;
using Aspire.Hosting.Postgres;

namespace Ems.AppHost;

public static class DatabaseServerServices
{
    /// <summary>
    /// Creates a database server using Azure Postgres Flexible Server with specified configuration.
    /// </summary>
    /// <param name="builder">The distributed application builder used for creating and configuring resources.</param>
    /// <param name="name">The name of the database server to be created.</param>
    /// <param name="username">The resource builder providing the username for PostgreSQL authentication.</param>
    /// <param name="password">The resource builder providing the password for PostgreSQL authentication.</param>
    /// <param name="containerLifetime">The lifetime of the container hosting the database server (default is Session).</param>
    /// <param name="addPgAdmin">Indicates whether to enable the PgAdmin container for managing the database (default is true).</param>
    /// <returns>Returns the resource builder for the configured Azure Postgres Flexible Server resource.</returns>
    public static IResourceBuilder<AzurePostgresFlexibleServerResource> CreateDatabaseServer(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource> username,
        IResourceBuilder<ParameterResource> password,
        ContainerLifetime containerLifetime = ContainerLifetime.Session,
        bool addPgAdmin = true
    )
    {
        var postgresService = builder
            .AddAzurePostgresFlexibleServer(name)
            .WithPasswordAuthentication(username, password)
            .RunAsContainer(x =>
                x.WithImage("postgres:15.15-trixie")
                    .WithDataVolume($"{name.ToLowerInvariant()}-postgres")
                    .WithLifetime(containerLifetime)
                    .WithPossiblePgAdmin(enable: addPgAdmin)
            );
        
        return postgresService;
    }

    
    public static IResourceBuilder<AzurePostgresFlexibleServerResource> CreateDatabaseServer(
        this IDistributedApplicationBuilder builder,
        string name,
        ContainerLifetime containerLifetime = ContainerLifetime.Session,
        bool addPgAdmin = true
    )
    {
        var postgresService = builder
            .AddAzurePostgresFlexibleServer(name)
            .WithPasswordAuthentication()
            .RunAsContainer(x =>
                x.WithImage("postgres:15.15-trixie")
                    .WithDataVolume($"{name.ToLowerInvariant()}-postgres")
                    .WithLifetime(containerLifetime)
                    .WithPossiblePgAdmin(enable: addPgAdmin)
            );
        
        return postgresService;
    }
    
    //Helper method to enable/disable pgAdmin
    private static IResourceBuilder<T> WithPossiblePgAdmin<T>(
        this IResourceBuilder<T> builder,
        Action<IResourceBuilder<PgAdminContainerResource>>? configureContainer = null, 
        string? containerName = null,
        bool enable = true
    ) where T : PostgresServerResource => enable ? builder.WithPgAdmin(configureContainer, containerName) : builder;
}