﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BigDataCollections.DistributedArray.Managers;
using BigDataCollections.DistributedArray.SupportClasses;

namespace BigDataCollections
{
    /// <summary>
    /// DistributedArray(T) provides collection of elements of T type, with indices of the items ranging from 0 to one less
    /// than the count of items in the collection. DistributedArray(T) consist of blocks. Size of block more or equal 0 and less then
    /// MaxBlockSize. Because of architecture many actions(remove, insert, add) faster than their counterparts in List or LinedList, 
    /// but some operations might slower because of due to the nature of architecture and the overhead. 
    /// It makes no sense to use it with a small number of items(less than 1000), because in that case List and LinkedList
    /// most operations will be more efficient.
    /// </summary>
    /// <typeparam name="T">Type of array elements.</typeparam>
    public partial class DistributedArray<T> : IList<T>
    {
        //API
        /// <summary>
        /// Create a new instance of the DistributedArray(T) class that is empty 
        /// and has one empty block with DefaultBlockSize capacity.
        /// </summary>
        public DistributedArray() : this(new Collection<T>())
        {
        }
        /// <summary>
        /// Create a new instance of the DistributedArray(T) class using elements from specified collection.
        /// </summary>
        /// <param name="collection">Collection whitch use as base for new DistributedArray(T).
        /// The collection it self cannot be null, but it can contain elements that are null, if type T is a reference type.</param>
        public DistributedArray(ICollection<T> collection)
        {
            Initialize();
            //Add divided blocks by collection
            int collectionCount = collection.Count;

            if (collectionCount != 0)
            {
                _blocks.AddRange(_blocks.DivideIntoBlocks(collection));
            }
            else
            {
                _blocks.Add(new List<T>());
            }

            Count = collectionCount;
        }
        /// <summary>
        /// Add an object to the end of last block of the DistributedArray(T).
        /// </summary>
        public void Add(T value)
        {            
            int indexOfBlock = _blocks.Count - 1;
            if (_blocks[indexOfBlock].Count >= MaxBlockSize)
            {
                _blocks.Add(new List<T>(DefaultBlockSize));
                indexOfBlock++;
            }

            _blocks[indexOfBlock].Add(value);
            Count++;
        }
        /// <summary>
        /// Adds the elements of the specified collection to the end of the last block of DistributedArray(T)
        /// or if there is needed - create another block.
        /// </summary>
        /// <param name="collection">The collection whose elements should be added to the end of the DistributedArray(T).
        ///  The collection it self cannot benull, but it can contain elements that are null, if type T is a reference type. </param>
        public void AddRange(ICollection<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            //Transfer data to the last block while it is possible
            var lastBlockIndex = _blocks[_blocks.Count - 1];
            var emptySize = MaxBlockSize - lastBlockIndex.Count;
            var sizeToFill = 0;
            if (emptySize != 0)
            {
                sizeToFill = (emptySize > collection.Count) ? collection.Count : emptySize;
                var blocksToInsert = _blocks.DivideIntoBlocks(collection, 0, sizeToFill);
                //Transfer data
                var block = blocksToInsert.GetEnumerator();
                for (int i = 0; i < blocksToInsert.Count; i++)
                {
                    block.MoveNext();
                    InsertRange(Count, block.Current);
                }
            }
            //Transfer other data as new blocks
            var newBlocks = _blocks.DivideIntoBlocks(collection, sizeToFill);
            _blocks.AddRange(newBlocks);

            Count += newBlocks.Count;
        }
        /// <summary>
        /// Returns a read-only wrapper based on current DistributedArray(T).
        /// </summary>
        /// <returns>A ReadOnlyCollection(T) that acts as a read-only wrapper around the current DistributedArray(T).</returns>
        public ReadOnlyCollection<T> AsReadOnly()
        {
            return new ReadOnlyCollection<T>(this);
        }
        /// <summary>
        /// Searches the entire sorted DistributedArray(T) for an element using the default comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="item">The object to locate. The value can be null for reference types.</param>
        /// <returns>The zero-based index of item in the sorted DistributedArray(T), ifitem is found; otherwise,
        ///  a negative number that is the bitwise complement of the index of the next element that is larger than item or,
        ///  if there is no larger element, the bitwise complement of Count. </returns>
        public int BinarySearch(T item)
        {
            return BinarySearch(0, Count, item, Comparer<T>.Default);
        }
        /// <summary>
        /// Searches the entire sorted DistributedArray(T) for an element using the specified comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="item">The object to locate. The value can be null for reference types.</param>
        /// <param name="comparer">The IComparer(T) implementation to use when comparing elements.
        ///-or- 
        ///null to use the default comparer Comparer(T).Default.
        ///</param>
        /// <returns>The zero-based index of item in the sorted DistributedArray(T), ifitem is found; otherwise,
        ///  a negative number that is the bitwise complement of the index of the next element that is larger than item or,
        ///  if there is no larger element, the bitwise complement of Count. </returns>
        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return BinarySearch(0, Count, item, comparer);
        }
        /// <summary>
        /// Searches a range of elements in the sorted DistributedArray(T) for an element using the specified comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to search.</param>
        /// <param name="count">The length of the range to search.</param>
        /// <param name="item">The object to locate. The value can be null for reference types.</param>
        /// <param name="comparer">The IComparer(T) implementation to use when comparing elements, ornull to use the default comparer Comparer(T).Default.</param>
        /// <returns></returns>
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            if (!IsValidRange(index, count))
            {
                throw new ArgumentOutOfRangeException();
            }

