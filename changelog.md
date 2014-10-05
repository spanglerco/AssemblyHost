AssemblyHost Releases
============

The current released version of AssemblyHost is 1.1.0.0.

# 2.0.0.0 (Under development)

Source compatible with 1.1.0.0 unless code registered for the HostProgress event with a non-InterfaceHostProcess reference. Not binary compatible.

- Added AssemblyHostLauncher32 which enables hosting 32-bit assemblies on a 64-bit operating system. __Behavior change:__ if the parent process is 32-bit, the child process will also be 32-bit by default, where previous a 64-bit child process was always created. Use the new HostBitness parameter when creating a host process to control the bitness of the child process.
- Added the ability to host duplex WCF services using an overload of CreateChannel on WcfHostProcess to pass in a callback object.
- Moved the HostProgress event from HostProcess to InterfaceHostProcess to clean up the API a bit.

# 1.1.0.0 (Released September 27, 2014)

Backwards compatible with 1.0.0.0 but introduces a new API function.

- Added HostProcess.WaitStopped method to provide a convenient way to wait for the child process to complete and return the result.
- Added example application containing several examples that can be used as starting points as well as explanations of each mode.
- Modified the AssemblyHost to attempt to notify the parent process of unhandled exceptions in the child and avoid the crash dialog.
- Modified tests to collect impact data and to be able to pass on slower machines.

# 1.0.0.0 (Released August 31, 2014)

Initial release of the AssemblyHost.