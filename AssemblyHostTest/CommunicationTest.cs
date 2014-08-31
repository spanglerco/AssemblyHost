// This file is part of AssemblyHost.
// Copyright © 2014 Paul Spangler
//
// AssemblyHost is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// AssemblyHost is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with AssemblyHost.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SpanglerCo.AssemblyHost.Ipc;
using SpanglerCo.UnitTests.AssemblyHost.Mock;

namespace SpanglerCo.UnitTests.AssemblyHost
{
    /// <summary>
    ///This is a test class for CommunicationTest and is intended
    ///to contain all CommunicationTest Unit Tests
    ///</summary>
    [TestClass()]
    public class CommunicationTest
    {
        private static WaitCallbackTask<MockCommunication, MockCommunication.ReadMessageResult> TryReadMessageTask()
        {
            return new WaitCallbackTask<MockCommunication, MockCommunication.ReadMessageResult>(MockCommunication.TryReadMessageDelegate);
        }

        /// <summary>
        ///A test for SendMessage
        ///</summary>
        [TestMethod()]
        public void SendMessageTest()
        {
            using (MockCommunication server = new MockCommunication())
            {
                MockCommunication client;
                MessageType type = MessageType.SignalTerminate;

                using (client = new MockCommunication(server))
                {
                    WaitCallbackTask<MockCommunication, MockCommunication.ReadMessageResult> task = TryReadMessageTask();

                    // Server can send message to client.
                    Assert.IsTrue(server.SendMessage(type));
                    Assert.IsTrue(task.RunTask(client, 500));
                    Assert.IsTrue(task.Result.Success);
                    Assert.AreEqual(type, task.Result.Type);
                    Assert.IsNull(task.Result.Data);
                    Assert.IsNull(task.Result.Exception);

                    // Client can send message to server.
                    Assert.IsTrue(client.SendMessage(type));
                    Assert.IsTrue(task.RunTask(server, 500));
                    Assert.IsTrue(task.Result.Success);
                    Assert.AreEqual(type, task.Result.Type);
                    Assert.IsNull(task.Result.Data);
                    Assert.IsNull(task.Result.Exception);
                }

                // SendMessage after closed should return false.
                Assert.IsFalse(client.SendMessage(type));
                Assert.IsFalse(server.SendMessage(type));
            }
        }

        /// <summary>
        ///A test for SendMessage
        ///</summary>
        [TestMethod()]
        public void SendMessageDataTest()
        {
            using (MockCommunication server = new MockCommunication())
            {
                MockCommunication client;
                string data = "Testing";
                MessageType type = MessageType.Progress;

                using (client = new MockCommunication(server))
                {
                    WaitCallbackTask<MockCommunication, MockCommunication.ReadMessageResult> task = TryReadMessageTask();

                    // Server can send data to client.
                    Assert.IsTrue(server.SendMessage(type, data));
                    Assert.IsTrue(task.RunTask(client, 500));
                    Assert.IsTrue(task.Result.Success);
                    Assert.AreEqual(type, task.Result.Type);
                    Assert.AreEqual(data, task.Result.Data);
                    Assert.IsNull(task.Result.Exception);

                    // Client can send data to server.
                    Assert.IsTrue(client.SendMessage(type, data));
                    Assert.IsTrue(task.RunTask(server, 500));
                    Assert.IsTrue(task.Result.Success);
                    Assert.AreEqual(type, task.Result.Type);
                    Assert.AreEqual(data, task.Result.Data);
                    Assert.IsNull(task.Result.Exception);
                }

                // SendMessage after closed should return false.
                Assert.IsFalse(client.SendMessage(type, data));
                Assert.IsFalse(server.SendMessage(type, data));
            }
        }

