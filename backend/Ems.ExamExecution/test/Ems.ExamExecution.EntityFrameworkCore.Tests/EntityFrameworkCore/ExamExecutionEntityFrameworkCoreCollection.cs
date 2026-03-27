using Xunit;

namespace Ems.ExamExecution.EntityFrameworkCore;

[CollectionDefinition(ExamExecutionTestConsts.CollectionDefinitionName)]
public class ExamExecutionEntityFrameworkCoreCollection : ICollectionFixture<ExamExecutionEntityFrameworkCoreFixture>
{

}
