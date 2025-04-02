namespace Kontur.TestCity.GitLabJobsCrawler;

public class ArtifactsContentsInfo
{
    public required TestReportData? TestReportData { get; set; }
    public required bool HasCodeQualityReport { get; set; }
}