        /// <summary>
        ///A test for SendMessage
        ///</summary>
        [TestMethod()]
        public void SendMessageExceptionTest()
        {
            using (MockCommunication server = new MockCommunication())
            {
                MockCommunication client;
                string message = "Testing";
                MessageType type = MessageType.ExecuteError;

                using (client = new MockCommunication(server))
                {
                    WaitCallbackTask<MockCommunication, MockCommunication.ReadMessageResult> task = TryReadMessageTask();

                    // Server can send exception to client.
                    Assert.IsTrue(server.SendMessage(type, new InvalidOperationException(message)));
                    Assert.IsTrue(task.RunTask(client, 500));
                    Assert.IsTrue(task.Result.Success);
                    Assert.AreEqual(type, task.Result.Type);
                    Assert.IsNull(task.Result.Data);
                    Assert.IsInstanceOfType(task.Result.Exception, typeof(InvalidOperationException));
                    Assert.AreEqual(message, task.Result.Exception.Message);

                    // Sending a valid exception not in the System assembly gets converted to TargetInvocationException.
                    Assert.IsTrue(server.SendMessage(type, new CustomException(message)));
                    Assert.IsTrue(task.RunTask(client, 500));
                    Assert.IsTrue(task.Result.Success);
                    Assert.AreEqual(type, task.Result.Type);
                    Assert.IsNull(task.Result.Data);
                    Assert.IsInstanceOfType(task.Result.Exception, typeof(TargetInvocationException));
                    Assert.IsTrue(task.Result.Exception.Message.Contains(typeof(CustomException).FullName));
                    Assert.IsTrue(task.Result.Exception.Message.Contains(message));

                    // Client can send message to server.
                    Assert.IsTrue(client.SendMessage(type, new InternalException(message)));
                    Assert.IsTrue(task.RunTask(server, 500));
                    Assert.IsTrue(task.Result.Success);
                    Assert.AreEqual(type, task.Result.Type);
                    Assert.IsNull(task.Result.Data);
                    Assert.IsInstanceOfType(task.Result.Exception, typeof(TargetInvocationException));
                    Assert.IsTrue(task.Result.Exception.Message.Contains(typeof(InternalException).FullName));
                    Assert.IsTrue(task.Result.Exception.Message.Contains(message));
                }

                // SendMessage after closed should return false.
                Assert.IsFalse(client.SendMessage(type, new InvalidOperationException(message)));
                Assert.IsFalse(server.SendMessage(type, new InvalidOperationException(message)));
            }
        }

        /// <summary>
        ///A test for SendMessage
        ///</summary>
        [TestMethod()]
        public void SendMessageDataExceptionTest()
        {
            using (MockCommunication server = new MockCommunication())
            {
                MockCommunication client;
                string data = "data message";
                string message = "Testing";
                MessageType type = MessageType.ExecuteError;

                using (client = new MockCommunication(server))
                {
                    WaitCallbackTask<MockCommunication, MockCommunication.ReadMessageResult> task = TryReadMessageTask();

                    // Server can send data and exception to client.
                    Assert.IsTrue(server.SendMessage(type, data, new InvalidOperationException(message)));
                    Assert.IsTrue(task.RunTask(client, 500));
                    Assert.IsTrue(task.Result.Success);
                    Assert.AreEqual(type, task.Result.Type);
                    Assert.AreEqual(data, task.Result.Data);
                    Assert.IsInstanceOfType(task.Result.Exception, typeof(InvalidOperationException));
                    Assert.AreEqual(message, task.Result.Exception.Message);

                    // Client can send message to server.
                    Assert.IsTrue(client.SendMessage(type, data, new InternalException(message)));
                    Assert.IsTrue(task.RunTask(server, 500));
                    Assert.IsTrue(task.Result.Success);
                    Assert.AreEqual(type, task.Result.Type);
                    Assert.AreEqual(data, task.Result.Data);
                    Assert.IsInstanceOfType(task.Result.Exception, typeof(TargetInvocationException));
                    Assert.IsTrue(task.Result.Exception.Message.Contains(typeof(InternalException).FullName));
                    Assert.IsTrue(task.Result.Exception.Message.Contains(message));
                }

                // SendMessage after closed should return false.
                Assert.IsFalse(client.SendMessage(type, data, new InvalidOperationException(message)));
                Assert.IsFalse(server.SendMessage(type, data, new InvalidOperationException(message)));
            }
        }

