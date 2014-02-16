﻿using System.Collections.Generic;
using BigDataCollections.DistributedArray.Managers;

namespace BigDataCollections.DistributedArray.SupportClasses.BlockCollection
{
    /// <summary>
    /// It is layer over List(T) to have simple way to add new functional and change internal
    /// structure of block if it need to be done.
    /// </summary>
    public class Block<T> : List<T>
    {
        /// <summary>
        /// Create new instance of Block. There is DefaultBlockSize as capacity of it.
        /// </summary>
        public Block():base(DefaultValuesManager.DefaultBlockSize)
        {
            
        }
    }
}