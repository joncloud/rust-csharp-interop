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
        }
    }

    struct OptionInt
    {
        public byte IsSome { get; private set; }
        public int Value { get; private set; }

        public Option<int> ToOption() =>
            IsSome == 1 ? Some(Value) : None<int>();
    }

    static class RustLib
    {
        [DllImport("lib", EntryPoint = "maybe_parse")]
        public extern static OptionInt MaybeParse(string text);
    }
}
