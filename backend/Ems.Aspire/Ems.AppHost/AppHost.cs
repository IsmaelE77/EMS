using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("examManagementPostgres")
    .WithImage("postgres:15.15-trixie")
    .WithDataVolume("ems-postgres");

var examManagementDb = postgres
    .AddDatabase("examManagementDb");

var examManagementMigrator = builder
    .AddProject<Ems_ExamManagement_DbMigrator>("examManagementDbMigrator")
    .WithReference(examManagementDb, "Default")
    .WaitFor(examManagementDb)
    .WithReplicas(1);

var examManagementHost = builder.AddProject<Ems_ExamExecution_HttpApi_Host>("examManagementHostApi")
    .WaitForCompletion(examManagementMigrator)
    .WithHttpHealthCheck()
    .WithReference(examManagementDb, "Default");

var examExecutionDb = postgres
    .AddDatabase("ExamExecution");

var examExecutionMigrator =builder
    .AddProject<Ems_ExamExecution_DbMigrator>("examExecutionDbMigrator")
    .WithReference(examExecutionDb, "Default")
    .WaitFor(examExecutionDb)
    .WithReplicas(1);

var examExecutionHost = builder.AddProject<Ems_ExamExecution_HttpApi_Host>("examExecutionHostApi")
    .WaitForCompletion(examExecutionMigrator)
    .WithHttpHealthCheck()
    .WithReference(examExecutionDb, "Default");


builder.Build().Run();