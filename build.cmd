src\.nuget\nuget.exe install FAKE -OutputDirectory packages -ExcludeVersion 
src\.nuget\nuget.exe install SourceLink.Fake -OutputDirectory packages -ExcludeVersion
src\.nuget\nuget.exe restore
packages\FAKE\tools\FAKE.exe build.fsx %*