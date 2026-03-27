using Ems.ExamExecution.Samples;
using Xunit;

namespace Ems.ExamExecution.EntityFrameworkCore.Applications;

[Collection(ExamExecutionTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<ExamExecutionEntityFrameworkCoreTestModule>
{

}