            if (comparer == null)
            {
                comparer = Comparer<T>.Default;
            }

            int startIndex = index;
            int endIndex = index + count;
            T middleValue;

            while (startIndex <= endIndex)
            {
                int middlePosition = (startIndex + endIndex)/2;
                middleValue = this[middlePosition];
                //Compare
                int comparerResult = comparer.Compare(item, middleValue);
                switch (comparerResult)
                {
                    case -1:
                        endIndex = middlePosition - 1;
                        break;
                    case 0:
                        return middlePosition;
                    case 1:
                        startIndex = middlePosition + 1;
                        break;
                }
            }
            //If there is no such item specify the location where the element should be
            if (endIndex == -1) // if we need first element
            {
                return -1;
            }

            var enumerator = GetEnumerator();
            ((DistributedArrayEnumerator)enumerator).MoveToIndex(endIndex); // Move to start position
            var counter = 0;
            while (endIndex + counter != index + count && comparer.Compare(enumerator.Current, item) != 1)
            {
                enumerator.MoveNext();
                counter++;
            }
            return ~(endIndex + counter);
        }
        /// <summary>
        /// Removes all elements from the DistributedArray(T). If there is too many elements
        /// (at this moment if count of elements is more or equal than 64*MaxBlockSize) -
        /// force call of garbadge collector to delete all generations of garbage.
        /// </summary>
        /// <remarks>(!)If your change this documentation - please, dont fogret to change it
        /// in DistributedQueue(T) and DistributedStack(T) classes. </remarks>
        public void Clear()
        {
            _blocks.Clear();
            Count = 0;
        }
        /// <summary>
        /// Remove true if DistributedArray(T) contains value, otherwise return false.
        /// </summary>
        /// <param name="item">Data to be checked.</param>
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }
        /// <summary>
        /// Converts the elements in the current DistributedArray(T) to another type, and returns a list containing the converted elements.
        /// </summary>
        /// <typeparam name="TOutput">The type of the elements of the target array.</typeparam>
        /// <param name="converter">A Converter(TInput, TOutput) delegate that converts each element from one type to another type.</param>
        /// <returns>A DistributedArray(T) of the target type containing the converted elements from the current DistributedArray(T).</returns>
        public DistributedArray<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (converter == null)
            {
                throw new ArgumentNullException("converter");
            }

            var result = new DistributedArray<TOutput>();
            //Convert all blocks
            foreach (var block in _blocks)
            {
                result._blocks.Add(block.ConvertAll(converter));
            }
            return result;
        }
        /// <summary>
        /// Copies the entire DistributedArray(T) to a compatible one-dimensional array, starting at the beginning of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from DistributedArray(T).
        ///  The Array must have zero-based indexing.</param>
        public void CopyTo(T[] array)
        {
            CopyTo(0, array, 0, array.Length);
        }
        /// <summary>
        /// Copies the entire DistributedArray(T) to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from DistributedArray(T).
        ///  The Array must have zero-based indexing. </param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins. </param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(0, array, arrayIndex, array.Length - arrayIndex);
        }
        /// <summary>
        /// Copies a range of elements from the DistributedArray(T) to a compatible one-dimensional array,
        ///  starting at the specified index of the target array.
        /// </summary>
        /// <param name="index">The zero-based index in the source DistributedArray(T) at which copying begins.</param>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from DistributedArray(T). 
        /// The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (!IsValidIndex(index) || !ValidationManager.IsValidRange(array, arrayIndex, count))
            {
                throw new ArgumentOutOfRangeException();
            }

            var enumerator = GetEnumerator();
            ((DistributedArrayEnumerator)enumerator).MoveToIndex(index);

            //Transfer data
            for (int i = arrayIndex; i < arrayIndex + count; i++)
            {
                array[i] = enumerator.Current;

                if (!enumerator.MoveNext())
                {
                    break;
                }
            }
        }
        /// <summary>
        /// Determines whether the DistributedArray(T) contains elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The Predicate(T) delegate that defines the conditions of the elements to search for.</param>
        /// <returns>True if the DistributedArray(T) contains one or more elements that match the conditions defined by the specified predicate; otherwise false. </returns>
        public bool Exists(Predicate<T> match)
        {
            return _blocks.Any(block => block.Exists(match));
        }
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        ///  and returns the first occurrence within the entire DistributedArray(T).
        /// </summary>
        /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
        /// <returns>The first element that matches the conditions defined by the specified predicate, if found;
        ///  otherwise, the default value for type T. </returns>
        public T Find(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            foreach (var item in this)
            {
                if (match.Invoke(item))
                {
                    return item;
                }
            }
            //If there is not needed item
            return default(T);
        }
        /// <summary>
        /// Retrieves all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">The Predicate(T) delegate that defines the conditions of the elements to search for.</param>
        /// <returns>A DistributedArray(T) containing all the elements that match the conditions defined by the specified predicate,
        ///  if found; otherwise, an empty DistributedArray(T).</returns>
        public DistributedArray<T> FindAll(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            var result = new DistributedArray<T>();
            foreach (var item in this)
            {
                if (match.Invoke(item))
                {
                    result.Add(item);
                }
            }
            return result;
        }
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        ///  and returns the zero-based index of the first occurrence within the entire DistributedArray(T).
        /// </summary>
        /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by match, if found; otherwise, –1.</returns>
        public int FindIndex(Predicate<T> match)
        {
            return FindIndex(0, Count, match);
        }
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        ///  and returns the zero-based index of the first occurrence within the range of elements in the DistributedArray(T)
        ///  that extends from the specified index to the last element.
        /// </summary>
        /// <param name="index">The zero-based starting index of the search.</param>
        /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by match, if found; otherwise, –1. </returns>
        public int FindIndex(int index, Predicate<T> match)
        {
            return FindIndex(index, Count - index, match);
        }
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        ///  and returns the zero-based index of the first occurrence within the range of elements in the DistributedArray(T)
        ///  that starts at the specified index and contains the specified number of elements.
        /// </summary>
        /// <param name="index">The zero-based starting index of the search. </param>
        /// <param name="count">The number of elements in the section to search. </param>
        /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by match,
        ///  if found; otherwise, –1. </returns>
        public int FindIndex(int index, int count, Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            var range = MultyblockRange(index, count);

            for (int i = range.IndexOfStartBlock; i < range.IndexOfStartBlock + range.Count; i++)
            {
                var blockRange = range[i - range.IndexOfStartBlock];
                int findIndexResult = _blocks[i].FindIndex(blockRange.StartSubindex, blockRange.Count, match);

                if (findIndexResult != -1)
                {
                    return blockRange.CommonBlockStartIndex + findIndexResult;
                }
            }
            //If there is no needed value
            return -1;
        }
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        ///  and returns the last occurrence within the entire DistributedArray(T).
        /// </summary>
        /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
        /// <returns>The last element that matches the conditions defined by the specified predicate, if found; otherwise, the default value for type T.</returns>
        public T FindLast(Predicate<T> match)
        {
            for (int i = _blocks.Count - 1; i != 0; i--)
            {
                var block = _blocks[i];
                for (int j = block.Count - 1; j != 0; j--)
                {
                    if (match.Invoke(block[j]))
                    {
                        return block[j];
                    }
                }
            }
            //If there is no such item
            return default(T);
        }
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        ///  and returns the zero-based index of the last occurrence within the entire DistributedArray(T).
        /// </summary>
        /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the last occurrence of an element that matches the conditions defined by match, if found; otherwise, –1.</returns>
        public int FindLastIndex(Predicate<T> match)
        {
            return FindLastIndex(Count - 1, Count, match);
        }
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the last 
        /// occurrence within the range of elements in the DistributedArray(T) that extends from the first element to the specified index.
        /// </summary>
        /// <param name="index">The zero-based starting index of the backward search.</param>
        /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the last occurrence of an element that matches the conditions defined by match, if found; otherwise, –1.</returns>
        public int FindLastIndex(int index, Predicate<T> match)
        {
            return FindLastIndex(index, index + 1, match);
        }
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the 
        /// last occurrence within the range of elements in the DistributedArray(T) that contains the specified number of elements and ends at the specified index.
        /// </summary>
        /// <param name="index">The zero-based starting index of the backward search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="match">The Predicate(T) delegate that defines the conditions of the element to search for.</param>
        /// <returns></returns>
        public int FindLastIndex(int index, int count, Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentOutOfRangeException("match");
            }

            int indexOfStartBlock = IndexOfBlock(index - count + 1);
            int indexOfEndBlock = IndexOfBlock(index);

            //Find index of item
            for (int i = indexOfEndBlock; i >= indexOfStartBlock; i--)
            {
                var range = RevereseBlockRange(i, index, count);
                //Try to find it in current block
                int lastSubindex = _blocks[i].FindLastIndex(range.StartSubindex, range.Count, match);
                if (lastSubindex != -1)
                {
                    return BlockStartIndex(i) + lastSubindex;
                }
            }
            //If there is no needed value
            return -1;
        }
        /// <summary>
        /// Returns an enumerator that iterates through the DistributedArray(T).
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return new DistributedArrayEnumerator(this);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new DistributedArrayEnumerator(this);
        }
        /// <summary>
        /// Creates a shallow copy of a range of elements in the source DistributedArray(T).
        /// </summary>
        /// <param name="index">The zero-based DistributedArray(T) index at which the range starts.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <returns></returns>
        public DistributedArray<T> GetRange(int index, int count)
        {
            var range = MultyblockRange(index, count);
            var newArray = new DistributedArray<T>();
            for (int i = range.IndexOfStartBlock; i < range.IndexOfStartBlock + range.Count; i++)
            {
                var blockRange = range[i - range.IndexOfStartBlock];
                var block = _blocks[i];

                if (blockRange.Count != 0)
                {
                    if (block.Count == blockRange.Count)
                    {
                        newArray._blocks.Add(block);
                    }
                    else
                    {
                        newArray.AddRange(block.GetRange(blockRange.StartSubindex, blockRange.Count));
                    }
                }
            }

            return newArray;
        }
        /// <summary>
        /// If value conatins in DistributedArray(T) returns index of this value, otherwise return -1.
        /// </summary>
        /// <param name="item">Data, the location of which is necessary to calculate</param>
        /// <returns>The zero-based index of the first occurrence of item within the entire DistributedArray(T), if found; otherwise, –1.</returns>
        public int IndexOf(T item)
        {
            return IndexOf(item, 0, Count);
        }
        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first
        ///  occurrence within the range of elements in the DistributedArray(T) that extends from the 
        /// specified index to the last element.
        /// </summary>
        /// <param name="item">The object to locate in the DistributedArray(T). The value can benull for reference types.</param>
        /// <param name="index">The zero-based starting index of the search. 0 (zero) is valid in an empty list.</param>
        /// <returns>The zero-based index of the first occurrence of item within the range of elements 
        /// in the DistributedArray(T) that extends fromindex to the last element, if found; otherwise, –1.</returns>
        public int IndexOf(T item, int index)
        {
            return IndexOf(item, index, Count - index);
        }
        /// <summary>
        ///  Searches for the specified object and returns the zero-based index of the first occurrence
        ///  within the range of elements in the DistributedArray(T) that starts at the specified index
        ///  and contains the specified number of elements.
        /// </summary>
        /// <param name="item">The object to locate in the DistributedArray(T). The value can benull for reference types.</param>
        /// <param name="index">The zero-based starting index of the search. 0 (zero) is valid in an empty list. </param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <returns>The zero-based index of the first occurrence of item within the range of elements
        ///  in the DistributedArray(T) that starts atindex and contains count number of elements, if found; otherwise, –1. </returns>
        public int IndexOf(T item, int index, int count)
        {
            var range = MultyblockRange(index, count);

            for (int i = range.IndexOfStartBlock; i < range.IndexOfStartBlock + range.Count; i++)
            {
                var blockRange = range[i - range.IndexOfStartBlock];
                int indexOfResult = _blocks[i].IndexOf(item, blockRange.StartSubindex, blockRange.Count);

                if (indexOfResult != -1)
                {
                    return blockRange.CommonBlockStartIndex + indexOfResult;
                }
            }
            //If there is no needed value
            return -1;
        }
        /// <summary>
        /// Inserts an element into the DistributedArray(T) at the specified index.
        /// </summary>
        /// <param name="index">Index of DistibutedArray(T) where the value will be.</param>
        /// <param name="item">The data to be placed.</param>
        public void Insert(int index, T item)
        {
            if (index == Count)
            {
                Add(item);
                return;
            }

            int blockStartIndex;
            int indexOfBlock;
            IndexOfBlockAndBlockStartIndex(index, out indexOfBlock, out blockStartIndex);

            //Insert
            int blockSubindex = index - blockStartIndex;
            var block = _blocks[indexOfBlock];

            bool isMaxSize = (block.Count == MaxBlockSize);
            bool isStartIndex = (blockSubindex == 0);

            if (isMaxSize)
            {
                DivideBlockIfItIsTooBig(indexOfBlock);
                Insert(index, item);
                return;
            }

            //Try to add to the previous block
            if (isStartIndex)
            {
                //If there is need - add new block
                if (indexOfBlock == 0
                    || (indexOfBlock != 0 && _blocks[indexOfBlock - 1].Count == MaxBlockSize))
                {
                    _blocks.Insert(indexOfBlock, new List<T>(DefaultBlockSize));
                    indexOfBlock++;
                }
                _blocks[indexOfBlock - 1].Add(item);
            }
            else
            {
               _blocks[indexOfBlock].Insert(blockSubindex, item);
               DivideBlockIfItIsTooBig(indexOfBlock);
            }

            Count++;
        }
        /// <summary>
        /// Inserts the elements of a collection into the DistributedArray(T) at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the new elements should be inserted. </param>
        /// <param name="collection">The collection whose elements should be inserted into the DistributedArray(T).
        ///  The collection it self cannot be null, but it can contain elements that are null, if type T is a reference type. </param>
        public void InsertRange(int index, ICollection<T> collection)
        {
            //Validate of index and count check in IndexOfBlockAndBlockStartIndex

            int indexOfBlock;
            int blockStartIndex;
            //Determine indexOfBlock and blockStartIndex
            if (index == Count)
            {
                indexOfBlock = _blocks.Count - 1;
                blockStartIndex = Count - _blocks[indexOfBlock].Count;
            }
            else if (Count == 0 && index == 0) // If there is insertion in empty DistributedArray
            {
                indexOfBlock = 0;
                blockStartIndex = 0;
            }
            else
            {
                IndexOfBlockAndBlockStartIndex(index, out indexOfBlock, out blockStartIndex); // Default case
            }
            //Insert
            _blocks[indexOfBlock].InsertRange(index - blockStartIndex, collection);
            DivideBlockIfItIsTooBig(indexOfBlock);

            Count += collection.Count;
        }
        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last occurrence within the entire DistributedArray(T).
        /// </summary>
        /// <param name="item">The object to locate in the DistributedArray(T). The value can benull for reference types.</param>
        /// <returns>The zero-based index of the last occurrence of item within the entire the DistributedArray(T), if found; otherwise, –1.</returns>
        public int LastIndexOf(T item)
        {
            return LastIndexOf(item, Count - 1, Count);
        }
        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last occurrence
        /// within the range of elements in the DistributedArray(T) that extends from the first element to the specified index.
        /// </summary>
        /// <param name="item">The object to locate in the DistributedArray(T). The value can benull for reference types.</param>
        /// <param name="index">The zero-based starting index of the backward search.</param>
        /// <returns>The zero-based index of the last occurrence of item within the range of 
        /// elements in the DistributedArray(T) that extends from the first element toindex, if found; otherwise, –1. </returns>
        public int LastIndexOf(T item, int index)
        {
            return LastIndexOf(item, index, index + 1);
        }
        /// <summary>
        /// Searches for the specified object and returns the zero-based index
        ///  of the last occurrence within the range of elements in the DistributedArray(T) that contains the specified 
        /// number of elements and ends at the specified index.
        /// </summary>
        /// <param name="item">The object to locate in the DistributedArray(T). The value can benull for reference types.</param>
        /// <param name="index">The zero-based starting index of the backward search. </param>
        /// <param name="count">The number of elements in the section to search. </param>
        /// <returns>The zero-based index of the last occurrence of item within the range of elements in the DistributedArray(T)
        ///  that containscount number of elements and ends at index, if found; otherwise, –1. </returns>
        public int LastIndexOf(T item, int index, int count)
        {
            int indexOfStartBlock = IndexOfBlock(index - count + 1);
            int indexOfEndBlock = IndexOfBlock(index);

            for (int i = indexOfEndBlock; i >= indexOfStartBlock; i--)
            {
                var range = RevereseBlockRange(i, index, count);

                //Try to find it in current block
                int blockFidLastIndex = _blocks[i].LastIndexOf(item, range.StartSubindex, range.Count);
                if (blockFidLastIndex != -1)
                {
                    return BlockStartIndex(i) + blockFidLastIndex;
                }
            }
            //If there is no needed value
            return -1;
        }
        /// <summary>
        /// Removes the first occurrence of a specific object from the DistributedArray(T).
        /// </summary>
        /// <param name="item">The object to remove from the DistributedArray(T). The value can benull for reference types.</param>
        /// <returns>True if item is successfully removed; otherwise, false.
        ///  This method also returns false if item was not found in the DistributedArray(T).</returns>
        public bool Remove(T item)
        {
            for (int i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                int blockIndexOf = block.IndexOf(item);
                //If there is value in this block 
                if (blockIndexOf != -1)
                {
                    block.Remove(item);
                    //If there is empty block - remove it
                    if (block.Count == 0)
                    {
                        _blocks.RemoveAt(i);
                    }

                    Count--;
                    return true;
                }
            }
            //If there is not value in this distributed array
            return false;
        }
        /// <summary>
        /// Removes the element at the specified index of the DistributedArray(T).
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            //Validate of index check in IndexOfBlockAndBlockStartIndex
            //Calculate position of this element
            int indexOfBlock;
            int blockStartIndex;
            IndexOfBlockAndBlockStartIndex(index, out indexOfBlock, out blockStartIndex);
            //Remove
            _blocks[indexOfBlock].RemoveAt(index - blockStartIndex);
            //If there is empty block remove it
            if (_blocks[indexOfBlock].Count == 0)
            {
                _blocks.RemoveAt(indexOfBlock);
            }
            Count--;
        }
        /// <summary>
        /// Removes a range of elements from the DistributedArray(T).
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
        /// <param name="count">The number of elements to remove.</param>
        public void RemoveRange(int index, int count)
        {
            var range = MultyblockRange(index, count);
            int shift = 0;
            for (int i = range.IndexOfStartBlock; i < range.IndexOfStartBlock + range.Count; i++)
            {
                var blockRange = range[i - range.IndexOfStartBlock];
                var block = _blocks[i - shift];

                if (blockRange.Count != 0)
                {
                    if (block.Count == blockRange.Count)
                    {
                        _blocks.RemoveAt(i - shift);
                        shift++;
                    }
                    else
                    {
                        block.RemoveRange(blockRange.StartSubindex, blockRange.Count);
                    }
                }
            }

            Count -= count;
        }
        /// <summary>
        /// Reverses the order of the elements in the entire DistributedArray(T).
        /// </summary>
        public void Reverse()
        {
            foreach (var block in _blocks)
            {
                block.Reverse();
            }
            _blocks.Reverse();
        }
        /// <summary>
        /// Copies the elements of the DistributedArray(T) to a new array.
        /// </summary>
        /// <returns>An array containing copies of the elements of the DistributedArray(T).</returns>
        public T[] ToArray()
        {
            var array = new T[Count];
            int count = 0;
            //Transfer data
            foreach (var item in this)
            {
                array[count++] = item;
            }
            return array;
        }
        /// <summary>
        /// Sets the capacity of every block to the actual number of elements in it.
        /// </summary>
        public void TrimExcess()
        {
            foreach (var block in _blocks)
            {
                block.TrimExcess();
            }
        }

        //Debug functions