        /// <summary>
        ///A test for TryReadMessage
        ///</summary>
        [TestMethod()]
        public void TryReadMessageTest()
        {
            using (MockCommunication server = new MockCommunication())
            {
                MockCommunication client;
                WaitCallbackTask<MockCommunication, MockCommunication.ReadMessageResult> task = TryReadMessageTask();

                using (client = new MockCommunication(server))
                {
                    // TryReadMessage should block until there is a message to read.
                    Assert.IsFalse(task.RunTask(client, 500));

                    // TryReadMessage should unblock when a message arrives.
                    Assert.IsTrue(server.SendMessage(MessageType.SignalTerminate));
                    Assert.IsTrue(task.WaitForTask(500));
                    Assert.IsTrue(task.Result.Success);
                    Assert.AreEqual(MessageType.SignalTerminate, task.Result.Type);
                }

                // TryReadMessage should return false after connection is closed.
                Assert.IsTrue(task.RunTask(client, 500));
                Assert.IsFalse(task.Result.Success);
            }
        }

        /// <summary>
        ///A test for WaitForRead
        ///</summary>
        [TestMethod()]
        public void WaitForReadTest()
        {
            using (MockCommunication server = new MockCommunication())
            {
                using (MockCommunication client = new MockCommunication(server))
                {
                    string data;
                    Exception ex;
                    MessageType type;

                    // WaitForRead should return immediately when no data has been written.
                    WaitCallbackTask<MockCommunication> task = new WaitCallbackTask<MockCommunication>(MockCommunication.WaitForReadDelegate);
                    Assert.IsTrue(task.RunTask(client, 500));
                    Assert.IsTrue(task.RunTask(server, 500));

                    // WaitForRead should block when there is pending data.
                    Assert.IsTrue(server.SendMessage(MessageType.SignalTerminate));
                    Assert.IsTrue(task.RunTask(client, 500));
                    Assert.IsFalse(task.RunTask(server, 500));

                    // WaitForRead should unblock once the data is read.
                    Assert.IsTrue(client.TryReadMessage(out type, out data, out ex));
                    Assert.AreEqual(MessageType.SignalTerminate, type);

                    Assert.IsTrue(task.WaitForTask(500));
                    Assert.IsTrue(task.RunTask(client, 500));
                }
            }
        }

        #region Helper classes

        /// <summary>
        /// An exception used to test SendMessage.
        /// </summary>

        [Serializable]
        public class CustomException : ApplicationException
        {
            public CustomException() { }
            public CustomException(string message) : base(message) { }
            public CustomException(string message, Exception inner) : base(message, inner) { }
            protected CustomException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context)
                : base(info, context) { }
        }

        /// <summary>
        /// An exception with an internal constructor used to test SendMessage.
        /// </summary>

