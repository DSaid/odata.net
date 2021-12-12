1. Install Visual Studio 2019 Enterprise 16.11 configuring the installer with the `.vsconfig` file [here](./OData.Tests.Performance.vsconfig) (other versions and SKUs of Visual Studio 2019 should also work, but these instructions were tested with the version specified here). Continue through any prompts about components that are out-of-support. You can find more information about using `.vsconfig` files [here](https://docs.microsoft.com/en-us/visualstudio/install/import-export-installation-configurations?view=vs-2022)

5. Create a local directory for the repository, then run `git clone https://github.com/OData/odata.net.git` from the command prompt in that directory
TODO checkout corranrogue9/onboarding2
7. Open [odata.net/sln/OData.Tests.Performance.sln](OData.Tests.Performance.sln) using the previously installed Visual Studio

12. In Visual Studio, right click `Solution 'OData.Tests.Performance' -> Build Solution`

For each benchmark project, open the binary output folder in a command prompt and run its `exe` with the arguments `--filter *`. Ensure that the output doesn't not contain any failures or exit code errors. As an example, navigate to `C:\github\odata.net\test\PerformanceTests\ComponentTests\bin\Release\netcoreapp3.1` and run `Microsoft.OData.Performance.ComponentTests.exe --filter * > tests.txt 2>&1` and ensure that `tests.txt` does not contain any failures. NOTE: currently, there are no running benchmarks in `Microsoft.OData.Performance.ServiceTests`; this issue is currently being tracked [here](TODO).




2. Install [.NET Core SDK 1.1.14](https://dotnet.microsoft.com/download/dotnet/1.1)
3. Install [.NET Core SDK 2.1.818](https://dotnet.microsoft.com/download/dotnet/2.1)
4. Despite being documented as included in .NET Core SDK 2.1.818, install [.NET Core Runtime 2.1.30](https://dotnet.microsoft.com/download/dotnet/2.1)

8. In Visual Studio, go to `Tools -> NuGet Package Manager -> Package Manager Console`
9. In the package manager console, run `Install-Package xunit.runner.visualstudio -Version 2.4.1`
10. Undo any changes that might now exist in your git working tree
11. Close Visual Studio, then re-open [odata.net/sln/odata.sln](OData.sln) from an administrator command prompt

13. Open the test explorer window from `Test -> Test Explorer`
14. Select all of the tests and `right click -> Run`; NOTE: The `Microsoft.OData.PublicApi.Tests` tests will not run unless individually selected. They will also fail if run. This issue is currently being tracked [here](TODO)
