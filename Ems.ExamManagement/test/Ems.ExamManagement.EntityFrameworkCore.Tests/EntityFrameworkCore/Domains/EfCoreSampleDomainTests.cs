using Ems.ExamManagement.Samples;
using Xunit;

namespace Ems.ExamManagement.EntityFrameworkCore.Domains;

[Collection(ExamManagementTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<ExamManagementEntityFrameworkCoreTestModule>
{

}