        [Serializable]
        public class InternalException : Exception
        {
            public InternalException() { }
            internal InternalException(string message) : base(message) { }
            public InternalException(string message, Exception inner) : base(message, inner) { }
            protected InternalException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context)
                : base(info, context) { }
        }

        /// <summary>
        /// Base class for running a task on another thread and waiting for it to complete.
        /// </summary>

        private abstract class WaitCallbackTask
        {
            private AutoResetEvent _endEvent;
            private ManualResetEventSlim _startEvent;

            protected WaitCallbackTask()
            {
                _endEvent = new AutoResetEvent(false);
                _startEvent = new ManualResetEventSlim(false);
            }

            /// <summary>
            /// Waits for the task to complete. Must have started the task before calling.
            /// </summary>
            /// <param name="timeout">The number of milliseconds to allow the task to complete after it has started.</param>
            /// <returns>True if the task completed, false on timeout.</returns>

            public bool WaitForTask(int timeout)
            {
                _startEvent.Wait();

                if (_endEvent.WaitOne(timeout))
                {
                    _startEvent.Reset();
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Runs the task. Called on the worker thread.
            /// </summary>

            protected abstract void RunTask();

            /// <summary>
            /// Thread callback used to run the task.
            /// </summary>
            /// <param name="arg"></param>

            protected static void CallbackProc(object arg)
            {
                WaitCallbackTask task = arg as WaitCallbackTask;

                try
                {
                    task._startEvent.Set();
                    task.RunTask();
                    task._endEvent.Set();
                }
                catch (ThreadAbortException)
                { }
            }
        }

        /// <summary>
        /// A wait callback task that takes in a single argument.
        /// </summary>
        /// <typeparam name="TArg">The type of argument to pass to the task.</typeparam>

        private class WaitCallbackTask<TArg> : WaitCallbackTask
        {
            private TArg _arg;
            private Action<TArg> _task;

            /// <summary>
            /// Creates a new wait callback task.
            /// </summary>
            /// <param name="task">The task to run on a worker thread.</param>

            public WaitCallbackTask(Action<TArg> task)
            {
                _task = task;
            }

            /// <summary>
            /// Queues the task to run on a worker thread. Use WaitForTask to determine when it has completed.
            /// </summary>
            /// <param name="arg">The argument to pass to the task when it runs.</param>

            public void StartTask(TArg arg)
            {
                _arg = arg;
                ThreadPool.QueueUserWorkItem(CallbackProc, this);
            }

            /// <summary>
            /// Queues the task to run on a worker thread and waits for it to finish.
            /// </summary>
            /// <param name="arg">The argument to pass to the task when it runs.</param>
            /// <param name="timeout">The number of milliseconds to wait for the task to complete once it has started.</param>
            /// <returns>True if the task completed within the timeout, false if not.</returns>

            public bool RunTask(TArg arg, int timeout)
            {
                StartTask(arg);
                return WaitForTask(timeout);
            }

            /// <see cref="WaitCallbackTask.RunTask"/>

            protected override void RunTask()
            {
                _task(_arg);
            }
        }

        /// <summary>
        /// A wait callback task that takes in a single argument and returns a value.
        /// </summary>
        /// <typeparam name="TArg">The type of argument to pass to the task.</typeparam>
        /// <typeparam name="TReturn">The type of value returned by the task.</typeparam>

        private class WaitCallbackTask<TArg, TReturn> : WaitCallbackTask
        {
            private TArg _arg;
            private Func<TArg, TReturn> _task;

            /// <summary>
            /// Gets the return value of the task after it has completed.
            /// </summary>

            public TReturn Result { get; private set; }

            /// <summary>
            /// Creates a new wait callback task.
            /// </summary>
            /// <param name="task">The task to run on a worker thread.</param>

            public WaitCallbackTask(Func<TArg, TReturn> task)
            {
                _task = task;
            }

            /// <summary>
            /// Queues the task to run on a worker thread. Use WaitForTask to determine when it has completed.
            /// </summary>
            /// <param name="arg">The argument to pass to the task when it runs.</param>

            public void StartTask(TArg arg)
            {
                _arg = arg;
                ThreadPool.QueueUserWorkItem(CallbackProc, this);
            }

            /// <summary>
            /// Queues the task to run on a worker thread and waits for it to finish.
            /// </summary>
            /// <param name="arg">The argument to pass to the task when it runs.</param>
            /// <param name="timeout">The number of milliseconds to wait for the task to complete once it has started.</param>
            /// <returns>True if the task completed within the timeout, false if not.</returns>

            public bool RunTask(TArg arg, int timeout)
            {
                StartTask(arg);
                return WaitForTask(timeout);
            }

            /// <see cref="WaitCallbackTask.RunTask"/>

            protected override void RunTask()
            {
                Result = _task(_arg);
            }
        }

        #endregion
    }
}
