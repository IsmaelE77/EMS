namespace Microsoft.Extensions.Hosting;

public static class ServiceNames
{
    public const string DatabaseServer = "DatabaseServer";
    
    public const string ExamManagementDatabase = "ExamManagementDatabase";
    public const string ExamManagementDatabaseMigrator = "ExamManagementMigrator";
    public const string ExamManagementServer = "ExamManagementServer";
    public const string ExamManagementFrontend = "ExamManagementFrontend";
    
    //
    // public const string ExamExecutionDatabase = "ExamExecutionDatabase";
    // public const string ExamExecutionDatabaseMigrator = "ExamExecutionMigrator";
    // public const string ExamExecutionServer = "ExamExecutionServer";
    // public const string ExamExecutionFrontend = "ExamExecutionFrontend";
    
    public const string Cache = "Cache";
}