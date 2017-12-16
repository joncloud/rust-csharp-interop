using Rlx;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using static Rlx.Functions;

namespace ConsoleApp1
{
    class Program
    {
        delegate RustOptionInt32 MaybeParse(string s);
        delegate RustOptionVector3 MaybePos();
        delegate float Magnitude(Vector3 v);

        static void Main(string[] args)
        {
            string lib = Path.Combine(
                new FileInfo(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath).Directory.FullName,
                "lib.dll");

            using (var platform = new ShadowPlatformLibrary(lib))
            {
                var maybeParse = platform.GetFunc<MaybeParse, RustOptionInt32>("maybe_parse");

                var number = maybeParse.Invoke(fn => fn("123")).ToOption().UnwrapOr(-1);
                Console.WriteLine(number);
                number = maybeParse.Invoke(fn => fn("abc")).ToOption().UnwrapOr(-1);

                Console.WriteLine(number);

                var maybePos = platform.GetFunc<MaybePos, RustOptionVector3>("maybe_pos");

                var vec = maybePos.Invoke(fn => fn()).ToOption().Unwrap();
                Console.WriteLine(vec);

                var magnitude = platform.GetFunc<Magnitude, float>("magnitude");
                Console.WriteLine(magnitude.Invoke(fn => fn(vec)));
            }
        }
    }

    struct Vector3
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString() =>
            $"({X}, {Y}, {Z})";
    }

    static class RustOption
    {
        public static Option<T> ToOption<T>(byte isSome, T value) =>
            isSome == 1 ? Some(value) : None<T>();
    }

    struct RustOptionInt32
    {
        public readonly byte IsSome;
        internal readonly int Value;

        public Option<int> ToOption() =>
            RustOption.ToOption(IsSome, Value);
    }

    struct RustOptionVector3
    {
        public readonly byte IsSome;
        internal readonly Vector3 Value;

        public Option<Vector3> ToOption() =>
            RustOption.ToOption(IsSome, Value);
    }

    static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);
    }

    interface IPlatformFunc<T, TResult>
    {
        TResult Invoke(Func<T, TResult> fn);
    }

    interface IPlatformLibrary : IDisposable
    {
        IPlatformFunc<T, TResult> GetFunc<T, TResult>(string name);
    }

    class ShadowPlatformLibrary : IPlatformLibrary
    {
        IPlatformLibrary _inner;
        readonly FileSystemWatcher _watcher;
        public ShadowPlatformLibrary(string path)
        {
            var fileInfo = new FileInfo(path);
            var fileWithoutExtension = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);

            Func<string> getTempPath = () =>
            {
                fileInfo.Refresh();
                string lastWriteTime = fileInfo.LastWriteTime.ToString("yyyyMMddHHmmss");
                var p = Path.Combine(fileInfo.Directory.FullName, $"{fileWithoutExtension}_{lastWriteTime}{extension}");
                if (!File.Exists(p)) File.Copy(path, p);
                return p;
            };

            string tempPath = getTempPath();

            _inner = new Win32PlatformLibrary(tempPath);
            _watcher = new FileSystemWatcher(fileInfo.Directory.FullName, $"{fileWithoutExtension}{extension}")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            _watcher.Changed += (sender, e) =>
            {
                var temp = _inner;
                var newPath = getTempPath();
                _inner = new Win32PlatformLibrary(newPath);
                OnReload(EventArgs.Empty);
                temp.Dispose();
                File.Delete(tempPath);
                tempPath = newPath;
            };
            _watcher.EnableRaisingEvents = true;
        }

        public event EventHandler Reloaded;

        protected virtual void OnReload(EventArgs e)
        {
            Reloaded?.Invoke(this, e);
            Console.WriteLine("Reloaded");
        }

        public void Dispose() =>
            _inner.Dispose();

        public IPlatformFunc<T, TResult> LoadFunc<T, TResult>(string name) =>
            _inner.GetFunc<T, TResult>(name);

        public IPlatformFunc<T, TResult> GetFunc<T, TResult>(string name) =>
            new ShadowPlatformAction<T, TResult>(this, name);
    }

    class ShadowPlatformAction<T, TResult> : IPlatformFunc<T, TResult>
    {
        IPlatformFunc<T, TResult> _action;
        public ShadowPlatformAction(ShadowPlatformLibrary shadow, string name)
        {
            _action = shadow.LoadFunc<T, TResult>(name);
            shadow.Reloaded += delegate
            {
                _action = shadow.LoadFunc<T, TResult>(name);
            };
        }

        public TResult Invoke(Func<T, TResult> fn) =>
            _action.Invoke(fn);
    }

    class Win32PlatformLibrary : IPlatformLibrary
    {
        readonly IntPtr _dll;
        public Win32PlatformLibrary(string path) =>
            _dll = NativeMethods.LoadLibrary(path);

        IntPtr LoadProcedureAddress(string name)
        {
            var address = NativeMethods.GetProcAddress(_dll, name);
            if (address == IntPtr.Zero) throw new ArgumentOutOfRangeException(nameof(name), name, "Invalid procedure name");
            return address;
        }

        public IPlatformFunc<T, TResult> GetFunc<T, TResult>(string name)
        {
            var address = LoadProcedureAddress(name);
            var fn = Marshal.GetDelegateForFunctionPointer<T>(address);
            return new DelegatePlatformFunc<T, TResult>(fn);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                NativeMethods.FreeLibrary(_dll);

                disposedValue = true;
            }
        }

        ~Win32PlatformLibrary()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    class DelegatePlatformFunc<T, TResult> : IPlatformFunc<T, TResult>
    {
        readonly T _del;
        public DelegatePlatformFunc(T del) =>
            _del = del;

        public TResult Invoke(Func<T, TResult> fn) =>
            fn(_del);
    }
}
