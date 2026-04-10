using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;
using Projects;

namespace Ems.AppHost;

public static class ExamManagementServices
{
    public static IResourceBuilder<ProjectResource> CreateExamManagementHostNode(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<AzurePostgresFlexibleServerResource> postgres
    )
    {
        var node = builder
            .AddResource(new ExamNodeResource("ExamManagement"))
            .WithInitialState(new CustomResourceSnapshot
            {
                State = KnownResourceStates.Running,
                ResourceType = string.Empty,
                Properties = default
            });
        
        var database = postgres
            .AddDatabase(ServiceNames.ExamManagementDatabase)
            .WithParentRelationship(node);

        var migrator = builder
            .AddProject<Ems_ExamManagement_DbMigrator>(ServiceNames.ExamManagementDatabaseMigrator)
            .WithReference(database, "Default")
            .WaitFor(database)
            .WithParentRelationship(node);

        var host = builder
            .AddProject<Ems_ExamManagement_HttpApi_Host>(ServiceNames.ExamManagementServer)
            .WithExternalHttpEndpoints()
            .WaitForCompletion(migrator)
            .WithHttpHealthCheck()
            .WithReference(database, "Default")
            .WithParentRelationship(node);
        
        var frontend = builder
            .AddJavaScriptApp(ServiceNames.ExamManagementFrontend, "./frontend")
            .WithNpm()
            .WithRunScript("start")
            .WithBuildScript("prod")
            // .WithHttpEndpoint(port: 3000, env: "PORT")
            .WithReference(host)
            .WithParentRelationship(node);

        return host;
    }
    
    public static IResourceBuilder<ProjectResource> CreateExamManagementHostNode(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ParameterResource> username,
        IResourceBuilder<ParameterResource> password,
        ContainerLifetime containerLifetime = ContainerLifetime.Session
    )
    {
        var node = builder
            .AddResource(new ExamNodeResource("ExamManagement"))
            .WithInitialState(new CustomResourceSnapshot
            {
                State = KnownResourceStates.Running,
                ResourceType = string.Empty,
                Properties = default
            });

        var postgres = builder
            .CreateDatabaseServer(ServiceNames.DatabaseServer, username, password, containerLifetime: containerLifetime)
            .WithParentRelationship(node);
        
        var database = postgres
            .AddDatabase(ServiceNames.ExamManagementDatabase)
            .WithParentRelationship(node);

        var migrator = builder
            .AddProject<Ems_ExamManagement_DbMigrator>(ServiceNames.ExamManagementDatabaseMigrator)
            .WithReference(database, "Default")
            .WaitFor(database)
            .WithParentRelationship(node);

        var host = builder
            .AddProject<Ems_ExamManagement_HttpApi_Host>(ServiceNames.ExamManagementServer)
            .WithExternalHttpEndpoints()
            .WaitForCompletion(migrator)
            .WithHttpHealthCheck()
            .WithReference(database, "Default")
            .WithParentRelationship(node);

        var frontend = builder
            .AddJavaScriptApp(ServiceNames.ExamManagementFrontend, "./frontend")
            .WithNpm()
            .WithRunScript("start")
            .WithBuildScript("prod")
            // .WithHttpEndpoint(port: 3000, env: "PORT")
            .WithReference(host)
            .WithParentRelationship(node);
        
        return host;
    }
}