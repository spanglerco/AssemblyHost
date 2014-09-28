AssemblyHost Releases
============

The current released version of AssemblyHost is 1.1.0.0.

# 2.0.0.0 (Under development)

Source compatible with 1.1.0.0 unless code registered for the HostProgress event with a non-InterfaceHostProcess reference. Not binary compatible.

- Moved the HostProgress event from HostProcess to InterfaceHostProcess to clean up the API a bit.

# 1.1.0.0 (Released September 27, 2014)

Backwards compatible with 1.0.0.0 but introduces a new API function.

- Added HostProcess.WaitStopped method to provide a convenient way to wait for the child process to complete and return the result.
- Added example application containing several examples that can be used as starting points as well as explanations of each mode.
- Modified the AssemblyHost to attempt to notify the parent process of unhandled exceptions in the child and avoid the crash dialog.
- Modified tests to collect impact data and to be able to pass on slower machines.

# 1.0.0.0 (Released August 31, 2014)

Initial release of the AssemblyHost.