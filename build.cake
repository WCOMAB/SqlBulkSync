/*
Cake build script template
*/

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target          = Argument<string>("target", "Default");
var configuration   = Argument<string>("configuration", "Release");
var solutions       = System.IO.Directory
                            .EnumerateFiles("src", "*.sln", SearchOption.AllDirectories)
                            .ToArray();
var solutionDirs    = solutions
                        .Select(System.IO.Path.GetDirectoryName)
                        .ToArray();

//////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
//////////////////////////////////////////////////////////////////////

Setup(() =>
{
    // Executed BEFORE the first task.
    Information("Running tasks...");
});

Teardown(() =>
{
    // Executed AFTER the last task.
    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
// TASK DEFINITIONS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    // Clean directories.
    Array.ForEach(
        solutionDirs,
        solutionDir=>{
                        Information("Cleaning {0}", solutionDir);
                        CleanDirectories(solutionDir + "/**/bin/" + configuration);
                        CleanDirectories(solutionDir + "/**/obj/" + configuration);
                     }
    );
});

Task("Restore")
    .Does(() =>
{
    // Build all solutions.
    Array.ForEach(
        solutions,
        solution=>{
                        Information("Restoring {0}", solution);
                        NuGetRestore(solution);
                  }
    );
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
    // Build all solutions.
    Array.ForEach(
        solutions,
        solution=>{
                        Information("Building {0}", solution);
                        MSBuild(solution, settings => 
                            settings.SetPlatformTarget(PlatformTarget.MSIL)
                                .WithProperty("TreatWarningsAsErrors","true")
                                .WithTarget("Build")
                                .SetConfiguration(configuration));
                  }
    );
});

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);