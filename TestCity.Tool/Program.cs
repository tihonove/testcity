using System.Text;
using dotenv.net;
using Kontur.TestCity.Core;
using Kontur.TestCity.Core.GitLab;

DotEnv.Fluent().WithProbeForEnv(10).Load();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var clientProvider = new SkbKonturGitLabClientProvider(GitLabSettings.Default);
var clientEx = clientProvider.GetExtendedClient();
var projects = GitLabProjectsService.GetAllProjects();
foreach(var project in projects)
{
    var jobs = await clientEx.GetAllProjectJobsAsync(long.Parse(project.Id)).Take(10).ToListAsync();
    foreach (var job in jobs)
    {
        Console.WriteLine($"Job ID: {job.Id}, Name: {job.Name}, Status: {job.Status}");
    }
}
