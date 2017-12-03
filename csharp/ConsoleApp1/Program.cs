using Rlx;
using System;
using System.Runtime.InteropServices;
using static Rlx.Functions;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var number = RustLib.MaybeParse("123").ToOption().UnwrapOr(-1);
            Console.WriteLine(number);
            number = RustLib.MaybeParse("abc").ToOption().UnwrapOr(-1);

            Console.WriteLine(number);

            var vec = RustLib.MaybePos().ToOption().Unwrap();
            Console.WriteLine(vec);
            Console.WriteLine(vec.Magnitude());
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

        public float Magnitude() =>
            RustLib.Magnitude(this);

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

    static class RustLib
    {
        [DllImport("lib", EntryPoint = "maybe_parse")]
        public extern static RustOptionInt32 MaybeParse(string text);

        [DllImport("lib", EntryPoint = "maybe_pos")]
        public extern static RustOptionVector3 MaybePos();

        [DllImport("lib", EntryPoint = "magnitude")]
        public extern static float Magnitude(Vector3 vector);
    }
}
