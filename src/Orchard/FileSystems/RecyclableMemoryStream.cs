﻿// ---------------------------------------------------------------------
// Copyright (c) 2015 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ---------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Orchard.FileSystems //Internalize to avoid conflicts
{
#if !SL5
    using Events = RecyclableMemoryStreamManager.Events;
#endif

    public static class MemoryStreamFactory
    {
        public static bool UseRecyclableMemoryStream { get; set; }

#if !SL5
        public static RecyclableMemoryStreamManager RecyclableInstance = new RecyclableMemoryStreamManager();

        public static MemoryStream GetStream()
        {
            return UseRecyclableMemoryStream
                ? RecyclableInstance.GetStream()
                : new MemoryStream();
        }

        public static MemoryStream GetStream(int capacity)
        {
            return UseRecyclableMemoryStream
                ? RecyclableInstance.GetStream(typeof(MemoryStreamFactory).Name, capacity)
                : new MemoryStream(capacity);
        }

        public static MemoryStream GetStream(byte[] bytes)
        {
            return UseRecyclableMemoryStream
                ? RecyclableInstance.GetStream(typeof(MemoryStreamFactory).Name, bytes, 0, bytes.Length)
                : new MemoryStream(bytes);
        }

        public static MemoryStream GetStream(byte[] bytes, int index, int count)
        {
            return UseRecyclableMemoryStream
                ? RecyclableInstance.GetStream(typeof(MemoryStreamFactory).Name, bytes, index, count)
                : new MemoryStream(bytes, index, count);
        }
#else
        public static MemoryStream GetStream()
        {
            return new MemoryStream();
        }

        public static MemoryStream GetStream(int capacity)
        {
            return new MemoryStream(capacity);
        }

        public static MemoryStream GetStream(byte[] bytes)
        {
            return new MemoryStream(bytes);
        }

        public static MemoryStream GetStream(byte[] bytes, int index, int count)
        {
            return new MemoryStream(bytes, index, count);
        }
#endif
    }

#if !SL5
    /// <summary>
    /// Manages pools of RecyclableMemoryStream objects.
    /// </summary>
    /// <remarks>
    /// There are two pools managed in here. The small pool contains same-sized buffers that are handed to streams
    /// as they write more data.
    /// 
    /// For scenarios that need to call GetBuffer(), the large pool contains buffers of various sizes, all
    /// multiples of LargeBufferMultiple (1 MB by default). They are split by size to avoid overly-wasteful buffer
    /// usage. There should be far fewer 8 MB buffers than 1 MB buffers, for example.
    /// </remarks>
    public partial class RecyclableMemoryStreamManager
    {
        /// <summary>
        /// Generic delegate for handling events without any arguments.
        /// </summary>
        public delegate void EventHandler();

        /// <summary>
        /// Delegate for handling large buffer discard reports.
        /// </summary>
        /// <param name="reason">Reason the buffer was discarded.</param>
        public delegate void LargeBufferDiscardedEventHandler(Events.MemoryStreamDiscardReason reason);

        /// <summary>
        /// Delegate for handling reports of stream size when streams are allocated
        /// </summary>
        /// <param name="bytes">Bytes allocated.</param>
        public delegate void StreamLengthReportHandler(long bytes);

        /// <summary>
        /// Delegate for handling periodic reporting of memory use statistics.
        /// </summary>
        /// <param name="smallPoolInUseBytes">Bytes currently in use in the small pool.</param>
        /// <param name="smallPoolFreeBytes">Bytes currently free in the small pool.</param>
        /// <param name="largePoolInUseBytes">Bytes currently in use in the large pool.</param>
        /// <param name="largePoolFreeBytes">Bytes currently free in the large pool.</param>
        public delegate void UsageReportEventHandler(
            long smallPoolInUseBytes, long smallPoolFreeBytes, long largePoolInUseBytes, long largePoolFreeBytes);

        public const int DefaultBlockSize = 128 * 1024;
        public const int DefaultLargeBufferMultiple = 1024 * 1024;
        public const int DefaultMaximumBufferSize = 128 * 1024 * 1024;

        private readonly int blockSize;
        private readonly long[] largeBufferFreeSize;
        private readonly long[] largeBufferInUseSize;

        private readonly int largeBufferMultiple;

        /// <summary>
        /// pools[0] = 1x largeBufferMultiple buffers
        /// pools[1] = 2x largeBufferMultiple buffers
        /// etc., up to maximumBufferSize
        /// </summary>
        private readonly ConcurrentStack<byte[]>[] largePools;

        private readonly int maximumBufferSize;
        private readonly ConcurrentStack<byte[]> smallPool;

        private long smallPoolFreeSize;
        private long smallPoolInUseSize;

        /// <summary>
        /// Initializes the memory manager with the default block/buffer specifications.
        /// </summary>
        public RecyclableMemoryStreamManager()
            : this(DefaultBlockSize, DefaultLargeBufferMultiple, DefaultMaximumBufferSize)
        { }

        /// <summary>
        /// Initializes the memory manager with the given block requiredSize.
        /// </summary>
        /// <param name="blockSize">Size of each block that is pooled. Must be > 0.</param>
        /// <param name="largeBufferMultiple">Each large buffer will be a multiple of this value.</param>
        /// <param name="maximumBufferSize">Buffers larger than this are not pooled</param>
        /// <exception cref="ArgumentOutOfRangeException">blockSize is not a positive number, or largeBufferMultiple is not a positive number, or maximumBufferSize is less than blockSize.</exception>
        /// <exception cref="ArgumentException">maximumBufferSize is not a multiple of largeBufferMultiple</exception>
        public RecyclableMemoryStreamManager(int blockSize, int largeBufferMultiple, int maximumBufferSize)
        {
            if (blockSize <= 0)
            {
                throw new ArgumentOutOfRangeException("blockSize", blockSize, "blockSize must be a positive number");
            }

            if (largeBufferMultiple <= 0)
            {
                throw new ArgumentOutOfRangeException("largeBufferMultiple",
                                                      "largeBufferMultiple must be a positive number");
            }

            if (maximumBufferSize < blockSize)
            {
                throw new ArgumentOutOfRangeException("maximumBufferSize",
                                                      "maximumBufferSize must be at least blockSize");
            }

            this.blockSize = blockSize;
            this.largeBufferMultiple = largeBufferMultiple;
            this.maximumBufferSize = maximumBufferSize;

            if (!IsLargeBufferMultiple(maximumBufferSize))
            {
                throw new ArgumentException("maximumBufferSize is not a multiple of largeBufferMultiple",
                                            "maximumBufferSize");
            }

            smallPool = new ConcurrentStack<byte[]>();
            var numLargePools = maximumBufferSize / largeBufferMultiple;

            // +1 to store size of bytes in use that are too large to be pooled
            largeBufferInUseSize = new long[numLargePools + 1];
            largeBufferFreeSize = new long[numLargePools];

            largePools = new ConcurrentStack<byte[]>[numLargePools];

            for (var i = 0; i < largePools.Length; ++i)
            {
                largePools[i] = new ConcurrentStack<byte[]>();
            }

            Events.Write.MemoryStreamManagerInitialized(blockSize, largeBufferMultiple, maximumBufferSize);
        }

        /// <summary>
        /// The size of each block. It must be set at creation and cannot be changed.
        /// </summary>
        public int BlockSize
        {
            get { return blockSize; }
        }

        /// <summary>
        /// All buffers are multiples of this number. It must be set at creation and cannot be changed.
        /// </summary>
        public int LargeBufferMultiple
        {
            get { return largeBufferMultiple; }
        }

        /// <summary>
        /// Gets or sets the maximum buffer size.
        /// </summary>
        /// <remarks>Any buffer that is returned to the pool that is larger than this will be
        /// discarded and garbage collected.</remarks>
        public int MaximumBufferSize
        {
            get { return maximumBufferSize; }
        }

        /// <summary>
        /// Number of bytes in small pool not currently in use
        /// </summary>
        public long SmallPoolFreeSize
        {
            get { return smallPoolFreeSize; }
        }

        /// <summary>
        /// Number of bytes currently in use by stream from the small pool
        /// </summary>
        public long SmallPoolInUseSize
        {
            get { return smallPoolInUseSize; }
        }

        /// <summary>
        /// Number of bytes in large pool not currently in use
        /// </summary>
        public long LargePoolFreeSize
        {
            get { return largeBufferFreeSize.Sum(); }
        }

        /// <summary>
        /// Number of bytes currently in use by streams from the large pool
        /// </summary>
        public long LargePoolInUseSize
        {
            get { return largeBufferInUseSize.Sum(); }
        }

        /// <summary>
        /// How many blocks are in the small pool
        /// </summary>
        public long SmallBlocksFree
        {
            get { return smallPool.Count; }
        }

        /// <summary>
        /// How many buffers are in the large pool
        /// </summary>
        public long LargeBuffersFree
        {
            get
            {
                long free = 0;
                foreach (var pool in largePools)
                {
                    free += pool.Count;
                }
                return free;
            }
        }

        /// <summary>
        /// How many bytes of small free blocks to allow before we start dropping
        /// those returned to us.
        /// </summary>
        public long MaximumFreeSmallPoolBytes { get; set; }

        /// <summary>
        /// How many bytes of large free buffers to allow before we start dropping
        /// those returned to us.
        /// </summary>
        public long MaximumFreeLargePoolBytes { get; set; }

        /// <summary>
        /// Maximum stream capacity in bytes. Attempts to set a larger capacity will
        /// result in an exception.
        /// </summary>
        /// <remarks>A value of 0 indicates no limit.</remarks>
        public long MaximumStreamCapacity { get; set; }

        /// <summary>
        /// Whether to save callstacks for stream allocations. This can help in debugging.
        /// It should NEVER be turned on generally in production.
        /// </summary>
        public bool GenerateCallStacks { get; set; }

        /// <summary>
        /// Whether dirty buffers can be immediately returned to the buffer pool. E.g. when GetBuffer() is called on
        /// a stream and creates a single large buffer, if this setting is enabled, the other blocks will be returned
        /// to the buffer pool immediately.
        /// Note when enabling this setting that the user is responsible for ensuring that any buffer previously
        /// retrieved from a stream which is subsequently modified is not used after modification (as it may no longer
        /// be valid).
        /// </summary>
        public bool AggressiveBufferReturn { get; set; }

        /// <summary>
        /// Removes and returns a single block from the pool.
        /// </summary>
        /// <returns>A byte[] array</returns>
        internal byte[] GetBlock()
        {
            byte[] block;
            if (!smallPool.TryPop(out block))
            {
                // We'll add this back to the pool when the stream is disposed
                // (unless our free pool is too large)
                block = new byte[BlockSize];
                Events.Write.MemoryStreamNewBlockCreated(smallPoolInUseSize);

                BlockCreated?.Invoke();
            }
            else
            {
                Interlocked.Add(ref smallPoolFreeSize, -BlockSize);
            }

            Interlocked.Add(ref smallPoolInUseSize, BlockSize);
            return block;
        }

        /// <summary>
        /// Returns a buffer of arbitrary size from the large buffer pool. This buffer
        /// will be at least the requiredSize and always be a multiple of largeBufferMultiple.
        /// </summary>
        /// <param name="requiredSize">The minimum length of the buffer</param>
        /// <param name="tag">The tag of the stream returning this buffer, for logging if necessary.</param>
        /// <returns>A buffer of at least the required size.</returns>
        internal byte[] GetLargeBuffer(int requiredSize, string tag)
        {
            requiredSize = RoundToLargeBufferMultiple(requiredSize);

            var poolIndex = requiredSize / largeBufferMultiple - 1;

            byte[] buffer;
            if (poolIndex < largePools.Length)
            {
                if (!largePools[poolIndex].TryPop(out buffer))
                {
                    buffer = new byte[requiredSize];

                    Events.Write.MemoryStreamNewLargeBufferCreated(requiredSize, LargePoolInUseSize);
                    LargeBufferCreated?.Invoke();
                }
                else
                {
                    Interlocked.Add(ref largeBufferFreeSize[poolIndex], -buffer.Length);
                }
            }
            else
            {
                // Buffer is too large to pool. They get a new buffer.

                // We still want to track the size, though, and we've reserved a slot
                // in the end of the inuse array for nonpooled bytes in use.
                poolIndex = largeBufferInUseSize.Length - 1;

                // We still want to round up to reduce heap fragmentation.
                buffer = new byte[requiredSize];
                string callStack = null;
                if (GenerateCallStacks)
                {
                    // Grab the stack -- we want to know who requires such large buffers
                    callStack = PclExport.Instance.GetStackTrace();
                }
                Events.Write.MemoryStreamNonPooledLargeBufferCreated(requiredSize, tag, callStack);

                LargeBufferCreated?.Invoke();
            }

            Interlocked.Add(ref largeBufferInUseSize[poolIndex], buffer.Length);

            return buffer;
        }

        private int RoundToLargeBufferMultiple(int requiredSize)
        {
            return ((requiredSize + LargeBufferMultiple - 1) / LargeBufferMultiple) * LargeBufferMultiple;
        }

        private bool IsLargeBufferMultiple(int value)
        {
            return (value != 0) && (value % LargeBufferMultiple) == 0;
        }

        /// <summary>
        /// Returns the buffer to the large pool
        /// </summary>
        /// <param name="buffer">The buffer to return.</param>
        /// <param name="tag">The tag of the stream returning this buffer, for logging if necessary.</param>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ArgumentException">buffer.Length is not a multiple of LargeBufferMultiple (it did not originate from this pool)</exception>
        internal void ReturnLargeBuffer(byte[] buffer, string tag)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (!IsLargeBufferMultiple(buffer.Length))
            {
                throw new ArgumentException(
                    "buffer did not originate from this memory manager. The size is not a multiple of " +
                    LargeBufferMultiple);
            }

            var poolIndex = buffer.Length / largeBufferMultiple - 1;

            if (poolIndex < largePools.Length)
            {
                if ((largePools[poolIndex].Count + 1) * buffer.Length <= MaximumFreeLargePoolBytes ||
                    MaximumFreeLargePoolBytes == 0)
                {
                    largePools[poolIndex].Push(buffer);
                    Interlocked.Add(ref largeBufferFreeSize[poolIndex], buffer.Length);
                }
                else
                {
                    Events.Write.MemoryStreamDiscardBuffer(Events.MemoryStreamBufferType.Large, tag,
                                                           Events.MemoryStreamDiscardReason.EnoughFree);

                    LargeBufferDiscarded?.Invoke(Events.MemoryStreamDiscardReason.EnoughFree);
                }
            }
            else
            {
                // This is a non-poolable buffer, but we still want to track its size for inuse
                // analysis. We have space in the inuse array for 
                poolIndex = largeBufferInUseSize.Length - 1;

                Events.Write.MemoryStreamDiscardBuffer(Events.MemoryStreamBufferType.Large, tag,
                                                       Events.MemoryStreamDiscardReason.TooLarge);
                LargeBufferDiscarded?.Invoke(Events.MemoryStreamDiscardReason.TooLarge);
            }

            Interlocked.Add(ref largeBufferInUseSize[poolIndex], -buffer.Length);

            UsageReport?.Invoke(smallPoolInUseSize, smallPoolFreeSize, LargePoolInUseSize,
                 LargePoolFreeSize);
        }

        /// <summary>
        /// Returns the blocks to the pool
        /// </summary>
        /// <param name="blocks">Collection of blocks to return to the pool</param>
        /// <param name="tag">The tag of the stream returning these blocks, for logging if necessary.</param>
        /// <exception cref="ArgumentNullException">blocks is null</exception>
        /// <exception cref="ArgumentException">blocks contains buffers that are the wrong size (or null) for this memory manager</exception>
        internal void ReturnBlocks(ICollection<byte[]> blocks, string tag)
        {
            if (blocks == null)
            {
                throw new ArgumentNullException("blocks");
            }

            var bytesToReturn = blocks.Count * BlockSize;
            Interlocked.Add(ref smallPoolInUseSize, -bytesToReturn);

            foreach (var block in blocks)
            {
                if (block == null || block.Length != BlockSize)
                {
                    throw new ArgumentException("blocks contains buffers that are not BlockSize in length");
                }
            }

            foreach (var block in blocks)
            {
                if (MaximumFreeSmallPoolBytes == 0 || SmallPoolFreeSize < MaximumFreeSmallPoolBytes)
                {
                    Interlocked.Add(ref smallPoolFreeSize, BlockSize);
                    smallPool.Push(block);
                }
                else
                {
                    Events.Write.MemoryStreamDiscardBuffer(Events.MemoryStreamBufferType.Small, tag,
                                                           Events.MemoryStreamDiscardReason.EnoughFree);
                    BlockDiscarded?.Invoke();
                    break;
                }
            }

            UsageReport?.Invoke(smallPoolInUseSize, smallPoolFreeSize, LargePoolInUseSize,
                 LargePoolFreeSize);
        }

        internal void ReportStreamCreated()
        {
            StreamCreated?.Invoke();
        }

        internal void ReportStreamDisposed()
        {
            StreamDisposed?.Invoke();
        }

        internal void ReportStreamFinalized()
        {
            StreamFinalized?.Invoke();
        }

        internal void ReportStreamLength(long bytes)
        {
            StreamLength?.Invoke(bytes);
        }

        internal void ReportStreamToArray()
        {
            StreamConvertedToArray?.Invoke();
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with no tag and a default initial capacity.
        /// </summary>
        /// <returns>A MemoryStream.</returns>
        public MemoryStream GetStream()
        {
            return new RecyclableMemoryStream(this);
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and a default initial capacity.
        /// </summary>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <returns>A MemoryStream.</returns>
        public MemoryStream GetStream(string tag)
        {
            return new RecyclableMemoryStream(this, tag);
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and at least the given capacity.
        /// </summary>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <param name="requiredSize">The minimum desired capacity for the stream.</param>
        /// <returns>A MemoryStream.</returns>
        public MemoryStream GetStream(string tag, int requiredSize)
        {
            return new RecyclableMemoryStream(this, tag, requiredSize);
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and at least the given capacity, possibly using
        /// a single continugous underlying buffer.
        /// </summary>
        /// <remarks>Retrieving a MemoryStream which provides a single contiguous buffer can be useful in situations
        /// where the initial size is known and it is desirable to avoid copying data between the smaller underlying
        /// buffers to a single large one. This is most helpful when you know that you will always call GetBuffer
        /// on the underlying stream.</remarks>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <param name="requiredSize">The minimum desired capacity for the stream.</param>
        /// <param name="asContiguousBuffer">Whether to attempt to use a single contiguous buffer.</param>
        /// <returns>A MemoryStream.</returns>
        public MemoryStream GetStream(string tag, int requiredSize, bool asContiguousBuffer)
        {
            if (!asContiguousBuffer || requiredSize <= BlockSize)
            {
                return GetStream(tag, requiredSize);
            }

            return new RecyclableMemoryStream(this, tag, requiredSize, GetLargeBuffer(requiredSize, tag));
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and with contents copied from the provided
        /// buffer. The provided buffer is not wrapped or used after construction.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <param name="buffer">The byte buffer to copy data from.</param>
        /// <param name="offset">The offset from the start of the buffer to copy from.</param>
        /// <param name="count">The number of bytes to copy from the buffer.</param>
        /// <returns>A MemoryStream.</returns>
        //[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public MemoryStream GetStream(string tag, byte[] buffer, int offset, int count)
        {
            var stream = new RecyclableMemoryStream(this, tag, count);
            stream.Write(buffer, offset, count);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Triggered when a new block is created.
        /// </summary>
        public event EventHandler BlockCreated;

        /// <summary>
        /// Triggered when a new block is created.
        /// </summary>
        public event EventHandler BlockDiscarded;

        /// <summary>
        /// Triggered when a new large buffer is created.
        /// </summary>
        public event EventHandler LargeBufferCreated;

        /// <summary>
        /// Triggered when a new stream is created.
        /// </summary>
        public event EventHandler StreamCreated;

        /// <summary>
        /// Triggered when a stream is disposed.
        /// </summary>
        public event EventHandler StreamDisposed;

        /// <summary>
        /// Triggered when a stream is finalized.
        /// </summary>
        public event EventHandler StreamFinalized;

        /// <summary>
        /// Triggered when a stream is finalized.
        /// </summary>
        public event StreamLengthReportHandler StreamLength;

        /// <summary>
        /// Triggered when a user converts a stream to array.
        /// </summary>
        public event EventHandler StreamConvertedToArray;

        /// <summary>
        /// Triggered when a large buffer is discarded, along with the reason for the discard.
        /// </summary>
        public event LargeBufferDiscardedEventHandler LargeBufferDiscarded;

        /// <summary>
        /// Periodically triggered to report usage statistics.
        /// </summary>
        public event UsageReportEventHandler UsageReport;
    }


    /// <summary>
    /// MemoryStream implementation that deals with pooling and managing memory streams which use potentially large
    /// buffers.
    /// </summary>
    /// <remarks>
    /// This class works in tandem with the RecylableMemoryStreamManager to supply MemoryStream
    /// objects to callers, while avoiding these specific problems:
    /// 1. LOH allocations - since all large buffers are pooled, they will never incur a Gen2 GC
    /// 2. Memory waste - A standard memory stream doubles its size when it runs out of room. This
    /// leads to continual memory growth as each stream approaches the maximum allowed size.
    /// 3. Memory copying - Each time a MemoryStream grows, all the bytes are copied into new buffers.
    /// This implementation only copies the bytes when GetBuffer is called.
    /// 4. Memory fragmentation - By using homogeneous buffer sizes, it ensures that blocks of memory
    /// can be easily reused.
    /// 
    /// The stream is implemented on top of a series of uniformly-sized blocks. As the stream's length grows,
    /// additional blocks are retrieved from the memory manager. It is these blocks that are pooled, not the stream
    /// object itself.
    /// 
    /// The biggest wrinkle in this implementation is when GetBuffer() is called. This requires a single 
    /// contiguous buffer. If only a single block is in use, then that block is returned. If multiple blocks 
    /// are in use, we retrieve a larger buffer from the memory manager. These large buffers are also pooled, 
    /// split by size--they are multiples of a chunk size (1 MB by default).
    /// 
    /// Once a large buffer is assigned to the stream the blocks are NEVER again used for this stream. All operations take place on the 
    /// large buffer. The large buffer can be replaced by a larger buffer from the pool as needed. All blocks and large buffers 
    /// are maintained in the stream until the stream is disposed (unless AggressiveBufferReturn is enabled in the stream manager).
    /// 
    /// </remarks>
    public sealed class RecyclableMemoryStream : MemoryStream
    {
        private const long MaxStreamLength = Int32.MaxValue;

        /// <summary>
        /// All of these blocks must be the same size
        /// </summary>
        private readonly List<byte[]> blocks = new List<byte[]>(1);

        /// <summary>
        /// This is only set by GetBuffer() if the necessary buffer is larger than a single block size, or on
        /// construction if the caller immediately requests a single large buffer.
        /// </summary>
        /// <remarks>If this field is non-null, it contains the concatenation of the bytes found in the individual
        /// blocks. Once it is created, this (or a larger) largeBuffer will be used for the life of the stream.
        /// </remarks>
        private byte[] largeBuffer;

        /// <summary>
        /// This list is used to store buffers once they're replaced by something larger.
        /// This is for the cases where you have users of this class that may hold onto the buffers longer
        /// than they should and you want to prevent race conditions which could corrupt the data.
        /// </summary>
        private List<byte[]> dirtyBuffers;

        private readonly Guid id;
        /// <summary>
        /// Unique identifier for this stream across it's entire lifetime
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        internal Guid Id { get { CheckDisposed(); return id; } }

        private readonly string tag;
        /// <summary>
        /// A temporary identifier for the current usage of this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        internal string Tag { get { CheckDisposed(); return tag; } }

        private readonly RecyclableMemoryStreamManager memoryManager;

        /// <summary>
        /// Gets the memory manager being used by this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        internal RecyclableMemoryStreamManager MemoryManager
        {
            get
            {
                CheckDisposed();
                return memoryManager;
            }
        }

        private bool disposed;

        private readonly string allocationStack;
        private string disposeStack;

        /// <summary>
        /// Callstack of the constructor. It is only set if MemoryManager.GenerateCallStacks is true,
        /// which should only be in debugging situations.
        /// </summary>
        internal string AllocationStack { get { return allocationStack; } }

        /// <summary>
        /// Callstack of the Dispose call. It is only set if MemoryManager.GenerateCallStacks is true,
        /// which should only be in debugging situations.
        /// </summary>
        internal string DisposeStack { get { return disposeStack; } }

        /// <summary>
        /// This buffer exists so that WriteByte can forward all of its calls to Write
        /// without creating a new byte[] buffer on every call.
        /// </summary>
        private readonly byte[] byteBuffer = new byte[1];

        #region Constructors
        /// <summary>
        /// Allocate a new RecyclableMemoryStream object.
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        public RecyclableMemoryStream(RecyclableMemoryStreamManager memoryManager)
            : this(memoryManager, null, 0, null)
        {
        }

        /// <summary>
        /// Allocate a new RecyclableMemoryStream object
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        /// <param name="tag">A string identifying this stream for logging and debugging purposes</param>
        public RecyclableMemoryStream(RecyclableMemoryStreamManager memoryManager, string tag)
            : this(memoryManager, tag, 0, null)
        {
        }

        /// <summary>
        /// Allocate a new RecyclableMemoryStream object
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        /// <param name="tag">A string identifying this stream for logging and debugging purposes</param>
        /// <param name="requestedSize">The initial requested size to prevent future allocations</param>
        public RecyclableMemoryStream(RecyclableMemoryStreamManager memoryManager, string tag, int requestedSize)
            : this(memoryManager, tag, requestedSize, null)
        {
        }

        /// <summary>
        /// Allocate a new RecyclableMemoryStream object
        /// </summary>
        /// <param name="memoryManager">The memory manager</param>
        /// <param name="tag">A string identifying this stream for logging and debugging purposes</param>
        /// <param name="requestedSize">The initial requested size to prevent future allocations</param>
        /// <param name="initialLargeBuffer">An initial buffer to use. This buffer will be owned by the stream and returned to the memory manager upon Dispose.</param>
        internal RecyclableMemoryStream(RecyclableMemoryStreamManager memoryManager, string tag, int requestedSize,
                                      byte[] initialLargeBuffer)
        {
            this.memoryManager = memoryManager;
            id = Guid.NewGuid();
            this.tag = tag;

            if (requestedSize < memoryManager.BlockSize)
            {
                requestedSize = memoryManager.BlockSize;
            }

            if (initialLargeBuffer == null)
            {
                EnsureCapacity(requestedSize);
            }
            else
            {
                largeBuffer = initialLargeBuffer;
            }

            disposed = false;

            if (memoryManager.GenerateCallStacks)
            {
                allocationStack = PclExport.Instance.GetStackTrace();
            }

            Events.Write.MemoryStreamCreated(id, tag, requestedSize);
            memoryManager.ReportStreamCreated();
        }
        #endregion

        #region Dispose and Finalize
        ~RecyclableMemoryStream()
        {
            Dispose(false);
        }

        /// <summary>
        /// Returns the memory used by this stream back to the pool.
        /// </summary>
        /// <param name="disposing">Whether we're disposing (true), or being called by the finalizer (false)</param>
        /// <remarks>This method is not thread safe and it may not be called more than once.</remarks>
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly", Justification = "We have different disposal semantics, so SuppressFinalize is in a different spot.")]
        protected override void Dispose(bool disposing)
        {
            if (disposed)
            {
                string doubleDisposeStack = null;
                if (memoryManager.GenerateCallStacks)
                {
                    doubleDisposeStack = PclExport.Instance.GetStackTrace();
                }

                Events.Write.MemoryStreamDoubleDispose(id, tag, allocationStack, disposeStack, doubleDisposeStack);
                return;
            }

            Events.Write.MemoryStreamDisposed(id, tag);

            if (memoryManager.GenerateCallStacks)
            {
                disposeStack = PclExport.Instance.GetStackTrace();
            }

            if (disposing)
            {
                // Once this flag is set, we can't access any properties -- use fields directly
                disposed = true;

                memoryManager.ReportStreamDisposed();

                GC.SuppressFinalize(this);
            }
            else
            {
                // We're being finalized.

                Events.Write.MemoryStreamFinalized(id, tag, allocationStack);

#if !(PCL || NETSTANDARD1_1)
                if (AppDomain.CurrentDomain.IsFinalizingForUnload())
                {
                    // If we're being finalized because of a shutdown, don't go any further.
                    // We have no idea what's already been cleaned up. Triggering events may cause
                    // a crash.
                    base.Dispose(disposing);
                    return;
                }
#endif

                memoryManager.ReportStreamFinalized();
            }

            memoryManager.ReportStreamLength(length);

            if (largeBuffer != null)
            {
                memoryManager.ReturnLargeBuffer(largeBuffer, tag);
            }

            if (dirtyBuffers != null)
            {
                foreach (var buffer in dirtyBuffers)
                {
                    memoryManager.ReturnLargeBuffer(buffer, tag);
                }
            }

            memoryManager.ReturnBlocks(blocks, tag);

            base.Dispose(disposing);
        }

        /// <summary>
        /// Equivalent to Dispose
        /// </summary>
#if !(PCL || NETSTANDARD1_1)
        public override void Close()
#else
        public void Close()
#endif
        {
            Dispose(true);
        }
        #endregion

        #region MemoryStream overrides
        /// <summary>
        /// Gets or sets the capacity
        /// </summary>
        /// <remarks>Capacity is always in multiples of the memory manager's block size, unless
        /// the large buffer is in use.  Capacity never decreases during a stream's lifetime. 
        /// Explicitly setting the capacity to a lower value than the current value will have no effect. 
        /// This is because the buffers are all pooled by chunks and there's little reason to 
        /// allow stream truncation.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override int Capacity
        {
            get
            {
                CheckDisposed();
                if (largeBuffer != null)
                {
                    return largeBuffer.Length;
                }

                if (blocks.Count > 0)
                {
                    return blocks.Count * memoryManager.BlockSize;
                }
                return 0;
            }
            set
            {
                CheckDisposed();
                EnsureCapacity(value);
            }
        }

        private int length;

        /// <summary>
        /// Gets the number of bytes written to this stream.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override long Length
        {
            get
            {
                CheckDisposed();
                return length;
            }
        }

        private int position;

        /// <summary>
        /// Gets the current position in the stream
        /// </summary>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override long Position
        {
            get
            {
                CheckDisposed();
                return position;
            }
            set
            {
                CheckDisposed();
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "value must be non-negative");
                }

                if (value > MaxStreamLength)
                {
                    throw new ArgumentOutOfRangeException("value", "value cannot be more than " + MaxStreamLength);
                }

                position = (int)value;
            }
        }

        /// <summary>
        /// Whether the stream can currently read
        /// </summary>
        public override bool CanRead
        {
            get { return !disposed; }
        }

        /// <summary>
        /// Whether the stream can currently seek
        /// </summary>
        public override bool CanSeek
        {
            get { return !disposed; }
        }

        /// <summary>
        /// Always false
        /// </summary>
        public override bool CanTimeout
        {
            get { return false; }
        }

        /// <summary>
        /// Whether the stream can currently write
        /// </summary>
        public override bool CanWrite
        {
            get { return !disposed; }
        }

        /// <summary>
        /// Returns a single buffer containing the contents of the stream.
        /// The buffer may be longer than the stream length.
        /// </summary>
        /// <returns>A byte[] buffer</returns>
        /// <remarks>IMPORTANT: Doing a Write() after calling GetBuffer() invalidates the buffer. The old buffer is held onto
        /// until Dispose is called, but the next time GetBuffer() is called, a new buffer from the pool will be required.</remarks>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
#if !(PCL || NETSTANDARD1_1)
        public override byte[] GetBuffer()
#else
        public byte[] GetBuffer()
#endif
        {
            CheckDisposed();

            if (largeBuffer != null)
            {
                return largeBuffer;
            }

            if (blocks.Count == 1)
            {
                return blocks[0];
            }

            // Buffer needs to reflect the capacity, not the length, because
            // it's possible that people will manipulate the buffer directly
            // and set the length afterward. Capacity sets the expectation
            // for the size of the buffer.
            var newBuffer = memoryManager.GetLargeBuffer(Capacity, tag);

            // InternalRead will check for existence of largeBuffer, so make sure we
            // don't set it until after we've copied the data.
            InternalRead(newBuffer, 0, length, 0);
            largeBuffer = newBuffer;

            if (blocks.Count > 0 && memoryManager.AggressiveBufferReturn)
            {
                memoryManager.ReturnBlocks(blocks, tag);
                blocks.Clear();
            }

            return largeBuffer;
        }
        /// <summary>
        /// Reads from the current position into the provided buffer
        /// </summary>
        /// <param name="buffer">Destination buffer</param>
        /// <param name="offset">Offset into buffer at which to start placing the read bytes.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>The number of bytes read</returns>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset or count is less than 0</exception>
        /// <exception cref="ArgumentException">offset subtracted from the buffer length is less than count</exception>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", "offset cannot be negative");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", "count cannot be negative");
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("buffer length must be at least offset + count");
            }

            int amountRead = InternalRead(buffer, offset, count, position);
            position += amountRead;
            return amountRead;
        }

        /// <summary>
        /// Writes the buffer to the stream
        /// </summary>
        /// <param name="buffer">Source buffer</param>
        /// <param name="offset">Start position</param>
        /// <param name="count">Number of bytes to write</param>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset or count is negative</exception>
        /// <exception cref="ArgumentException">buffer.Length - offset is not less than count</exception>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", offset, "Offset must be in the range of 0 - buffer.Length-1");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", count, "count must be non-negative");
            }

            if (count + offset > buffer.Length)
            {
                throw new ArgumentException("count must be greater than buffer.Length - offset");
            }

            int blockSize = memoryManager.BlockSize;
            long end = (long)position + count;
            // Check for overflow
            if (end > MaxStreamLength)
            {
                throw new IOException("Maximum capacity exceeded");
            }

            long requiredBuffers = (end + blockSize - 1) / blockSize;

            if (requiredBuffers * blockSize > MaxStreamLength)
            {
                throw new IOException("Maximum capacity exceeded");
            }

            EnsureCapacity((int)end);

            if (largeBuffer == null)
            {
                int bytesRemaining = count;
                int bytesWritten = 0;
                var blockAndOffset = GetBlockAndRelativeOffset(position);

                while (bytesRemaining > 0)
                {
                    byte[] currentBlock = blocks[blockAndOffset.Block];
                    int remainingInBlock = blockSize - blockAndOffset.Offset;
                    int amountToWriteInBlock = Math.Min(remainingInBlock, bytesRemaining);

                    Buffer.BlockCopy(buffer, offset + bytesWritten, currentBlock, blockAndOffset.Offset, amountToWriteInBlock);

                    bytesRemaining -= amountToWriteInBlock;
                    bytesWritten += amountToWriteInBlock;

                    ++blockAndOffset.Block;
                    blockAndOffset.Offset = 0;
                }
            }
            else
            {
                Buffer.BlockCopy(buffer, offset, largeBuffer, position, count);
            }
            position = (int)end;
            length = Math.Max(position, length);
        }

        /// <summary>
        /// Returns a useful string for debugging. This should not normally be called in actual production code.
        /// </summary>
        public override string ToString()
        {
            return string.Format("Id = {0}, Tag = {1}, Length = {2:N0} bytes", Id, Tag, Length);
        }

        /// <summary>
        /// Writes a single byte to the current position in the stream.
        /// </summary>
        /// <param name="value">byte value to write</param>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override void WriteByte(byte value)
        {
            CheckDisposed();
            byteBuffer[0] = value;
            Write(byteBuffer, 0, 1);
        }

        /// <summary>
        /// Reads a single byte from the current position in the stream.
        /// </summary>
        /// <returns>The byte at the current position, or -1 if the position is at the end of the stream.</returns>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override int ReadByte()
        {
            CheckDisposed();
            if (position == length)
            {
                return -1;
            }
            byte value = 0;
            if (largeBuffer == null)
            {
                var blockAndOffset = GetBlockAndRelativeOffset(position);
                value = blocks[blockAndOffset.Block][blockAndOffset.Offset];
            }
            else
            {
                value = largeBuffer[position];
            }
            position++;
            return value;
        }

        /// <summary>
        /// Sets the length of the stream
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">value is negative or larger than MaxStreamLength</exception>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        public override void SetLength(long value)
        {
            CheckDisposed();
            if (value < 0 || value > MaxStreamLength)
            {
                throw new ArgumentOutOfRangeException("value", "value must be non-negative and at most " + MaxStreamLength);
            }

            EnsureCapacity((int)value);

            length = (int)value;
            if (position > value)
            {
                position = (int)value;
            }
        }

        /// <summary>
        /// Sets the position to the offset from the seek location
        /// </summary>
        /// <param name="offset">How many bytes to move</param>
        /// <param name="loc">From where</param>
        /// <returns>The new position</returns>
        /// <exception cref="ObjectDisposedException">Object has been disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">offset is larger than MaxStreamLength</exception>
        /// <exception cref="ArgumentException">Invalid seek origin</exception>
        /// <exception cref="IOException">Attempt to set negative position</exception>
        public override long Seek(long offset, SeekOrigin loc)
        {
            CheckDisposed();
            if (offset > MaxStreamLength)
            {
                throw new ArgumentOutOfRangeException("offset", "offset cannot be larger than " + MaxStreamLength);
            }

            int newPosition;
            switch (loc)
            {
                case SeekOrigin.Begin:
                    newPosition = (int)offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = (int)offset + position;
                    break;
                case SeekOrigin.End:
                    newPosition = (int)offset + length;
                    break;
                default:
                    throw new ArgumentException("Invalid seek origin", "loc");
            }
            if (newPosition < 0)
            {
                throw new IOException("Seek before beginning");
            }
            position = newPosition;
            return position;
        }

        /// <summary>
        /// Synchronously writes this stream's bytes to the parameter stream.
        /// </summary>
        /// <param name="stream">Destination stream</param>
        /// <remarks>Important: This does a synchronous write, which may not be desired in some situations</remarks>
        public override void WriteTo(Stream stream)
        {
            CheckDisposed();
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (largeBuffer == null)
            {
                int currentBlock = 0;
                int bytesRemaining = length;

                while (bytesRemaining > 0)
                {
                    int amountToCopy = Math.Min(blocks[currentBlock].Length, bytesRemaining);
                    stream.Write(blocks[currentBlock], 0, amountToCopy);

                    bytesRemaining -= amountToCopy;

                    ++currentBlock;
                }
            }
            else
            {
                stream.Write(largeBuffer, 0, length);
            }
        }
        #endregion

        #region Helper Methods
        private void CheckDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(string.Format("The stream with Id {0} and Tag {1} is disposed.", id, tag));
            }
        }

        private int InternalRead(byte[] buffer, int offset, int count, int fromPosition)
        {
            if (length - fromPosition <= 0)
            {
                return 0;
            }
            if (largeBuffer == null)
            {
                var blockAndOffset = GetBlockAndRelativeOffset(fromPosition);
                int bytesWritten = 0;
                int bytesRemaining = Math.Min(count, length - fromPosition);

                while (bytesRemaining > 0)
                {
                    int amountToCopy = Math.Min(blocks[blockAndOffset.Block].Length - blockAndOffset.Offset, bytesRemaining);
                    Buffer.BlockCopy(blocks[blockAndOffset.Block], blockAndOffset.Offset, buffer, bytesWritten + offset, amountToCopy);

                    bytesWritten += amountToCopy;
                    bytesRemaining -= amountToCopy;

                    ++blockAndOffset.Block;
                    blockAndOffset.Offset = 0;
                }
                return bytesWritten;
            }
            else
            {
                int amountToCopy = Math.Min(count, length - fromPosition);
                Buffer.BlockCopy(largeBuffer, fromPosition, buffer, offset, amountToCopy);
                return amountToCopy;
            }
        }

        private struct BlockAndOffset
        {
            public int Block;
            public int Offset;

            public BlockAndOffset(int block, int offset)
            {
                Block = block;
                Offset = offset;
            }
        }

        private BlockAndOffset GetBlockAndRelativeOffset(int offset)
        {
            var blockSize = memoryManager.BlockSize;
            return new BlockAndOffset(offset / blockSize, offset % blockSize);
        }

        private void EnsureCapacity(int newCapacity)
        {
            if (newCapacity > memoryManager.MaximumStreamCapacity && memoryManager.MaximumStreamCapacity > 0)
            {
                Events.Write.MemoryStreamOverCapacity(newCapacity, memoryManager.MaximumStreamCapacity, tag, allocationStack);
                throw new InvalidOperationException("Requested capacity is too large: " + newCapacity + ". Limit is " + memoryManager.MaximumStreamCapacity);
            }

            if (largeBuffer != null)
            {
                if (newCapacity > largeBuffer.Length)
                {
                    var newBuffer = memoryManager.GetLargeBuffer(newCapacity, tag);
                    InternalRead(newBuffer, 0, length, 0);
                    ReleaseLargeBuffer();
                    largeBuffer = newBuffer;
                }
            }
            else
            {
                while (Capacity < newCapacity)
                {
                    blocks.Add((memoryManager.GetBlock()));
                }
            }
        }

        /// <summary>
        /// Release the large buffer (either stores it for eventual release or returns it immediately).
        /// </summary>
        private void ReleaseLargeBuffer()
        {
            if (memoryManager.AggressiveBufferReturn)
            {
                memoryManager.ReturnLargeBuffer(largeBuffer, tag);
            }
            else
            {
                if (dirtyBuffers == null)
                {
                    // We most likely will only ever need space for one
                    dirtyBuffers = new List<byte[]>(1);
                }
                dirtyBuffers.Add(largeBuffer);
            }

            largeBuffer = null;
        }
        #endregion
    }

    //Avoid taking on an extra dep
    public sealed partial class RecyclableMemoryStreamManager
    {
        //[EventSource(Name = "Microsoft-IO-RecyclableMemoryStream", Guid = "{B80CD4E4-890E-468D-9CBA-90EB7C82DFC7}")]
        public sealed class Events// : EventSource
        {
            public static Events Write = new Events();

            public enum MemoryStreamBufferType
            {
                Small,
                Large
            }

            public enum MemoryStreamDiscardReason
            {
                TooLarge,
                EnoughFree
            }

            //[Event(1, Level = EventLevel.Verbose)]
            public void MemoryStreamCreated(Guid guid, string tag, int requestedSize)
            {
                //if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
                //{
                //    WriteEvent(1, guid, tag ?? string.Empty, requestedSize);
                //}
            }

            //[Event(2, Level = EventLevel.Verbose)]
            public void MemoryStreamDisposed(Guid guid, string tag)
            {
                //if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
                //{
                //    WriteEvent(2, guid, tag ?? string.Empty);
                //}
            }

            //[Event(3, Level = EventLevel.Critical)]
            public void MemoryStreamDoubleDispose(Guid guid, string tag, string allocationStack, string disposeStack1,
                                                  string disposeStack2)
            {
                //if (IsEnabled())
                //{
                //    WriteEvent(3, guid, tag ?? string.Empty, allocationStack ?? string.Empty,
                //                    disposeStack1 ?? string.Empty, disposeStack2 ?? string.Empty);
                //}
            }

            //[Event(4, Level = EventLevel.Error)]
            public void MemoryStreamFinalized(Guid guid, string tag, string allocationStack)
            {
                //if (IsEnabled())
                //{
                //    WriteEvent(4, guid, tag ?? string.Empty, allocationStack ?? string.Empty);
                //}
            }

            //[Event(5, Level = EventLevel.Verbose)]
            public void MemoryStreamToArray(Guid guid, string tag, string stack, int size)
            {
                //if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
                //{
                //    WriteEvent(5, guid, tag ?? string.Empty, stack ?? string.Empty, size);
                //}
            }

            //[Event(6, Level = EventLevel.Informational)]
            public void MemoryStreamManagerInitialized(int blockSize, int largeBufferMultiple, int maximumBufferSize)
            {
                //if (IsEnabled())
                //{
                //    WriteEvent(6, blockSize, largeBufferMultiple, maximumBufferSize);
                //}
            }

            //[Event(7, Level = EventLevel.Verbose)]
            public void MemoryStreamNewBlockCreated(long smallPoolInUseBytes)
            {
                //if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
                //{
                //    WriteEvent(7, smallPoolInUseBytes);
                //}
            }

            //[Event(8, Level = EventLevel.Verbose)]
            public void MemoryStreamNewLargeBufferCreated(int requiredSize, long largePoolInUseBytes)
            {
                //if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
                //{
                //    WriteEvent(8, requiredSize, largePoolInUseBytes);
                //}
            }

            //[Event(9, Level = EventLevel.Verbose)]
            public void MemoryStreamNonPooledLargeBufferCreated(int requiredSize, string tag, string allocationStack)
            {
                //if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
                //{
                //    WriteEvent(9, requiredSize, tag ?? string.Empty, allocationStack ?? string.Empty);
                //}
            }

            //[Event(10, Level = EventLevel.Warning)]
            public void MemoryStreamDiscardBuffer(MemoryStreamBufferType bufferType, string tag,
                                                  MemoryStreamDiscardReason reason)
            {
                //if (IsEnabled())
                //{
                //    WriteEvent(10, bufferType, tag ?? string.Empty, reason);
                //}
            }

            //[Event(11, Level = EventLevel.Error)]
            public void MemoryStreamOverCapacity(int requestedCapacity, long maxCapacity, string tag,
                                                 string allocationStack)
            {
                //if (IsEnabled())
                //{
                //    WriteEvent(11, requestedCapacity, maxCapacity, tag ?? string.Empty, allocationStack ?? string.Empty);
                //}
            }
        }
    }
#endif
}
