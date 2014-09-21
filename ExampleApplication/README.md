AssemblyHost Examples
============

This project serves as a collection of examples for how to use the AssemblyHost. Each source file under the Examples folder represents a stand-alone example that can serve as a starting point in an application. The examples are designed in a way to hopefully make them self-documenting in the code.

# Running the Examples

To run the examples, open the AssemblyHostExample solution to build and run. Each example appears in the list on the left. Selecting an example opens it on the right, with a description of that example. Many examples allow for an input value to demonstrate different effects. After entering an input parameter, if applicable, click the Run Example button. Some examples allow or even require pressing the Stop Example button for the example to complete.

# Using the Examples

To use an example as a starting point for another application, first add a reference to AssemblyHost in the project. For simplicity, AssemblyHostExample uses a project reference rather than referencing an already built AssemblyHost.exe.

If running the example application from Visual Studio, you can click the link to the source file next to the name of any example to open that example in Visual Studio. This allows you to select an example whose run behavior you want to model in your application and then jump right to the source. Copy the Run and Stop (if applicable) methods and modify as needed.

# Adding an Example

To add a new example to the application, simply create a new class in the Examples folder that implements the IExample interface. No other registration is required.