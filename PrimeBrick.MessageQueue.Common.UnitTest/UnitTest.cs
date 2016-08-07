using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Dynamic;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Collections.Specialized;

namespace PrimeBrick.MessageQueue.Common.UnitTest
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestPrimitiveSerialization()
        {
            NetworkSerializer NS = new NetworkSerializer();
            using (MemoryStream MS = new MemoryStream())
            {
                //BOOLEAN
                bool BooleanValue = true;
                NS.Serialize(MS, BooleanValue);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), BooleanValue);
                MS.Position = 0;

                //SHORT
                short ShortNumber = short.MinValue;
                NS.Serialize(MS, ShortNumber);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), ShortNumber);
                MS.Position = 0;

                //UNSIGNED SHORT
                ushort UShortNumber = ushort.MaxValue;
                NS.Serialize(MS, UShortNumber);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), UShortNumber);
                MS.Position = 0;

                //INTEGER
                int IntegerNumber = int.MinValue;
                NS.Serialize(MS, IntegerNumber);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), IntegerNumber);
                MS.Position = 0;

                //UNSIGNED INTEGER
                uint UIntegerNumber = uint.MaxValue;
                NS.Serialize(MS, UIntegerNumber);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), UIntegerNumber);
                MS.Position = 0;

                //LONG
                long LongNumber = long.MinValue;
                NS.Serialize(MS, LongNumber);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), LongNumber);
                MS.Position = 0;

                //UNSIGNED LONG
                ulong ULongNumber = ulong.MaxValue;
                NS.Serialize(MS, ULongNumber);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), ULongNumber);
                MS.Position = 0;

                MS.SetLength(0);

                //SINGLE
                Single SingleNumber = Single.MinValue;
                NS.Serialize(MS, SingleNumber);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), SingleNumber);
                MS.Position = 0;

                //DOUBLE
                double DoubleNumber = double.MaxValue;
                NS.Serialize(MS, DoubleNumber);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), DoubleNumber);
                MS.Position = 0;

                MS.SetLength(0);

                //STRING
                string LiteralString = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce ac sem rutrum, scelerisque ex aliquet, ornare lorem. Nunc elementum mattis commodo. Curabitur non sem accumsan, tincidunt nunc nec, laoreet nulla. Nullam a risus nisl. Curabitur blandit urna eget mi varius imperdiet. Aliquam mattis nulla id neque placerat, ut iaculis ante hendrerit. Cras diam lacus, rutrum sit amet ipsum at, finibus commodo nibh";
                NS.Serialize(MS, LiteralString);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), LiteralString);
                MS.Position = 0;

                MS.SetLength(0);

                //DECIMAL
                Decimal DecimalNumber = decimal.MinValue;
                NS.Serialize(MS, DecimalNumber);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), DecimalNumber);
                MS.Position = 0;

                MS.SetLength(0);

                //GUID
                Guid UUID = Guid.NewGuid();
                NS.Serialize(MS, UUID);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), UUID);
                MS.Position = 0;

                MS.SetLength(0);

                //DATETIME
                DateTime DateAndTime = DateTime.Now;
                NS.Serialize(MS, DateAndTime);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), DateAndTime);
                MS.Position = 0;

                //TIMESPAN
                TimeSpan TimespanValue = TimeSpan.FromDays(1);
                NS.Serialize(MS, TimespanValue);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), TimespanValue);
                MS.Position = 0;

                //DATETIMEOFFSET
                DateTimeOffset DateAndTimeOffset = DateTimeOffset.Now;
                NS.Serialize(MS, DateAndTimeOffset);
                MS.Position = 0;
                Assert.AreEqual(NS.Deserialize(MS), DateAndTimeOffset);
                MS.Position = 0;

            }
        }

        [TestMethod]
        public void TestArrayAndDictionarySerialization()
        {
            NetworkSerializer NS = new NetworkSerializer();
            using (MemoryStream MS = new MemoryStream())
            {
                #region "NOT TYPED"
                //ARRAY
                int[] ArrayOfInt = new int[5] { 0, 1, 2, 3, 4 };
                NS.Serialize(MS, ArrayOfInt);
                MS.Position = 0;
                var ArrayResult = (int[])NS.Deserialize(MS);
                Assert.AreEqual(ArrayResult.Length, ArrayOfInt.Length);
                Assert.AreEqual(ArrayResult[2], ArrayOfInt[2]);
                MS.Position = 0;
                MS.SetLength(0);

                //LIST
                List<int> ListOfInt = new List<int>() { 2, 4, 8, 16, 32, 64 };
                NS.Serialize(MS, ListOfInt);
                MS.Position = 0;
                ArrayResult = (int[])NS.Deserialize(MS);
                Assert.AreEqual(ArrayResult.Length, ListOfInt.Count);
                Assert.AreEqual(ArrayResult[4], ListOfInt[4]);
                MS.Position = 0;
                MS.SetLength(0);

                //DICTIONARY
                Dictionary<string, int> DictionaryOfStringInt = new Dictionary<string, int>() { { "A", 1 }, { "B", 2 }, { "C", 3 } };
                NS.Serialize(MS, DictionaryOfStringInt);
                MS.Position = 0;
                var DictionaryResult = (IDictionary<string, int>)NS.Deserialize(MS);
                Assert.AreEqual(DictionaryResult.Count, DictionaryOfStringInt.Count);
                Assert.AreEqual(DictionaryResult["B"], DictionaryOfStringInt["B"]);
                MS.Position = 0;
                MS.SetLength(0);

                //ARRAYLIST
                ArrayList ArrayListOfObject = new ArrayList() { 11, "22", true };
                NS.Serialize(MS, ArrayListOfObject);
                MS.Position = 0;
                var ArrayListResult = (object[])NS.Deserialize(MS);
                Assert.AreEqual(ArrayListResult.Length, ArrayListOfObject.Count);
                Assert.AreEqual(ArrayListResult[1], ArrayListOfObject[1]);
                MS.Position = 0;
                MS.SetLength(0);

                //ARRAY MULTI-TYPE
                object[] ArrayOfObjects = new object[] { 11, "22", true, DateTime.Now };
                NS.Serialize(MS, ArrayOfObjects);
                MS.Position = 0;
                var ArrayObjectResult = (object[])NS.Deserialize(MS);
                Assert.AreEqual(ArrayObjectResult.Length, ArrayOfObjects.Length);
                Assert.AreEqual(ArrayObjectResult[3], ArrayOfObjects[3]);
                MS.Position = 0;
                MS.SetLength(0);
                #endregion

                #region "STRONGLY TYPED"
                //LIST TYPED RESULT
                ListOfInt = new List<int>() { 0, 1, 2, 3, 5, 8, 13, 21, 34, 55 };
                NS.Serialize(MS, ListOfInt);
                MS.Position = 0;
                var TypedList = NS.Deserialize<List<int>>(MS);
                Assert.AreEqual(TypedList.Count, ListOfInt.Count);
                Assert.AreEqual(TypedList[8], ListOfInt[8]);
                MS.Position = 0;
                MS.SetLength(0);

                //ARRAY TYPED RESULT
                ArrayOfInt = new int[] { 0, 1, 2, 3, 5, 8, 13, 21, 34, 55 };
                NS.Serialize(MS, ArrayOfInt);
                MS.Position = 0;
                var TypedArray = NS.Deserialize<int[]>(MS);
                Assert.AreEqual(TypedArray.Length, ArrayOfInt.Length);
                Assert.AreEqual(TypedArray[1], ArrayOfInt[1]);
                MS.Position = 0;
                MS.SetLength(0);

                //ARRAYLIST
                ArrayListOfObject = new ArrayList() { 11, "22", true, DateTime.Now, Guid.NewGuid(), -34.54M };
                NS.Serialize(MS, ArrayListOfObject);
                MS.Position = 0;
                var TypedArrayList = NS.Deserialize<ArrayList>(MS);
                Assert.AreEqual(TypedArrayList.Count, ArrayListOfObject.Count);
                Assert.AreEqual(TypedArrayList[4], ArrayListOfObject[4]);
                MS.Position = 0;
                MS.SetLength(0);

                //ARRAY MULTI-TYPE
                ArrayOfObjects = new object[] { 11, "22", true, DateTime.Now, Guid.NewGuid(), -34.54M };
                NS.Serialize(MS, ArrayOfObjects);
                MS.Position = 0;
                var TypedArrayObject = NS.Deserialize<object[]>(MS);
                Assert.AreEqual(TypedArrayObject.Length, ArrayOfObjects.Length);
                Assert.AreEqual(TypedArrayObject[3], ArrayOfObjects[3]);
                MS.Position = 0;
                MS.SetLength(0);

                //DICTIONARY
                Dictionary<int, bool> DictionaryOfIntBool = new Dictionary<int, bool>() { { 10, true }, { 40, false }, { 25, true } };
                NS.Serialize(MS, DictionaryOfIntBool);
                MS.Position = 0;
                var TypedDictionaryIntBool = NS.Deserialize<Dictionary<int, bool>>(MS);
                Assert.AreEqual(TypedDictionaryIntBool.Count, DictionaryOfIntBool.Count);
                Assert.AreEqual(TypedDictionaryIntBool[25], DictionaryOfIntBool[25]);
                Assert.AreEqual(TypedDictionaryIntBool[40], false);
                MS.Position = 0;
                MS.SetLength(0);
                #endregion

                #region "TO DIFFERENT STRONGLY TYPED"
                //LIST TO DIFFERENT TYPED RESULT
                ListOfInt = new List<int>() { 0, 1, 2, 3, 5, 8, 13, 21, 34, 55 };
                NS.Serialize(MS, ListOfInt);
                MS.Position = 0;
                TypedArray = NS.Deserialize<int[]>(MS);
                Assert.AreEqual(TypedArray.Length, ListOfInt.Count);
                Assert.AreEqual(TypedArray[6], ListOfInt[6]);
                MS.Position = 0;
                MS.SetLength(0);

                //ARRAY TO DIFFERENT TYPED RESULT
                ArrayOfInt = new int[] { 0, 1, 2, 3, 5, 8, 13, 21, 34, 55 };
                NS.Serialize(MS, ArrayOfInt);
                MS.Position = 0;
                TypedList = NS.Deserialize<List<int>>(MS);
                Assert.AreEqual(TypedList.Count, ArrayOfInt.Length);
                Assert.AreEqual(TypedList[9], ArrayOfInt[9]);
                MS.Position = 0;
                MS.SetLength(0);

                //ARRAYLIST TO DIFFERENT TYPED RESULT
                ArrayListOfObject = new ArrayList { 0, 1, 2, 3, 5, 8, 13, 21, 34, 55 };
                NS.Serialize(MS, ArrayListOfObject);
                MS.Position = 0;
                TypedList = NS.Deserialize<List<int>>(MS);
                MS.Position = 0;
                TypedArray = NS.Deserialize<int[]>(MS);
                Assert.AreEqual(TypedList.Count, ArrayListOfObject.Count);
                Assert.AreEqual(TypedList[9], ArrayListOfObject[9]);
                Assert.AreEqual(TypedArray.Length, ArrayListOfObject.Count);
                Assert.AreEqual(TypedArray[7], ArrayListOfObject[7]);
                MS.Position = 0;
                MS.SetLength(0);

                #endregion

                #region "All other kind of collections"

                //BitArray is not an array of bool, it is an array of bit. 
                //A boolean value need 1 byte in memory, 1 byte = 8 bits
                //BitArray is a list of 1 or 0 composing a binary word and not a list of boolean values
                var BitArrayOfBool = new BitArray(new bool[] { false, true, true, false });
                NS.Serialize(MS, BitArrayOfBool);
                MS.Position = 0;
                ArrayOfObjects = (object[])NS.Deserialize(MS);
                MS.Position = 0;
                var TypedBitArray = NS.Deserialize<BitArray>(MS);
                Assert.AreEqual(ArrayOfObjects.Length, BitArrayOfBool.Count);
                Assert.AreEqual(ArrayOfObjects[3], BitArrayOfBool[3]);
                Assert.AreEqual(ArrayOfObjects[2], true);
                Assert.AreEqual(TypedBitArray.Length, BitArrayOfBool.Count);
                Assert.AreEqual(TypedBitArray[1], BitArrayOfBool[1]);
                Assert.AreEqual(TypedBitArray[0], false);
                MS.Position = 0;
                MS.SetLength(0);


                var QueueOfInt = new Queue<int>(new int[] { 1, 2, 3, 5, 8, 13, 21 });
                NS.Serialize(MS, QueueOfInt);
                MS.Position = 0;
                ArrayResult = (int[])NS.Deserialize(MS);
                MS.Position = 0;
                var TypedQueue = NS.Deserialize<Queue<int>>(MS);
                Assert.AreEqual(ArrayResult.Length, QueueOfInt.Count);
                Assert.AreEqual(ArrayResult[0], QueueOfInt.Dequeue());
                //Because the original queue lost first item from previous assertion, TypedQueue also should lost first item
                TypedQueue.Dequeue();
                Assert.AreEqual(TypedQueue.Count, QueueOfInt.Count);
                Assert.AreEqual(TypedQueue.Dequeue(), QueueOfInt.Dequeue());
                MS.Position = 0;
                MS.SetLength(0);


                var StackOfInt = new Stack<int>(new int[] { 1, 2, 3, 5, 8, 13, 21 });
                NS.Serialize(MS, StackOfInt);
                MS.Position = 0;
                ArrayResult = (int[])NS.Deserialize(MS);
                MS.Position = 0;
                var TypedStack = NS.Deserialize<Stack<int>>(MS);
                Assert.AreEqual(ArrayResult.Length, StackOfInt.Count);
                Assert.AreEqual(ArrayResult[0], StackOfInt.Pop());
                //Because the original queue lost first item from previous assertion, TypedStack also should lost first item
                TypedStack.Pop();
                Assert.AreEqual(TypedStack.Count, StackOfInt.Count);
                Assert.AreEqual(TypedStack.Pop(), 2);
                Assert.AreEqual(StackOfInt.Pop(), 13);
                MS.Position = 0;
                MS.SetLength(0);


                var LinkedListOfInt = new LinkedList<int>(new int[] { 1, 2, 3, 5, 8, 13, 21 });
                NS.Serialize(MS, LinkedListOfInt);
                MS.Position = 0;
                ArrayResult = (int[])NS.Deserialize(MS);
                MS.Position = 0;
                var TypedLinkedList = NS.Deserialize<LinkedList<int>>(MS);
                Assert.AreEqual(ArrayResult.Length, LinkedListOfInt.Count);
                Assert.AreEqual(ArrayResult[0], LinkedListOfInt.First.Value);
                Assert.AreEqual(TypedLinkedList.Count, LinkedListOfInt.Count);
                Assert.AreEqual(TypedLinkedList.Last.Value, LinkedListOfInt.Last.Value);
                MS.Position = 0;
                MS.SetLength(0);


                var SortedSetOfObject = new SortedSet<int>(new int[] { 1, 2, 3, 5, 8, 13, 21 });
                NS.Serialize(MS, SortedSetOfObject);
                MS.Position = 0;
                ArrayResult = (int[])NS.Deserialize(MS);
                MS.Position = 0;
                var TypedSortedSet = NS.Deserialize<SortedSet<int>>(MS);
                var OriginalEnumerator = SortedSetOfObject.GetEnumerator();
                var DeserializedEnumerator = TypedSortedSet.GetEnumerator();
                OriginalEnumerator.MoveNext();
                DeserializedEnumerator.MoveNext();
                Assert.AreEqual(ArrayResult.Length, SortedSetOfObject.Count);
                Assert.AreEqual(ArrayResult[0], OriginalEnumerator.Current);
                OriginalEnumerator.MoveNext();
                DeserializedEnumerator.MoveNext();
                Assert.AreEqual(TypedSortedSet.Count, SortedSetOfObject.Count);
                Assert.AreEqual(DeserializedEnumerator.Current, OriginalEnumerator.Current);
                MS.Position = 0;
                MS.SetLength(0);


                var StringCollectionArray = new StringCollection();
                StringCollectionArray.Add("A");
                StringCollectionArray.Add("AA");
                StringCollectionArray.Add("B");
                NS.Serialize(MS, StringCollectionArray);
                MS.Position = 0;
                ArrayOfObjects = (object[])NS.Deserialize(MS);
                MS.Position = 0;
                var TypedStringCollection = NS.Deserialize<StringCollection>(MS);
                Assert.AreEqual(ArrayOfObjects.Length, StringCollectionArray.Count);
                Assert.AreEqual(ArrayOfObjects[0], StringCollectionArray[0]);
                Assert.AreEqual(TypedStringCollection.Count, StringCollectionArray.Count);
                Assert.AreEqual(TypedStringCollection[2], StringCollectionArray[2]);
                MS.Position = 0;
                MS.SetLength(0);


                #endregion

                #region "All other kind of dictionaries"

                var HashtableOfObject = new Hashtable() { { "A", "hello" }, { "B", 123 }, { 100, "1undered" } };
                NS.Serialize(MS, HashtableOfObject);
                MS.Position = 0;
                var DictionaryOfObjects = (Dictionary<object, object>)NS.Deserialize(MS);
                Assert.AreEqual(DictionaryOfObjects.Count, HashtableOfObject.Count);
                Assert.AreEqual(DictionaryOfObjects["B"], HashtableOfObject["B"]);
                MS.Position = 0;
                DictionaryOfObjects = NS.Deserialize<Dictionary<object, object>>(MS);
                Assert.AreEqual(DictionaryOfObjects.Count, HashtableOfObject.Count);
                Assert.AreEqual(DictionaryOfObjects[100], HashtableOfObject[100]);
                MS.Position = 0;
                MS.SetLength(0);


                var SortedListOfString = new SortedList() { { "A", "hello" }, { "B", 123 }, { "C", "1Hundered" }, { "D", true }, { "E", DateTime.Now } };
                NS.Serialize(MS, SortedListOfString);
                MS.Position = 0;
                DictionaryOfObjects = (Dictionary<object, object>)NS.Deserialize(MS);
                MS.Position = 0;
                var TypedSortedList = NS.Deserialize<SortedList>(MS);
                Assert.AreEqual(DictionaryOfObjects.Count, SortedListOfString.Count);
                Assert.AreEqual(DictionaryOfObjects["E"], SortedListOfString["E"]);
                Assert.AreEqual(DictionaryOfObjects["D"], true);
                Assert.AreEqual(TypedSortedList.Count, SortedListOfString.Count);
                Assert.AreEqual(TypedSortedList["A"], SortedListOfString["A"]);
                Assert.AreEqual(TypedSortedList["B"], 123);
                MS.Position = 0;
                MS.SetLength(0);


                var SortedDictionaryOfString = new SortedDictionary<string, object>() { { "A", "hello" }, { "B", 123 }, { "C", "1Hundered" }, { "D", true }, { "E", DateTime.Now } };
                NS.Serialize(MS, SortedDictionaryOfString);
                MS.Position = 0;
                var DictionaryOfString = (Dictionary<string, object>)NS.Deserialize(MS);
                MS.Position = 0;
                var TypedSortedDictionary = NS.Deserialize<SortedDictionary<string, object>>(MS);
                Assert.AreEqual(DictionaryOfString.Count, SortedDictionaryOfString.Count);
                Assert.AreEqual(DictionaryOfString["C"], SortedDictionaryOfString["C"]);
                Assert.IsInstanceOfType(DictionaryOfString["E"], typeof(DateTime));
                Assert.AreEqual(TypedSortedDictionary.Count, SortedDictionaryOfString.Count);
                Assert.AreEqual(TypedSortedDictionary["D"], SortedDictionaryOfString["D"]);
                Assert.AreEqual(TypedSortedDictionary["B"], 123);
                MS.Position = 0;
                MS.SetLength(0);

                var ListDictionaryOfObject = new ListDictionary() { { "A", "hello" }, { "B", 123 }, { 100, "1Hundered" }, { "D", true }, { false, DateTime.Now } };
                NS.Serialize(MS, ListDictionaryOfObject);
                MS.Position = 0;
                DictionaryOfObjects = (Dictionary<object, object>)NS.Deserialize(MS);
                MS.Position = 0;
                var TypedListDictionary = NS.Deserialize<ListDictionary>(MS);
                Assert.AreEqual(DictionaryOfObjects.Count, ListDictionaryOfObject.Count);
                Assert.IsInstanceOfType(DictionaryOfObjects[false], typeof(DateTime));
                Assert.AreEqual(TypedListDictionary.Count, ListDictionaryOfObject.Count);
                Assert.AreEqual(TypedListDictionary[100], ListDictionaryOfObject[100]);
                Assert.AreEqual(TypedListDictionary["D"], true);
                MS.Position = 0;
                MS.SetLength(0);


                //TODO Michael Sogos: Not yet supported the type NameValueCollection
                //var NameValueDictionary = new NameValueCollection() { { "A", "hello" }, { "B", "123" }, { "100", "1undered" } };
                //NS.Serialize(MS, NameValueDictionary);
                //MS.Position = 0;
                //var NameValueCollectionResult = (Dictionary<string, string>)NS.Deserialize(MS);
                //Assert.AreEqual(NameValueCollectionResult.Count, NameValueDictionary.Count);
                //Assert.AreEqual(NameValueCollectionResult["B"], NameValueDictionary["B"]);
                //MS.Position = 0;
                //var TypedNameValueCollection = NS.Deserialize<NameValueCollection>(MS);
                //Assert.AreEqual(TypedNameValueCollection.Count, NameValueDictionary.Count);
                //Assert.AreEqual(TypedNameValueCollection["100"], NameValueDictionary["100"]);
                //Assert.AreEqual(TypedNameValueCollection[1], NameValueDictionary[1]);
                //MS.Position = 0;
                //MS.SetLength(0);


                var OrderedDictionaryOfObject = new OrderedDictionary() { { "A", "hello" }, { "B", 123 }, { 100, "1Hundered" }, { "D", true }, { false, DateTime.Now } };
                NS.Serialize(MS, OrderedDictionaryOfObject);
                MS.Position = 0;
                DictionaryOfObjects = (Dictionary<object, object>)NS.Deserialize(MS);
                MS.Position = 0;
                var TypedOrderedDictionary = NS.Deserialize<OrderedDictionary>(MS);
                Assert.AreEqual(DictionaryOfObjects.Count, OrderedDictionaryOfObject.Count);
                Assert.AreEqual(DictionaryOfObjects[100], OrderedDictionaryOfObject[2]);
                Assert.AreEqual(DictionaryOfObjects["D"], true);
                Assert.AreEqual(TypedOrderedDictionary.Count, OrderedDictionaryOfObject.Count);
                Assert.AreEqual(TypedOrderedDictionary["A"], OrderedDictionaryOfObject["A"]);
                Assert.AreEqual(TypedOrderedDictionary["B"], 123);
                MS.Position = 0;
                MS.SetLength(0);

                //TODO Michael Sogos: Not yet supported the type StringDictionary
                //var StringDictionaryObject = new StringDictionary() { { "A", "hello" }, { "B", "123" }, { "C", "1Hundered" }, { "D", "true" }, { "E", DateTime.Now.ToString() } };
                //NS.Serialize(MS, StringDictionaryObject);
                //MS.Position = 0;
                //var DictionaryOfStringString = (Dictionary<object, object>)NS.Deserialize(MS);
                //MS.Position = 0;
                //var TypedStringDictionary = NS.Deserialize<StringDictionary>(MS);
                //Assert.AreEqual(DictionaryOfStringString.Count, StringDictionaryObject.Count);
                //Assert.AreEqual(DictionaryOfStringString["D"], StringDictionaryObject["D"]);
                //Assert.AreEqual(TypedStringDictionary.Count, StringDictionaryObject.Count);
                //Assert.AreEqual(TypedStringDictionary["E"], StringDictionaryObject["E"]);
                //MS.Position = 0;
                //MS.SetLength(0);

                #endregion

                #region "Collection containing complex object"
                var ListOfComplexObject = new List<MiniComplexObject>() {
                    new MiniComplexObject (){
                        IntegerValue =10,
                        BooleanValue=true,
                        StringValue="Test"
                    },
                    new MiniComplexObject (){
                        IntegerValue =100,
                        BooleanValue=false,
                        StringValue="Test2"
                    }
                };
                NS.Serialize(MS, ListOfComplexObject);
                MS.Position = 0;
                ArrayOfObjects = (object[])NS.Deserialize(MS);
                Assert.AreEqual(ArrayResult.Length, ListOfInt.Count);
                Assert.AreEqual(ArrayResult[4], ListOfInt[4]);
                MS.Position = 0;
                MS.SetLength(0);

                #endregion
            }
        }

        [TestMethod]
        public void TestComplexObjectSerialization()
        {
            var random = new Random();

            ComplexObject FirstObj = new ComplexObject();
            FirstObj.BoolValue = true;
            FirstObj.DateTimeOffsetValue = DateTimeOffset.Now;
            FirstObj.DateTimeOffsetValue.AddDays(-10);
            FirstObj.DateTimeValue = DateTime.Now;
            FirstObj.DateTimeValue.AddMonths(3);
            FirstObj.DecimalValue = random.Next() + 0.1234M;
            FirstObj.DoubleValue = random.Next() + 0.4321;
            FirstObj.GuidValue = Guid.NewGuid();
            FirstObj.IntValue = random.Next() * -1;
            FirstObj.LongValue = random.Next() * 10;
            FirstObj.ShortValue = (short)(short.MaxValue / random.Next(1, 10));
            FirstObj.SingleValue = random.Next() + 0.3322F;
            FirstObj.StringValue = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce ac sem rutrum, scelerisque ex aliquet, ornare lorem. Nunc elementum mattis commodo. Curabitur non sem accumsan, tincidunt nunc nec, laoreet nulla. Nullam a risus nisl. Curabitur blandit urna eget mi varius imperdiet. Aliquam mattis nulla id neque placerat, ut iaculis ante hendrerit. Cras diam lacus, rutrum sit amet ipsum at, finibus commodo nibh";
            FirstObj.TimespanValue = new TimeSpan(random.Next() * 10);
            FirstObj.UIntValue = (uint)random.Next();
            FirstObj.ULongValue = (ulong)(random.Next() * 10);
            FirstObj.UShortValue = (ushort)(ushort.MaxValue / random.Next(1, 10));

            NetworkSerializer NS = new NetworkSerializer();
            using (MemoryStream MS = new MemoryStream())
            {
                //NOT TYPED OBJECT               
                NS.Serialize(MS, FirstObj);
                MS.Position = 0;
                var NotTypedResult = NS.Deserialize(MS);
                Assert.IsInstanceOfType(NotTypedResult, typeof(ExpandoObject));
                var Result = NotTypedResult as IDictionary<string, object>;
                foreach (PropertyInfo Property in FirstObj.GetType().GetProperties())
                {
                    Assert.AreEqual(Property.GetValue(FirstObj), Result[Property.Name]);
                }

                //TYPED OBJECT
                MS.Position = 0;
                ComplexObject TypedResult = NS.Deserialize<ComplexObject>(MS);
                foreach (PropertyInfo Property in FirstObj.GetType().GetProperties())
                {
                    Assert.AreEqual(Property.GetValue(FirstObj), TypedResult.GetType().GetProperty(Property.Name).GetValue(TypedResult));
                }

                MS.Position = 0;
            }

        }

    }

    class ComplexObject
    {
        public bool BoolValue { get; set; }
        public short ShortValue { get; set; }
        public ushort UShortValue { get; set; }
        public int IntValue { get; set; }
        public uint UIntValue { get; set; }
        public long LongValue { get; set; }
        public ulong ULongValue { get; set; }
        public Single SingleValue { get; set; }
        public Double DoubleValue { get; set; }
        public String StringValue { get; set; }
        public Decimal DecimalValue { get; set; }
        public Guid GuidValue { get; set; }
        public TimeSpan TimespanValue { get; set; }
        public DateTimeOffset DateTimeOffsetValue { get; set; }
        public DateTime DateTimeValue { get; set; }
    }

    class MiniComplexObject
    {
        public int IntegerValue { get; set; }

        public string StringValue { get; set; }

        public bool BooleanValue { get; set; }
    }

}
