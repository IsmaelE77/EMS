using Aspire.Hosting.Azure;
using Projects;

namespace Ems.AppHost;

public static class ExamExecutionServices
{
    /// <summary>
    /// Adds an Exam Execution Node to the distributed application builder.
    /// </summary>
    /// <param name="builder">The distributed application builder used to configure and add resources to the application.</param>
    /// <param name="nodeName">The unique name of the Exam Execution Node to be added.</param>
    /// <param name="postgres">The PostgreSQL flexible server resource builder to configure the database for this node.</param>
    /// <param name="centralApi">The central API project resource builder required as a reference for this node.</param>
    /// <returns>A resource builder for the created Exam Execution Node.</returns>
    public static IResourceBuilder<ExamNodeResource> AddExamExecutionNode(
        this IDistributedApplicationBuilder builder,
        string nodeName,
        IResourceBuilder<AzurePostgresFlexibleServerResource> postgres,
        IResourceBuilder<ProjectResource> centralApi)
    {
        var node = builder
            .AddResource(new ExamNodeResource(nodeName))
            .WithInitialState(new CustomResourceSnapshot
            {
                State = KnownResourceStates.Running,
                ResourceType = string.Empty,
                Properties = default
            });

        var db = postgres
            .AddDatabase($"{nodeName}-Database")
            .WithParentRelationship(node);

        var migrator = builder
            .AddProject<Ems_ExamExecution_DbMigrator>($"{nodeName}-Migrator")
            .WithReference(db, "Default")
            .WaitFor(db)
            .WithParentRelationship(node);

        var host = builder
            .AddProject<Ems_ExamExecution_HttpApi_Host>($"{nodeName}-Api")
            .WithExternalHttpEndpoints()
            .WaitForCompletion(migrator)
            .WithHttpHealthCheck()
            .WithReference(db, "Default")
            .WithReference(centralApi)
            .WithParentRelationship(node);

        var frontend = builder
            .AddJavaScriptApp($"{nodeName}-Frontend", "./frontend")
            .WithNpm()
            .WithRunScript("start")
            .WithBuildScript("prod")
            // .WithHttpEndpoint(port: 3000, env: "PORT")
            .WithReference(host)
            .WithParentRelationship(node);

        return node;
    }

    /// <summary>
    /// Adds an Exam Execution Node to the distributed application builder, including all required resources and configurations.
    /// </summary>
    /// <param name="builder">The distributed application builder used to configure and add resources to the application.</param>
    /// <param name="nodeName">The unique name of the Exam Execution Node to be added.</param>
    /// <param name="centralApi">The central API project resource builder required as a reference for this node.</param>
    /// <param name="containerLifetime">The lifetime of the container hosting the Exam Execution Node (default is Session).</param>
    /// <returns>A resource builder for the created Exam Execution Node.</returns>
    public static IResourceBuilder<ExamNodeResource> AddExamExecutionNode(
        this IDistributedApplicationBuilder builder,
        string nodeName,
        IResourceBuilder<ProjectResource> centralApi,
        ContainerLifetime containerLifetime = ContainerLifetime.Session
    )
    {
        var node = builder
            .AddResource(new ExamNodeResource(nodeName))
            .WithInitialState(new CustomResourceSnapshot
            {
                State = KnownResourceStates.Running,
                ResourceType = string.Empty,
                Properties = default
            });
        
        // Dedicated Postgres server for this node
        var postgres = builder
            .CreateDatabaseServer($"{nodeName}-DatabaseServer", containerLifetime: containerLifetime, addPgAdmin: false)
            .WithParentRelationship(node);

        // Dedicated database on that dedicated server
        var db = postgres
            .AddDatabase($"{nodeName}-Database")
            .WithParentRelationship(node);

        var migrator = builder
            .AddProject<Ems_ExamExecution_DbMigrator>($"{nodeName}-Migrator")
            .WithReference(db, "Default")
            .WaitFor(db)
            .WithParentRelationship(node);

        var host = builder
            .AddProject<Ems_ExamExecution_HttpApi_Host>($"{nodeName}-Api")
            .WithExternalHttpEndpoints()
            .WaitForCompletion(migrator)
            .WithHttpHealthCheck()
            .WithReference(db, "Default")
            .WithReference(centralApi)
            .WithParentRelationship(node);

        // For more examples look at https://aspire.dev/whats-new/aspire-13/#javascript-as-a-first-class-citizen
        var frontend = builder
            .AddJavaScriptApp($"{nodeName}-Frontend", "./frontend")
            .WithNpm()
            .WithRunScript("start")
            .WithBuildScript("prod")
            // .WithHttpEndpoint(port: 3000, env: "PORT")
            .WithReference(host)
            .WithParentRelationship(node);

        return node;
    }
}