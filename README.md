AssemblyHost
============

A .NET library for running assemblies in another process while communicating with it. This may be useful for robustness testing (e.g. terminate the child process while it is executing) or for running buggy code without crashing the main process.

There are three modes for hosting an assembly: method, interface, and WCF. Method is the simplest: call a single static method with no arguments or instantiate a class using the default constructor then call an instance method. The return value of the method, if there is one, is converted to a string and sent back to the parent process. Interface mode provides argument passing from parent to child, progress reports from child to parent, as well as the ability to send a stop signal from parent to child. This is done by implementing the `IChildProcess` interface on a class with a default constructor. Finally, the WCF mode is the most versatile, allowing custom interfaces with two-way communication.

# Sample Usage (WCF)

```C#
using (WcfHostProcess process = new WcfHostProcess(new TypeArgument(typeof(BuggyService))))
{
	process.Start(true);
	
	using (WcfChildContract<IBuggyContract> channel = process.CreateChannel<IBuggyContract>())
	{
		int sum = channel.Contract.BuggyAdd(5, 3);
	}
}
```

`BuggyService` is a WCF service that implements the `IBuggyContract` service contract. The child process will shut down when `process.Stop()` is called or when it gets disposed at the end of the using block. For information on how to write a WCF service, see [Implementing Service Contracts](http://msdn.microsoft.com/en-us/library/ms733764%28v=vs.100%29.aspx) and for more advanced options [Designing Service Contracts](http://msdn.microsoft.com/en-us/library/ms733070%28v=vs.100%29.aspx).

# Goals and Non-Goals

This library attempts to accomplish the following goals:
- Provide a means to test the robustness of code by simulating crashes.
- Protect the main process from buggy code that may crash.

This library does _not_ attempt to and has not been tested for:
- Providing security of any kind.
- Execute code in an untrusted assembly.
- Run in a high performance context.

Per those goals, the library uses Argument classes that accept paths and names rather than binding to loaded types. This allows the parent process to never have to load the type being executed in the child process (protects from buggy static constructors), but this should not be mistaken for being a security measure.

# Using the Library

For examples of how to use the library in each mode, build and run the AssemblyHostExample application (see ExampleApplication\README.md). In general, use the library as follows:

1. Select which host you want to use: method, interface, or WCF.
2. If needed, write the target that will run in the child.
	- __Method__: Unless the code you want to run happens to fall within the requirements already, write a wrapper static method that returns a String.
	- __Interface__: Write a class that implements `IChildProcess`. Select an appropriate ExecutionMode based on how the child process should stop (i.e. stop when execute is done, when signaled by the parent, or either).
	- __WCF__: Unless the code you want to run already implements a service contract, write an [interface and class](http://msdn.microsoft.com/en-us/library/ms733764%28v=vs.100%29.aspx) that implements that interface.
3. Instantiate the selected HostProcess class, passing in the information on what to execute in the child process (don't forget the using block or store it in a field in an IDisposable class).
4. Register for progress or status change events if desired.
5. Call Start.
6. Interact with the child as needed (i.e. ChildProcess property or CreateChannel method for WCF).
7. Call Stop (WCF and some interface modes only).
8. Retrieve the Result (if applicable for the method or interface) and Error properties once the Status is Stopped or Error.

Note that the WaitStopped method will handle both steps 7 and 8 for you.

## Hosting Assemblies Built for Various Platforms (Bitness)

In order to be flexible, AssemblyHost is built for the "Any CPU" platform, which means on a 64-bit operating system it will create a 64-bit process but can still be loaded by a 32-bit process. However, a 64-bit process cannot load an assembly built for the x86 platform. To enable hosting 32-bit assemblies on a 64-bit OS, deploy the AssemblyHostLauncher32 assembly along with AssemblyHost. The launcher is a tiny executable that AssemblyHost invokes in order to create a 32-bit process before handing things over to AssemblyHost for the rest.

By default, the child process created by AssemblyHost will match the same bitness as the parent process. This means even if the assembly to be hosted is built for any CPU, it may still load into a 32-bit process by default. When instantiating a host process class, you may optionally specify a HostBitness value to control the bitness of the child process:
- __Native__ provides the same behavior as the Any CPU platform, meaning the child process will be 64-bit on a 64-bit OS. This is the pre-2.0 behavior and may be appropriate for assemblies built for any CPU.
- __Current__ matches the bitness of the parent process and is the default in order to provide predictable behavior.
- __Force32__ always makes a 32-bit child process. Use this option when the assembly being hosted is built for the x86 platform.
- __Force64__ always makes a 64-bit child process, or throws an exception at run-time on a 32-bit OS. Use this option when the assembly being hosted is built for the x64 platform.

Note that when the child process is 64-bit, the launcher is not required to be deployed. To avoid unexpected run-time errors, explicitly use Force64 when not deploying the launcher.

# Building the Library

One difficulty in providing open source .NET code is with strong naming of assemblies. Microsoft recommends signing all assemblies, and doing so restricts that assembly to only being able to reference other signed assemblies. Rather than adding a private key to the repository (which defeats the purpose), there are ways to build the assembly with your own key. Note that strongly named assemblies signed with different keys are not interchangeable at run time; the client must choose which one to use at compile time.

## Debug Builds

As of 2.0, the debug build of AssemblyHost no longer automatically triggers a breakpoint. To debug the child process, see the Main in Child\Program.cs for enabling a breakpoint allowing you to attach a debugger when the child process is created.

## Unsigned

If you don't need a strongly named assembly or plan to sign it manually later, you can create an unsigned build of AssemblyHost. Just open the solution and hit build using the Debug or Release configurations.

## Signed (Strongly Named)

Unless you are making modifications to the code, an alternative to building a signed assembly yourself is to use the assembly provided with each [release on GitHub](https://github.com/spanglerco/AssemblyHost/releases/latest).

The csproj file has been configured to look for a SigningKey environment variable so the project doesn't require modification to sign it. There are three ways to create a signed build:

1. Open the solution and manually edit the AssemblyHost project to choose your key then build the Debug, Release, or Analysis configurations.
2. Open a command prompt, set the SigningKey environment variable to the path of your key, launch Visual Studio from the command prompt, then build any of the configurations.
3. Open a Visual Studio command prompt, set the SigningKey environment variable to the path of your key, then run msbuild from the directory containing the solution (e.g. `msbuild /p:Configuration=Release`).

__Note:__ Be sure to do a clean build when switching between Signed and Unsigned builds. In Visual Studio, this means Build->Clean. For msbuild, use `msbuild /t:Clean /p:Configuration=Release`.

You can verify if the build assembly is signed or not by opening a Visual Studio command prompt and running `sn -T <path-to-AssemblyHost.exe>`. For an unsigned build, you get a message indicating it is not strongly named. For a signed build, you will see the public token which should correspond to the key used to sign it.

__Important:__ When creating a signed build, it is required that AssemblyHost and the AssemblyHostLauncher32 use the same key. Running msbuild from the directory containing the solution will build both projects, but be sure to get the output from the launcher's bin directory (both assemblies will be there).

## Code Analysis

The Analysis build configuration has code analysis enabled. Note that this configuration should not be used when building an unsigned assembly or you will receive a large number of warnings.

## Unit Tests

Conversely, the Test and Test32 build configurations will always produce an unsigned assembly regardless of the SigningKey variable. These are the only build configurations that include the AssemblyHostTest project, which contains all of the unit tests for AssemblyHost. The Test configuration produces 64-bit unit tests, while Test32 produces 32-bit tests. Additionally, there are two testsettings files: AssemblyHost.testsettings will run the tests in a 64-bit process, while AssemblyHost32.testsettings will run in a 32-bit process.

When running the tests, the following configurations should all pass:
- Test + AssemblyHost.testsettings: 64-bit process that creates 64-bit child processes hosting an Any CPU assembly
- Test + AssemblyHost32.testsettings: 32-bit process that creates 32-bit child processes hosting an Any CPU assembly
- Test32 + AssemblyHost32.testsettings: 32-bit process that creates 32-bit child processes hosting an x86 assembly

Some tests will momentarily open a console window. This is intentional.