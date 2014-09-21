AssemblyHost Examples
============

This project serves as a collection of examples for how to use the AssemblyHost. Each source file under the Examples folder represents a stand-alone example that can serve as a starting point in an application. The examples are designed in a way to hopefully make them self-documenting in the code.

# Running the Examples

To run the examples, open the AssemblyHostExample solution to build and run. Each example appears in the list on the left. Selecting an example opens it on the right, with a description of that example. Many examples allow for an input value to demonstrate different effects. After entering an input parameter, if applicable, click the Run Example button. Some examples allow or even require pressing the Stop Example button for the example to complete.

# Using the Examples

To use an example as a starting point for another application, first add a reference to AssemblyHost in the project. For simplicity, AssemblyHostExample uses a project reference rather than referencing an already built AssemblyHost.exe.

After adding a reference, find the example in the Examples folder and copy the Run and Stop (if applicable) methods. Modify as needed.

# Adding an Example

To add a new example to the application, simply create a new class in the Examples folder that implements the IExample interface. No other registration is required.