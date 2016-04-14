﻿using System;
using System.Collections.Generic;
using Bigio;
using Wintellect.PowerCollections;

namespace PerformanceTests
{
    public static class TestManager
    {
        private const int HUNDRED = 100;
        private const int THOUSAND = 1000;
        private const int MILLION = THOUSAND * THOUSAND;
        private const int BILLIARD = MILLION * THOUSAND;

        private static readonly TestEngine<BigArray<int>> _bigioTestEngine = new TestEngine<BigArray<int>>();
        private static readonly TestEngine<BigList<int>> _wintellectTestEngine = new TestEngine<BigList<int>>();
        private static readonly TestEngine<List<int>> _listTestEngine = new TestEngine<List<int>>();

        public static void TestAdd()
        {
            TestArguments arg = new TestArguments("Add", CallFlag.ClearTestList, new[] { MILLION / 10, MILLION, MILLION * 10, MILLION * 100});
            WriteResult("Add", _bigioTestEngine.GetResult(arg), _wintellectTestEngine.GetResult(arg), _listTestEngine.GetResult(arg));
        }

        public static void TestAddRange()
        {
            WriteResult("AddRange",
                GetBigioEngine().GetResult(new TestArguments("AddRange", CallFlag.ClearTestList, new[] { MILLION / 100, MILLION / 10, MILLION })),
                GetWintellectEngine().GetResult(new TestArguments("AddRange", CallFlag.ClearTestList, new[] { MILLION / 100, MILLION / 10, MILLION })),
                GetListEngine().GetResult(new TestArguments("AddRange", CallFlag.ClearTestList, new[] { MILLION / 100, MILLION / 10, MILLION })));
        }

        public static void TestInsertInStartPosition()
        {
            WriteResult("InsertInStartPosition",
                GetBigioEngine().GetResult(new TestArguments("InsertInStartPosition", CallFlag.ClearTestList, new[] { MILLION / 100, MILLION / 10, MILLION, MILLION * 10 })),
                GetWintellectEngine().GetResult(new TestArguments("InsertInStartPosition", CallFlag.ClearTestList, new[] { MILLION / 100, MILLION / 10, MILLION, MILLION * 10 })),
                GetListEngine().GetResult(new TestArguments("InsertInStartPosition", CallFlag.ClearTestList, new[] { MILLION / 100, MILLION / 10 })));
        }

        public static void TestInsertInMiddlePosition()
        {
            WriteResult("InsertInMiddlePosition",
                GetBigioEngine().GetResult(new TestArguments("InsertInMiddlePosition", CallFlag.ClearTestList, new[] { THOUSAND, MILLION / 100, MILLION / 10 })),
                GetWintellectEngine().GetResult(new TestArguments("InsertInMiddlePosition", CallFlag.ClearTestList, new[] { THOUSAND, MILLION / 100, MILLION / 10 })),
                GetListEngine().GetResult(new TestArguments("InsertInMiddlePosition", CallFlag.ClearTestList, new[] { THOUSAND, MILLION / 100, MILLION / 10 })));
        }

        public static void TestInsertInRandomPosition()
        {
            WriteResult("InsertInRandomPosition",
                GetBigioEngine().GetResult(new TestArguments("InsertInRandomPosition", CallFlag.ClearTestList, new[] { THOUSAND, MILLION / 100, MILLION / 10, MILLION })),
                GetWintellectEngine().GetResult(new TestArguments("InsertInRandomPosition", CallFlag.ClearTestList, new[] { THOUSAND, MILLION / 100, MILLION / 10, MILLION })),
                GetListEngine().GetResult(new TestArguments("InsertInRandomPosition", CallFlag.ClearTestList, new[] { THOUSAND, MILLION / 100, MILLION / 10 })));
        }

