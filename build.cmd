src\.nuget\nuget.exe install FAKE -OutputDirectory packages -ExcludeVersion
src\.nuget\nuget.exe install SourceLink.Fake -OutputDirectory packages -ExcludeVersion
src\.nuget\nuget.exe install NUnit.Runners -OutputDirectory packages
src\.nuget\nuget.exe restore src\Burrows.sln
packages\FAKE\tools\FAKE.exe build.fsx %*