using Xunit;

namespace Ems.ExamManagement.EntityFrameworkCore;

[CollectionDefinition(ExamManagementTestConsts.CollectionDefinitionName)]
public class ExamManagementEntityFrameworkCoreCollection : ICollectionFixture<ExamManagementEntityFrameworkCoreFixture>
{

}