        public static void TestInsertRangeInRandom()
        {
            WriteResult("InsertRangeInRandom",
                GetBigioEngine().GetResult(new TestArguments("InsertRangeInRandom", CallFlag.ClearTestList, new[] { THOUSAND, MILLION / 100, MILLION / 10 })),
                GetWintellectEngine().GetResult(new TestArguments("InsertRangeInRandom", CallFlag.ClearTestList, new[] { THOUSAND, MILLION / 100, MILLION / 10 })),
                GetListEngine().GetResult(new TestArguments("InsertRangeInRandom", CallFlag.ClearTestList, new[] { THOUSAND, MILLION / 100 })));
        }

        public static void TestFor()
        {
            TestArguments arg = new TestArguments("For", CallFlag.FillTestList, new[] { 1, 4 });
            WriteResult("For", GetBigioEngine().GetResult(arg), GetWintellectEngine().GetResult(arg), GetListEngine().GetResult(arg));
        }

        public static void TestForeach()
        {
            TestArguments arg = new TestArguments("Foreach", CallFlag.FillTestList, new[] { 1, 5, 10 });
            WriteResult("Foreach", GetBigioEngine().GetResult(arg), GetWintellectEngine().GetResult(arg), GetListEngine().GetResult(arg));
        }

        public static void TestIndexOf()
        {
            TestArguments arg = new TestArguments("IndexOf", CallFlag.FillTestList, new[] { HUNDRED, THOUSAND, THOUSAND * 10 });
            WriteResult("IndexOf", GetBigioEngine().GetResult(arg), GetWintellectEngine().GetResult(arg), GetListEngine().GetResult(arg));
        }

        public static void TestBinarySearch()
        {
            TestArguments arg = new TestArguments("BinarySearch", CallFlag.FillTestList, new[] { THOUSAND, THOUSAND * 10, THOUSAND * 100 });
            WriteResult("BinarySearch", GetBigioEngine().GetResult(arg), GetWintellectEngine().GetResult(arg), GetListEngine().GetResult(arg));
        }

        public static void TestFindAll()
        {
            TestArguments arg = new TestArguments("FindAll", CallFlag.FillTestList, new[] { 2, 5, 7 });
            WriteResult("FindAll", GetBigioEngine().GetResult(arg), GetWintellectEngine().GetResult(arg), GetListEngine().GetResult(arg));
        }

        public static void TestReverse()
        {
            WriteResult("Reverse",
                GetBigioEngine().GetResult(new TestArguments("Reverse", CallFlag.FillTestList, new[] { 1, 5, 10, 15 })),
                GetWintellectEngine().GetResult(new TestArguments("Reverse", CallFlag.FillTestList, new[] { 1 })),
                GetListEngine().GetResult(new TestArguments("Reverse", CallFlag.FillTestList, new[] { 1, 5, 10, 15 })));
        }

        private static readonly object _writeResultLocker = new object();

        private static void WriteResult(string methodName, List<TestResult> bigioResult, List<TestResult> wintellectResult,
            List<TestResult> listResult)
        {
            lock (_writeResultLocker)
            {
                Console.WriteLine("_____________{0}_____________", methodName);
                Console.WriteLine("Count \t \t Bigio \t \t Wintellect \t \t Microsoft List");

                for (int i = 0; i < bigioResult.Count; i++)
                {
                    var count = bigioResult[i].CountOfObjects;

                    string bigioStr = "-",
                        wintellectStr = "-",
                        listStr = "-";

                    bigioStr = bigioResult[i].ElapsedMilliseconds.ToString();

                    if (i < wintellectResult.Count)
                        wintellectStr = wintellectResult[i].ElapsedMilliseconds.ToString();

                    if (i < listResult.Count)
                        listStr = listResult[i].ElapsedMilliseconds.ToString();

                    Console.WriteLine("{0,10} \t {1,10} \t {2,10} \t {3,10}", count, bigioStr, wintellectStr, listStr);
                }
            }
        }

        private static TestEngine<BigArray<int>> GetBigioEngine()
        {
            return new TestEngine<BigArray<int>>();
        }

        private static TestEngine<BigList<int>> GetWintellectEngine()
        {
            return new TestEngine<BigList<int>>();
        }

        private static TestEngine<List<int>> GetListEngine()
        {
            return new TestEngine<List<int>>();
        }
    }
}
