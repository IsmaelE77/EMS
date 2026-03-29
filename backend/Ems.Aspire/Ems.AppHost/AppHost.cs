using Ems.AppHost;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aspire-env");

var username = builder.AddParameter("username", secret: true);
var password = builder.AddParameter("password", secret: true);


// You can switch to the desired setup.

//SetupDevelopment(builder, username, password, ContainerLifetime.Session, localCentresCount: 1);
SetupProduction(builder, username, password, ContainerLifetime.Session, localCentresCount: 1);


// if (builder.Environment.IsDevelopment())
// {
//     SetupDevelopment(builder, username, password);
// }
// else
// {
//     SetupProduction(builder, username, password);
// }


builder.Build().Run();
return;



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

static void SetupProduction(
    IDistributedApplicationBuilder builder,
    IResourceBuilder<ParameterResource> username,
    IResourceBuilder<ParameterResource> password,
    ContainerLifetime containerLifetime = ContainerLifetime.Session,
    int localCentresCount = 2
)
{
    // Create a single shared Postgres server for development,
    // instead of one per node as in production (Real world scenario),
    // to save resources and simplify publication.
    var postgres = builder
        .CreateDatabaseServer(ServiceNames.DatabaseServer, username, password, containerLifetime);

    var examManagementHost = builder.CreateExamManagementHostNode(postgres);

    for (var i = 0; i < localCentresCount; i++)
    {
        builder.AddExamExecutionNode($"ExamCentre{i + 1}", postgres, examManagementHost);
    }
}