#if DEBUG
        public void Display()
        {
            foreach (var item in this)
            {
                Console.WriteLine(item.ToString());
            }
        }
        public void DisplayByBlocks()
        {
            foreach (var block in _blocks)
            {
                foreach (var item in block)
                {
                    Console.WriteLine(item.ToString());
                }
                Console.WriteLine();
            }
        }
#endif

        //Data
        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set. </param>
        public T this[int index]
        {
            get
            {
                //Check for exceptions in IndexOfBlockAndBlockStartIndex()
                int indexOfBlock;
                int blockStartIndex;
                IndexOfBlockAndBlockStartIndex(index, out indexOfBlock, out blockStartIndex);
                return _blocks[indexOfBlock][index - blockStartIndex];
            }
            set
            {
                //Check for exceptions in IndexOfBlockAndBlockStartIndex()
                int indexOfBlock;
                int blockStartIndex;
                IndexOfBlockAndBlockStartIndex(index, out indexOfBlock, out blockStartIndex);
                _blocks[indexOfBlock][index - blockStartIndex] = value;
            }
        }
        /// <summary>
        /// Get the number of elements actually contained in the DistributedArray(T).
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Count cant be less then 0!");
                }
                _count = value;
            }
        }
        /// <summary>
        /// Gets a value indicating whether the DistributedArray(T) is read-only.
        /// </summary>
        public bool IsReadOnly { get; private set; }
        /// <summary>
        /// Default size of one DistributedArray(T) block. 
        /// Because of the way memory allocation is most effective that it is a power of 2.
        /// </summary>
        public int DefaultBlockSize 
        {
            get
            {
                return _blocks.DefaultBlockSize;
            }
            set
            {
                _blocks.DefaultBlockSize = value;
            }
        }
        /// <summary>
        /// The size of any block never will be more than this number.
        /// Because of the way memory allocation is most effective that it is a power of 2.
        /// </summary>
        public int MaxBlockSize
        {
            get
            {
                return _blocks.MaxBlockSize;
            }
            set
            {
                _blocks.MaxBlockSize = value;
            }
        }
        /// <summary>
        /// It is main data container where we save information.
        /// It is cant be null. There is always one block even it is empty.
        /// </summary>
        private int _count;
        private Blocks<T> _blocks; 

        //Support functions
        /// <summary>
        /// Initialize fields of object in time of creating.
        /// </summary>
        private void Initialize()
        {
            _blocks = new Blocks<T>();
            IsReadOnly = false;
        }
        /// <summary>
        /// Check range of the the current DistributedArray(T) to valid.
        /// </summary>
        /// <param name="index">The zero-based starting index of range of the DistributedArray(T) to check.</param>
        /// <param name="count">The number of elements of the range to check.</param>
        /// <returns>Return true of range is valid, otherwise return false.</returns>
        public bool IsValidRange(int index, int count)
        {
            return ValidationManager.IsValidRange(this, index, count);
        }
        /// <summary>
        /// Check index to valid in the current DistributedArray(T).
        /// </summary>
        /// <param name="index">The zero-based starting index of the DistributedArray(T) element.</param>
        /// <returns>True if index is valid, otherwise return false.</returns>
        public bool IsValidIndex(int index)
        {
            return ValidationManager.IsValidIndex(this, index);
        }
        /// <summary>
        /// Check count to valid in the current DistributedArray(T).
        /// </summary>
        /// <param name="count">Count to check.</param>
        /// <returns>True if count is valid, otherwise return false.</returns>
        public bool IsValidCount(int count)
        {
            return ValidationManager.IsValidCount(this, count);
        }
        /// <summary>
        /// Calculate indexOfBlock and blockStartIndex by index. 
        /// </summary>
        /// <param name="index">Common index of element in DistributedArray(T). index = [0; Count).</param>
        /// <param name="indexOfBlock">Index of block where situate index. indexOfBlock = [0; _blocks.Count).</param>
        /// <param name="blockStartIndex">Common index, which is start index of _blocks[indexOfBlock].</param>
        private void IndexOfBlockAndBlockStartIndex(int index, out int indexOfBlock, out int blockStartIndex)
        {
            if (!IsValidIndex(index))
            {
                throw new ArgumentOutOfRangeException();
            }

            //Find needed block
            blockStartIndex = 0;
            indexOfBlock = 0;
            for (int i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                //If there is needed block
                if (index >= blockStartIndex && index < blockStartIndex + block.Count)
                {
                    indexOfBlock = i;
                    break;
                }

                blockStartIndex += block.Count;
            }
        }
        /// <summary>
        /// Calculate start zero-based common index of specified block.
        /// </summary>
        /// <param name="indexOfBlock">Index of block we want to get start index.</param>
        /// <returns>Start zero-based common index</returns>
        private int BlockStartIndex(int indexOfBlock)
        {
            int blockStartIndex = 0;
            for (int i = 0; i < indexOfBlock; i++)
            {
                blockStartIndex += _blocks[i].Count;
            }

            return blockStartIndex;
        }
        /// <summary>
        /// Calculate index of block witch containt element with specified zero-base index.
        /// </summary>
        /// <param name="index">Zero-base index of element situated in the block to find.</param>
        /// <returns>Index of block witch containt element with specified zero-base index.</returns>
        private int IndexOfBlock(int index)
        {
            int indexOfBlock, blockStartIndex;
            IndexOfBlockAndBlockStartIndex(index, out indexOfBlock, out blockStartIndex);

            return indexOfBlock;
        }
        /// <summary>
        /// Calculate a subrange in the needed block of range by index and count.
        /// </summary>
        /// <param name="indexOfBlock">Block where we need to calculate subrange.</param>
        /// <param name="index">Zero-based start index of miltyblock range.</param>
        /// <param name="count">Count of elements of multyblock range.</param>
        /// <returns>Subrange in the needed block of range by index and count.</returns>
        private BlockRange BlockRange(int indexOfBlock, int index, int count)
        {
            if (!IsValidRange(index, count) || indexOfBlock >= _blocks.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            var range = new BlockRange();

            var startIndex = index;
            var endIndex = index + count - 1;
            var blockStartIndex = BlockStartIndex(indexOfBlock);
            var blockEndIndex = blockStartIndex + _blocks[indexOfBlock].Count - 1;

            //If needed block is out
            if (endIndex < blockStartIndex || startIndex > blockEndIndex)
            {
                range.StartSubindex = 0;
                range.Count = 0;
            }
            else
            {
                //StartSubindex
                if (blockStartIndex > startIndex)
                {
                    range.StartSubindex = 0;
                }
                else
                {
                    range.StartSubindex = startIndex - blockStartIndex;
                }

                //Count
                if (blockEndIndex < endIndex)
                {
                    range.Count = blockEndIndex - (blockStartIndex + range.StartSubindex) + 1;
                }
                else
                {
                    range.Count = endIndex - (blockStartIndex + range.StartSubindex) + 1;
                }
            }

            return range;
        }
        private MultyblockRange MultyblockRange(int index, int count)
        {
            if (!IsValidRange(index, count))
            {
                throw new ArgumentOutOfRangeException();
            }

            var ranges = new List<BlockRange>();
            var currentStartIndex = 0;
            var currentEndIndex = -1;
            var endIndex = index + count - 1;

            int indexOfStartBlock = -1;

            //If user want to select empty block
            if (count == 0)
            {
                return new MultyblockRange(BlockStartIndex(IndexOfBlock(index)), new BlockRange[0]);
            }

            for (int i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                currentEndIndex += block.Count;

                //f ranges overlap
                if ((index <= currentStartIndex && currentStartIndex <= endIndex)
                    || (index <= currentEndIndex && currentEndIndex <= endIndex)
                    || (currentStartIndex <= index && endIndex <= currentEndIndex))
                {
                    int startSubindex = (index >= currentStartIndex) ? index - currentStartIndex : 0;
                    int rangeCount = (endIndex >= currentEndIndex) ? block.Count - startSubindex
                        : endIndex - currentStartIndex - startSubindex + 1;

                    if (rangeCount >= 0)
                    {
                        ranges.Add(new BlockRange(startSubindex, rangeCount, currentStartIndex));

                        //Calculate start block
                        if (indexOfStartBlock == -1)
                        {
                            indexOfStartBlock = i;
                        }
                    }
                }

                currentStartIndex += block.Count;
            }

            return new MultyblockRange(indexOfStartBlock, ranges.ToArray());
        }
        /// <summary>
        /// Calculate a reverse subrange in the needed block of range by index and count.
        /// Reverse range is range where index situated in the end of range.
        /// </summary>
        /// <param name="indexOfBlock">>Block where we need to calculate subrange.</param>
        /// <param name="index">Zero-based start index of miltyblock range.</param>
        /// <param name="count">Count of elements of multyblock range.</param>
        /// <returns></returns>
        private BlockRange RevereseBlockRange(int indexOfBlock, int index, int count)
        {
            var range = BlockRange(indexOfBlock, index - count + 1, count);
            var reverseRange = new BlockRange(range.StartSubindex + range.Count - 1, range.Count);

            return reverseRange;
        }
        /// <summary>
        /// If count of elements in specified block equal or bigger then MaxBlockCount - divide it.
        /// </summary>
        /// <param name="indexOfBlock">Index of specified block.</param>
        private void DivideBlockIfItIsTooBig(int indexOfBlock)
        {
            if (_blocks[indexOfBlock].Count >= MaxBlockSize)
            {
                var newBlocks = _blocks.DivideIntoBlocks(_blocks[indexOfBlock]);
                //Insert new blocks instead old block
                _blocks.RemoveAt(indexOfBlock);
                _blocks.InsertRange(indexOfBlock, newBlocks);
            }
        }
    }
}