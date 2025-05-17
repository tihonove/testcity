using TestCity.Core.JUnit;
using NUnit.Framework;

namespace TestCity.UnitTests.JUnitExtraction;

public class JobInfoTests
{
    [Test]
    public void RemoveDuplicatePartInClassNameTests()
    {
        var data = new List<(string className, string testcaseName, string expected)>
        {
            (
                "Kontur.Forms.Candy.Api.IntegrationTests.ApiControllerTests.GetDigestTests.GetDigestTests",
                "GetDigestTests.GetDigest_FindExactBik",
                "Kontur.Forms.Candy.Api.IntegrationTests.ApiControllerTests.GetDigestTests."),
            (
                "Kontur.Forms.Candy.Api.IntegrationTests.ApiControllerTests.InnerController.ResolvePathTests",
                "ResolvePathTests.ResolveInnerPath_Returns_ResolvedPath(\"/Файл/1/Документ/1/НДФЛ6.2/1/ОбязНА\",\"Файл/Документ/НДФЛ6.2/ОбязНА\",113103,\"fuf.xml\")",
                "Kontur.Forms.Candy.Api.IntegrationTests.ApiControllerTests.InnerController."),
            (
                "Kontur.Forms.Api.FunctionalTests.DraftsApiV2CommonEditorFunctionalTest(Fns,\"warrant.xml\",\"windows-1251\",\"101415.xml\")",
                "DraftsApiV2CommonEditorFunctionalTest(Fns,\"warrant.xml\",\"windows-1251\",\"101415.xml\").DraftWithAutoConvert_SetOldFuf_StartEditor_AllowConvert_EditorNewer",
                "Kontur.Forms.Api.FunctionalTests."),
        };

        foreach (var (className, testcaseName, expected) in data)
        {
            var classNameActual = JUnitReportHelper.RemoveDuplicatePartInClassName(className, testcaseName);
            Assert.That(expected, Is.EqualTo(classNameActual));
        }
    }
}
