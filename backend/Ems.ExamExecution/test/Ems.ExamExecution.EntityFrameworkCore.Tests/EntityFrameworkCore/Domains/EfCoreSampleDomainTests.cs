using Ems.ExamExecution.Samples;
using Xunit;

namespace Ems.ExamExecution.EntityFrameworkCore.Domains;

[Collection(ExamExecutionTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<ExamExecutionEntityFrameworkCoreTestModule>
{

}
