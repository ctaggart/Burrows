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

let versionAssembly = cfg.AppSettings.["versionAssembly"].Value // change when incompatible
let versionFile = cfg.AppSettings.["versionFile"].Value // matches nuget version
let prerelease =
    if hasBuildParam "prerelease" then getBuildParam "prerelease"
    else sprintf "ci%s" (dt.ToString "yyMMddHHmm") // 20 char limit
let buildVersion = if String.IsNullOrEmpty prerelease then versionFile else sprintf "%s-%s" versionFile prerelease
let versionInfo = sprintf """{"buildVersion":"%s","buildDate":"%s","gitCommit":"%s"}""" buildVersion dt.IsoDateTime repo.Revision // json

Target "Clean" (fun _ -> 
    !! "**/bin/"
    ++ "**/obj/" 
    |> CleanDirs 
)

Target "BuildVersion" (fun _ ->
    let args = sprintf "UpdateBuild -Version \"%s\"" buildVersion
    Shell.Exec("appveyor", args) |> ignore
)

Target "AssemblyInfo" (fun _ ->
    // C:\Projects\Burrows\src\Burrows\Properties\AssemblyInfo.cs
//    [assembly: AssemblyDescription("Burrows is a distributed application framework for .NET forked from MassTransit.")]

    // C:\Projects\Burrows\src\Containers\Burrows.Autofac\Properties\AssemblyInfo.cs
    // [assembly: AssemblyDescription("Autofac integration for burrows. Burrows is a distributed application framework for .NET forked from MassTransit.")]

    // C:\Projects\Burrows\src\Loggers\Burrows.NLog\Properties\AssemblyInfo.cs
    // [assembly: AssemblyDescription("NLog integration for burrows. Burrows is a distributed application framework for .NET forked from MassTransit.")]

//    C:\Projects\Burrows\src\Loggers\Burrows.Log4Net\Properties\AssemblyInfo.cs
//    [assembly: AssemblyDescription("Log4Net integration for burrows. Burrows is a distributed application framework for .NET forked from MassTransit.")]

//    C:\Projects\Burrows\src\Persistence\Burrows.NHib\Properties\AssemblyInfo.cs
//    [assembly: AssemblyDescription("NHibernate persistence integration for burrows. Burrows is a distributed application framework forked from MassTransit.")]

    CreateFSharpAssemblyInfo "ProtoParser/AssemblyInfo.fs"
        [ 
        Attribute.Version versionAssembly 
        Attribute.FileVersion versionFile
        Attribute.InformationalVersion (versionInfo.Replace("\"","\\\"")) // escape quotes
        ]
    CreateFSharpAssemblyInfo "Roslyn/AssemblyInfo.fs"
        [ 
        Attribute.Version versionAssembly 
        Attribute.FileVersion versionFile
        Attribute.InformationalVersion (versionInfo.Replace("\"","\\\"")) // escape quotes
        ]
)

Target "Build" (fun _ ->
    !! "Froto.sln" |> MSBuildRelease "" "Rebuild" |> ignore
)

Target "SourceLink" (fun _ ->
    !! "ProtoParser/Froto.Parser.fsproj" 
    ++ "Roslyn/Froto.Roslyn.fsproj"
    |> Seq.iter (fun f ->
        let proj = VsProj.LoadRelease f
        logfn "source linking %s" proj.OutputFilePdb
        let files = proj.Compiles -- "**/AssemblyInfo.fs"
        repo.VerifyChecksums files
        proj.VerifyPdbChecksums files
        proj.CreateSrcSrv "https://raw.github.com/ctaggart/froto/{0}/%var2%" repo.Revision (repo.Paths files)
        Pdbstr.exec proj.OutputFilePdb proj.OutputFilePdbSrcSrv
    )
)

Target "NuGet" (fun _ ->
    let bin = "bin"
    Directory.CreateDirectory bin |> ignore

    NuGet (fun p -> 
    { p with
        Version = buildVersion
        WorkingDir = "ProtoParser/bin/Release"
        OutputPath = bin
        DependenciesByFramework =
        [{ 
            FrameworkVersion = "net45"
            Dependencies = 
                [
                "FParsec", GetPackageVersion "./packages/" "FParsec"
                ] 
        }]
    }) "ProtoParser/Froto.Parser.nuspec"

    NuGet (fun p -> 
    { p with
        Version = buildVersion
        WorkingDir = "Roslyn/bin/Release"
        OutputPath = bin
        DependenciesByFramework =
        [{ 
            FrameworkVersion = "net45"
            Dependencies = 
                [
                "Froto.Parser", sprintf "[%s]" buildVersion // exact version
                "Microsoft.CodeAnalysis.CSharp.Workspaces", GetPackageVersion "./packages/" "Microsoft.CodeAnalysis.CSharp.Workspaces"
                ] 
        }]
    }) "Roslyn/Froto.Roslyn.nuspec"
)

"Clean"
    =?> ("BuildVersion", buildServer = BuildServer.AppVeyor)
    ==> "AssemblyInfo"
    ==> "Build"
    =?> ("SourceLink", buildServer = BuildServer.AppVeyor || hasBuildParam "sl")
    ==> "NuGet"

RunTargetOrDefault "NuGet"