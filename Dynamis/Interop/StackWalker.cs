using System.Reflection;
using Dynamis.Interop.Win32;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.AbstractDac;
using Microsoft.Extensions.DependencyInjection;

namespace Dynamis.Interop;

public sealed unsafe class StackWalker(SymbolApi symbolApi, Lazy<ClrRuntime> runtime)
{
    public unsafe StackFrame[] StackWalk(ref readonly Context context, SymbolApi.ReadProcessMemoryRoutine? readProcessMemory)
    {
        var threadId = ProcessThreadApi.GetCurrentThreadId();
        using (var walk = new Walk(runtime.Value, threadId, null /*(Context*)Unsafe.AsPointer(ref Unsafe.AsRef(in context))*/)) {
            ThreadPool.QueueUserWorkItem(Process, walk, true);
            _ = walk.Result;
        }

        return symbolApi.StackWalk(in context, readProcessMemory);
    }

    private static void Process(Walk walk)
        => walk.Process();

    private static IServiceProvider GetServices(ClrRuntime runtime)
        => (IServiceProvider)runtime.GetType()
                                    .GetField("_services", BindingFlags.Instance | BindingFlags.NonPublic)!
                                    .GetValue(runtime)!;

    private sealed class Walk(ClrRuntime runtime, uint threadId, Context* context) : IDisposable
    {
        private readonly ManualResetEvent _event  = new(false);
        private          StackFrame[]     _result = [];

        public StackFrame[] Result
        {
            get
            {
                _event.WaitOne();
                return _result;
            }
        }

        public void Dispose()
            => _event.Dispose();

        public void Process()
        {
            using var hThread = SafeThreadHandle.Open(context is not null ? 0x1Au : 0x2u, false, threadId);
            hThread.Suspend();
            try {
                if (context is not null) {
                    var originalContext = new Context
                    {
                        ContextFlags = 0x0010003Fu,
                    };
                    hThread.GetContext(ref originalContext);
                    hThread.SetContext(in *context);
                    try {
                        DoProcess(threadId);
                    } finally {
                        hThread.SetContext(in originalContext);
                    }
                } else {
                    DoProcess(threadId);
                }
            } finally {
                hThread.Resume();
            }
        }

        private void DoProcess(uint threadId)
        {
            var threadHelpers = GetServices(runtime).GetService<IAbstractThreadHelpers>()!;
            foreach (var frame in threadHelpers.EnumerateStackTrace(threadId, false, false)) {
                var method = runtime.GetMethodByInstructionPointer(frame.InstructionPointer);
                Plugin.Log!.Info(
                    "{IP:X16} {SP:X16} {Internal} {Name} {Method}", frame.InstructionPointer, frame.StackPointer,
                    frame.IsInternalFrame,
                    frame.InternalFrameName ?? string.Empty,
                    method
                );
            }

            _event.Set();
        }
    }
}
