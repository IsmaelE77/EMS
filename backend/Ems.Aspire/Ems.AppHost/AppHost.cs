using Ems.AppHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aspire-env");

var username = builder.AddParameter("username", secret: true);
var password = builder.AddParameter("password", secret: true);


if (builder.Configuration.GetValue<bool>("ExperimentalMode"))
     SetupDevelopment(builder, username, password);
else
    SetupProduction(builder, username, password);

builder.Build().Run();
return;


static void SetupProduction(
    IDistributedApplicationBuilder builder,
    IResourceBuilder<ParameterResource> username,
    IResourceBuilder<ParameterResource> password)
{
    var postgres = builder
        .CreateDatabaseServer(ServiceNames.DatabaseServer, username, password, ContainerLifetime.Persistent);

    var examManagementDb = postgres
        .AddDatabase("examManagementDb");

    var examManagementMigrator = builder
        .AddProject<Ems_ExamManagement_DbMigrator>("examManagementDbMigrator")
        .WithReference(examManagementDb, "Default")
        .WaitFor(examManagementDb)
        .WithReplicas(1);

    var examManagementHost = builder.AddProject<Ems_ExamManagement_HttpApi_Host>("examManagementHostApi")
        .WithExternalHttpEndpoints()
        .WaitForCompletion(examManagementMigrator)
        .WithHttpHealthCheck()
        .WithReference(examManagementDb, "Default");

    var examExecutionDb = postgres
        .AddDatabase("examExecutionDb");

    var examExecutionMigrator =builder
        .AddProject<Ems_ExamExecution_DbMigrator>("examExecutionDbMigrator")
        .WithReference(examExecutionDb, "Default")
        .WaitFor(examExecutionDb)
        .WithReplicas(1);

    var examExecutionHost = builder.AddProject<Ems_ExamExecution_HttpApi_Host>("examExecutionHostApi")
        .WithExternalHttpEndpoints()
        .WaitForCompletion(examExecutionMigrator)
        .WithHttpHealthCheck()
        .WithReference(examExecutionDb, "Default");
}



static void SetupDevelopment(
    IDistributedApplicationBuilder builder,
    IResourceBuilder<ParameterResource> username,
    IResourceBuilder<ParameterResource> password,
    ContainerLifetime containerLifetime = ContainerLifetime.Session,
    int localCentresCount = 2
)
{
    var examManagementHost = builder
        .CreateExamManagementHostNode(username, password, containerLifetime: containerLifetime);

    for (var i = 0; i < localCentresCount; i++)
    {
        var nodeName = $"ExamCentre{i + 1}";
        builder.AddExamExecutionNode(nodeName, examManagementHost, containerLifetime: containerLifetime);
    }

    //TODO SetUp One PgAdmin and connect to all the servers.
}