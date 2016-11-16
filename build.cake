#tool "nuget:?package=xunit.runner.console&version=2.1.0"
#tool "nuget:?package=gitreleasemanager&version=0.4.0"

var projectName = "CakeLauncher";
var user = EnvironmentVariable("ghu");
var pass = EnvironmentVariable("ghp");
var version = ParseAssemblyInfo($"./{projectName}/Properties/AssemblyInfo.cs").AssemblyVersion;
var solution = $"{projectName}.sln";

Action  clearZip = () => {
    DeleteFiles($"{projectName}.Installer/*.msi");
};

Func<string> getZip = () => {
    return new System.IO.DirectoryInfo($"{projectName}").GetFiles("*.msi").LastOrDefault().FullName;
};

Task("Build-Release")
    .Does(() => {
        clearZip();
        var settings = new MSBuildSettings {
            ToolVersion = MSBuildToolVersion.VS2015,
            Configuration = "Release"
        }.WithTarget("Rebuild");
        MSBuild(solution, settings);
    });

Task("Create-Github-Release")
    .IsDependentOn("Build-Release")
    .Does(() => {
        var zip = getZip();
        var tag = $"v{version}";
        var args = $"tag -a {tag} -m \"{projectName} {tag}\"";
        var owner = "wk-j";
        var repo = "cake-launcher";

        StartProcess("git", new ProcessSettings {
            Arguments = args
        });

        StartProcess("git", new ProcessSettings {
            Arguments = $"push https://{user}:{pass}@github.com/{owner}/{repo}.git {tag}"
        });

        GitReleaseManagerCreate(user, pass, owner , repo, new GitReleaseManagerCreateSettings {
            Name              = tag,
            InputFilePath = "RELEASE.md",
            Prerelease        = false,
            TargetCommitish   = "master",
        });
        GitReleaseManagerAddAssets(user, pass, owner, repo, tag, zip);
        GitReleaseManagerPublish(user, pass, owner , repo, tag);
    });

Task("Build").Does(() => {
    DotNetBuild(solution);
});

var target = Argument("target", "Default");
RunTarget(target);