#I "packages/FAKE/tools"
#r "FakeLib.dll"
#load "packages/SourceLink.Fake/tools/SourceLink.fsx"

open System
open System.IO
open Fake
open Fake.AssemblyInfoFile
open SourceLink

let dt = DateTime.UtcNow
let cfg = getBuildConfig __SOURCE_DIRECTORY__
let repo = new GitRepo(__SOURCE_DIRECTORY__)

let versionAssembly = getBuildParamOrDefault "versionAssembly" cfg.AppSettings.["versionAssembly"].Value
let versionFile = getBuildParamOrDefault "versionFile" cfg.AppSettings.["versionFile"].Value
let prerelease = getBuildParamOrDefault "prerelease" (sprintf "ci%s" (dt.ToString "yyMMddHHmm")) // 20 char limit
let rawUrl = getBuildParamOrDefault "rawUrl" "https://raw.githubusercontent.com/ctaggart/burrows/{0}/%var2%"

let buildVersion = if String.IsNullOrEmpty prerelease then versionFile else sprintf "%s-%s" versionFile prerelease
let versionInfo =
    let vi = sprintf """{"buildVersion":"%s","buildDate":"%s","gitCommit":"%s"}""" buildVersion dt.IsoDateTime repo.Revision // json
    vi.Replace("\"","\\\"") // escape quotes

Target "Clean" (fun _ -> !! "**/bin/" ++ "**/obj/" |> CleanDirs)

Target "BuildVersion" (fun _ ->
    let args = sprintf "UpdateBuild -Version \"%s\"" buildVersion
    Shell.Exec("appveyor", args) |> ignore
)

Target "AssemblyInfo" (fun _ ->
    let common = [
        Attribute.Product "Burrows"
        Attribute.Copyright "Copyright 2014 Eric Swann, et. al. - All rights reserved."
        Attribute.Version versionAssembly
        Attribute.FileVersion versionFile
        Attribute.InformationalVersion versionInfo
        Attribute.ComVisible false
        Attribute.CLSCompliant false ]

    [   Attribute.Description "Burrows is a distributed application framework for .NET forked from MassTransit."
        Attribute.Guid "1bc31ebf-ea35-4389-8bae-1ced4544e3f6"
    ] @ common |> CreateCSharpAssemblyInfo @"src\Burrows\Properties\AssemblyInfo.cs"

    [   Attribute.Description "Autofac integration for burrows. Burrows is a distributed application framework for .NET forked from MassTransit."
    ] @ common |> CreateCSharpAssemblyInfo @"src\Containers\Burrows.Autofac\Properties\AssemblyInfo.cs"

    [   Attribute.Description "NLog integration for burrows. Burrows is a distributed application framework for .NET forked from MassTransit."
    ] @ common |> CreateCSharpAssemblyInfo @"src\Loggers\Burrows.NLog\Properties\AssemblyInfo.cs"

    [   Attribute.Description "Log4Net integration for burrows. Burrows is a distributed application framework for .NET forked from MassTransit."
    ] @ common |> CreateCSharpAssemblyInfo @"src\Loggers\Burrows.Log4Net\Properties\AssemblyInfo.cs"

    [   Attribute.Description "NHibernate persistence integration for burrows. Burrows is a distributed application framework forked from MassTransit."
    ] @ common |> CreateCSharpAssemblyInfo @"src\Persistence\Burrows.NHib\Properties\AssemblyInfo.cs"
)

Target "Build" (fun _ ->
    !! @"src\Burrows.sln" |> MSBuildRelease "" "Rebuild" |> ignore
)

// NUnit-Console Command Line Options
// http://www.nunit.org/index.php?p=consoleCommandLine&r=2.6.3
Target "Test" (fun _ ->
    let dlls =
        !! @"tests\Burrows.Tests.dll"
        ++ @"tests\Burrows.Tests.Framework.dll"
//        ++ @"tests\Burrows.Tests.RabbitMq.dll" // requires 127.0.0.1:5672
        ++ @"tests\Burrows.Containers.Tests.dll"
    dlls |> Seq.iter (logfn "run unit tests: %s")
    dlls |> NUnit (fun p -> { p with ErrorLevel = Error; Framework="net-4.5" })
)

Target "SourceLink" (fun _ ->
    !! @"src\Burrows\Burrows.csproj"
    ++ @"src\Containers\Burrows.Autofac\Burrows.Autofac.csproj"
    ++ @"src\Loggers\Burrows.NLog\Burrows.NLog.csproj"
    ++ @"src\Loggers\Burrows.Log4Net\Burrows.Log4Net.csproj"
    ++ @"src\Persistence\Burrows.NHib\Burrows.NHib.csproj"
    |> Seq.iter (fun f ->
        let proj = VsProj.LoadRelease f
        logfn "source linking %s" proj.OutputFilePdb
        let files = proj.Compiles -- "**/AssemblyInfo.cs"
        repo.VerifyChecksums files
        proj.VerifyPdbChecksums files
        proj.CreateSrcSrv rawUrl repo.Revision (repo.Paths files)
        Pdbstr.exec proj.OutputFilePdb proj.OutputFilePdbSrcSrv
    )
)

Target "NuGet" (fun _ ->
    CreateDir "bin"
    let common (p:NuGetParams) =
        { p with
            Version = buildVersion
            OutputPath = "bin"
        }

    NuGet (fun p ->
        { p with
            WorkingDir = @"src\Burrows\bin\Release"
            Dependencies =
                [   "RabbitMQ.Client", GetPackageVersion "src/packages" "RabbitMQ.Client"
                    "Stact", GetPackageVersion "src/packages" "Stact"
                    "Magnum", GetPackageVersion "src/packages" "Magnum"
                    "Newtonsoft.Json", GetPackageVersion "src/packages" "Newtonsoft.Json" ]
        } |> common) @"src\Burrows\Burrows.nuspec"

    NuGet (fun p ->
        { p with
            WorkingDir = @"src\Containers\Burrows.Autofac\bin\Release"
            Dependencies =
                [   "Burrows", buildVersion
                    "Autofac", GetPackageVersion "src/packages" "Autofac"]
        } |> common) @"src\Containers\Burrows.Autofac\Burrows.Autofac.nuspec"

    NuGet (fun p ->
        { p with
            WorkingDir = @"src\Loggers\Burrows.NLog\bin\Release"
            Dependencies =
                [   "Burrows", buildVersion
                    "NLog", GetPackageVersion "src/packages" "NLog"]
        } |> common) @"src\Loggers\Burrows.NLog\Burrows.NLog.nuspec"

    NuGet (fun p ->
        { p with
            WorkingDir = @"src\Loggers\Burrows.Log4Net\bin\Release"
            Dependencies =
                [   "Burrows", buildVersion
                    "log4net", GetPackageVersion "src/packages" "log4net"]
        } |> common) @"src\Loggers\Burrows.Log4Net\Burrows.Log4Net.nuspec"

    NuGet (fun p ->
        { p with
            WorkingDir = @"src\Persistence\Burrows.NHib\bin\Release"
            Dependencies =
                [   "Burrows", buildVersion
                    "NHibernate", GetPackageVersion "src/packages" "NHibernate"]
        } |> common) @"src\Persistence\Burrows.NHib\Burrows.NHib.nuspec"
)

Target "Start" DoNothing

"Start"
    =?> ("BuildVersion", buildServer = BuildServer.AppVeyor)
    ==> "AssemblyInfo"
    ==> "Build"
    ==> "Test"
    =?> ("SourceLink", buildServer = BuildServer.AppVeyor || hasBuildParam "link")
    ==> "NuGet"

RunTargetOrDefault "NuGet"