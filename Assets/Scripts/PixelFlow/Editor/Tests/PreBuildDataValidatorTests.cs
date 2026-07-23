using NUnit.Framework;
using PixelFlow.Editor;

namespace PixelFlow.Editor.Tests
{
    [TestFixture]
    public class PreBuildDataValidatorTests
    {
        [Test]
        public void ValidateAllData_RunsWithoutCrashing()
        {
            bool isValid = PreBuildDataValidator.ValidateAllData(out string errorMessage);
            
            // Should either be valid or return a non-null error message explaining what config is missing
            if (!isValid)
            {
                Assert.IsNotEmpty(errorMessage);
            }
        }
    }
}
