using Ems.ExamManagement.Samples;
using Xunit;

namespace Ems.ExamManagement.EntityFrameworkCore.Applications;

[Collection(ExamManagementTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<ExamManagementEntityFrameworkCoreTestModule>
{

}
