using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aspire-env");

var username = builder.AddParameter("username", secret: true);
var password = builder.AddParameter("password", secret: true);

var postgres = builder
    .AddAzurePostgresFlexibleServer("ems-postgres")
    .WithPasswordAuthentication(username, password)
    .RunAsContainer(x =>
        x.WithImage("postgres:15.15-trixie")
            .WithDataVolume("ems-postgres")
            .WithPgAdmin()
    );

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


builder.Build().Run();