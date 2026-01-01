namespace Ionic.BZip2;

// BZip2InputStream.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2011 Dino Chiesa.
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License.
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// Last Saved: <2011-July-31 11:57:32>
//
// ------------------------------------------------------------------
//
// This module defines the BZip2InputStream class, which is a decompressing
// stream that handles BZIP2. This code is derived from Apache commons source code.
// The license below applies to the original Apache code.
//
// ------------------------------------------------------------------
/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
/*
 * This package is based on the work done by Keiron Liddle, Aftex Software
 * <keiron@aftexsw.com> to whom the Ant project is very grateful for his
 * great code.
 */
// compile: msbuild
// not: csc.exe /t:library /debug+ /out:dll BZip2InputStream.cs BCRC32.cs Rand.cs
    /// <summary>
    ///   A read-only decorator stream that performs BZip2 decompression on Read.
    /// </summary>
    public class BZip2InputStream : System.IO.Stream
    {
        bool _disposed;
    readonly bool _leaveOpen;
        Int64 totalBytesRead;
        private int last;
        /* for undoing the Burrows-Wheeler transform */
        private int origPtr;
        // blockSize100k: 0 .. 9.
        //
        // This var name is a misnomer. The actual block size is 100000
        // * blockSize100k. (not 100k * blocksize100k)
        private int blockSize100k;
        private bool blockRandomised;
        private int bsBuff;
        private int bsLive;
        private readonly Ionic.Zlib.CRC32 crc = new(true);
        private int inUseCount;
        private Stream input;
        private int currentChar = -1;
        /// <summary>
        ///   Compressor State
        /// </summary>
        // variables names: ok
        enum CState
        {
            EOF = 0,
            START_BLOCK = 1,
            RAND_PART_A = 2,
            RAND_PART_B = 3,
            RAND_PART_C = 4,
            NO_RAND_PART_A = 5,
            NO_RAND_PART_B = 6,
            NO_RAND_PART_C = 7,
        }
        private CState currentState = CState.START_BLOCK;
        private uint storedBlockCRC, storedCombinedCRC;
        private uint computedBlockCRC, computedCombinedCRC;
        // Variables used by setup* methods exclusively
        private int setupCount;
        private int setupChar2;
        private int setupPreviousChar;
        private int setupIndex2;
        private int setupJ2;
        private int setupRandomToGo;
        private int setupRandomPosition;
        private int setupTPosition;
        private char setupZ;
        private BZip2InputStream.DecompressionState data;
        /// <summary>
        ///   Create a BZip2InputStream, wrapping it around the given input Stream.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     The input stream will be closed when the BZip2InputStream is closed.
        ///   </para>
        /// </remarks>
        /// <param name='input'>The stream from which to read compressed data</param>
        public BZip2InputStream(Stream input)
            : this(input, false)
        {}
        /// <summary>
        ///   Create a BZip2InputStream with the given stream, and
        ///   specifying whether to leave the wrapped stream open when
        ///   the BZip2InputStream is closed.
        /// </summary>
        /// <param name='input'>The stream from which to read compressed data</param>
        /// <param name='leaveOpen'>
        ///   Whether to leave the input stream open, when the BZip2InputStream closes.
        /// </param>
        ///
        /// <example>
        ///
        ///   This example reads a bzip2-compressed file, decompresses it,
        ///   and writes the decompressed data into a newly created file.
        ///
        ///   <code>
        ///   var fname = "logfile.log.bz2";
        ///   using (var fs = File.OpenRead(fname))
        ///   {
        ///       using (var decompressor = new BZip2InputStream(fs))
        ///       {
        ///           var outFname = fname + ".decompressed";
        ///           using (var output = File.Create(outFname))
        ///           {
        ///               byte[] buffer = new byte[2048];
        ///               int n;
        ///               while ((n = decompressor.Read(buffer, 0, buffer.Length)) > 0)
        ///               {
        ///                   output.Write(buffer, 0, n);
        ///               }
        ///           }
        ///       }
        ///   }
        ///   </code>
        /// </example>
        public BZip2InputStream(Stream input, bool leaveOpen)
            : base()
        {
            this.input = input;
            this._leaveOpen = leaveOpen;
            init();
        }
        /// <summary>
        ///   Read data from the stream.
        /// </summary>
        ///
        /// <remarks>
        ///   <para>
        ///     To decompress a BZip2 data stream, create a <c>BZip2InputStream</c>,
        ///     providing a stream that reads compressed data.  Then call Read() on
        ///     that <c>BZip2InputStream</c>, and the data read will be decompressed
        ///     as you read.
        ///   </para>
        ///
        ///   <para>
        ///     A <c>BZip2InputStream</c> can be used only for <c>Read()</c>, not for <c>Write()</c>.
        ///   </para>
        /// </remarks>
        ///
        /// <param name="buffer">The buffer into which the read data should be placed.</param>
        /// <param name="offset">the offset within that data array to put the first byte read.</param>
        /// <param name="count">the number of bytes to read.</param>
        /// <returns>the number of bytes actually read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset < 0)
                throw new IndexOutOfRangeException(String.Format("offset ({0}) must be > 0", offset));
            if (count < 0)
                throw new IndexOutOfRangeException(String.Format("count ({0}) must be > 0", count));
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException(String.Format("offset({0}) count({1}) bLength({2})",
                                                                 offset, count, buffer.Length));
            if (this.input == null)
                throw new IOException("the stream is not open");
            int hi = offset + count;
            int destOffset = offset;
            for (int b; (destOffset < hi) && ((b = ReadByte()) >= 0);)
            {
                buffer[destOffset++] = (byte) b;
            }
            return destOffset - offset;
        }
        private void MakeMaps()
        {
            bool[] inUse = this.data.inUse;
            byte[] seqToUnseq = this.data.seqToUnseq;
            int sequenceIndex = 0;
            for (int i = 0; i < 256; i++)
            {
                if (inUse[i])
                    seqToUnseq[sequenceIndex++] = (byte) i;
            }
            this.inUseCount = sequenceIndex;
        }
        /// <summary>
        ///   Read a single byte from the stream.
        /// </summary>
        /// <returns>the byte read from the stream, or -1 if EOF</returns>
        public override int ReadByte()
        {
            int retChar = this.currentChar;
            totalBytesRead++;
            switch (this.currentState)
            {
                case CState.EOF:
                    return -1;
                case CState.START_BLOCK:
                    throw new IOException("bad state");
                case CState.RAND_PART_A:
                    throw new IOException("bad state");
                case CState.RAND_PART_B:
                    SetupRandPartB();
                    break;
                case CState.RAND_PART_C:
                    SetupRandPartC();
                    break;
                case CState.NO_RAND_PART_A:
                    throw new IOException("bad state");
                case CState.NO_RAND_PART_B:
                    SetupNoRandPartB();
                    break;
                case CState.NO_RAND_PART_C:
                    SetupNoRandPartC();
                    break;
                default:
                    throw new IOException("bad state");
            }
            return retChar;
        }
        /// <summary>
        /// Indicates whether the stream can be read.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports reading.
        /// </remarks>
        public override bool CanRead
        {
            get
            {
            return _disposed ? throw new ObjectDisposedException("BZip2Stream") : input.CanRead;
        }
    }
        /// <summary>
        /// Indicates whether the stream supports Seek operations.
        /// </summary>
        /// <remarks>
        /// Always returns false.
        /// </remarks>
        public override bool CanSeek
        {
            get { return false; }
        }
        /// <summary>
        /// Indicates whether the stream can be written.
        /// </summary>
        /// <remarks>
        /// The return value depends on whether the captive stream supports writing.
        /// </remarks>
        public override bool CanWrite
        {
            get
            {
            return _disposed ? throw new ObjectDisposedException("BZip2Stream") : input.CanWrite;
        }
    }
        /// <summary>
        /// Flush the stream.
        /// </summary>
        public override void Flush()
        {
            if (_disposed) throw new ObjectDisposedException("BZip2Stream");
            input.Flush();
        }
        /// <summary>
        /// Reading this property always throws a <see cref="NotImplementedException"/>.
        /// </summary>
        public override long Length
        {
            get { throw new NotImplementedException(); }
        }
        /// <summary>
        /// The position of the stream pointer.
        /// </summary>
        ///
        /// <remarks>
        ///   Setting this property always throws a <see
        ///   cref="NotImplementedException"/>. Reading will return the
        ///   total number of uncompressed bytes read in.
        /// </remarks>
        public override long Position
        {
            get
            {
                return this.totalBytesRead;
            }
            set { throw new NotImplementedException(); }
        }
    /// <summary>
    /// Calling this method always throws a <see cref="NotImplementedException"/>.
    /// </summary>
    /// <param name="offset">this is irrelevant, since it will always throw!</param>
    /// <param name="origin">this is irrelevant, since it will always throw!</param>
    /// <returns>irrelevant!</returns>
    public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new NotImplementedException();
    /// <summary>
    /// Calling this method always throws a <see cref="NotImplementedException"/>.
    /// </summary>
    /// <param name="value">this is irrelevant, since it will always throw!</param>
    public override void SetLength(long value) => throw new NotImplementedException();
    /// <summary>
    ///   Calling this method always throws a <see cref="NotImplementedException"/>.
    /// </summary>
    /// <param name='buffer'>this parameter is never used</param>
    /// <param name='offset'>this parameter is never used</param>
    /// <param name='count'>this parameter is never used</param>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    /// <summary>
    ///   Dispose the stream.
    /// </summary>
    /// <param name="disposing">
    ///   indicates whether the Dispose method was invoked by user code.
    /// </param>
    protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_disposed)
                {
                    if (disposing && (this.input != null))
                        this.input.Close();
                    _disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
        void init()
        {
            if (null == this.input)
                throw new IOException("No input Stream");
            if (!this.input.CanRead)
                throw new IOException("Unreadable input Stream");
            CheckMagicChar('B', 0);
            CheckMagicChar('Z', 1);
            CheckMagicChar('h', 2);
            int blockSize = this.input.ReadByte();
            if ((blockSize < '1') || (blockSize > '9'))
                throw new IOException("Stream is not BZip2 formatted: illegal "
                                      + "blocksize " + (char) blockSize);
            this.blockSize100k = blockSize - '0';
            InitBlock();
            SetupBlock();
        }
        void CheckMagicChar(char expected, int position)
        {
            int magic = this.input.ReadByte();
            if (magic != (int)expected)
            {
                var msg = String.Format("Not a valid BZip2 stream. byte {0}, expected '{1}', got '{2}'",
                                        position, (int)expected, magic);
                throw new IOException(msg);
            }
        }
        void InitBlock()
        {
            char magic0 = bsGetUByte();
            char magic1 = bsGetUByte();
            char magic2 = bsGetUByte();
            char magic3 = bsGetUByte();
            char magic4 = bsGetUByte();
            char magic5 = bsGetUByte();
            if (magic0 == 0x17 && magic1 == 0x72 && magic2 == 0x45
                && magic3 == 0x38 && magic4 == 0x50 && magic5 == 0x90)
            {
                complete(); // end of file
            }
            else if (magic0 != 0x31 ||
                     magic1 != 0x41 ||
                     magic2 != 0x59 ||
                     magic3 != 0x26 ||
                     magic4 != 0x53 ||
                     magic5 != 0x59)
            {
                this.currentState = CState.EOF;
                var msg = String.Format("bad block header at offset 0x{0:X}",
                                      this.input.Position);
                throw new IOException(msg);
            }
            else
            {
                this.storedBlockCRC = bsGetInt();
                // Console.WriteLine(" stored block CRC     : {0:X8}", this.storedBlockCRC);
                this.blockRandomised = (GetBits(1) == 1);
                // Lazily allocate data
                if (this.data == null)
                    this.data = new DecompressionState(this.blockSize100k);
                // currBlockNo++;
                getAndMoveToFrontDecode();
                this.crc.Reset();
                this.currentState = CState.START_BLOCK;
            }
        }
        private void EndBlock()
        {
            this.computedBlockCRC = (uint)this.crc.Crc32Result;
            // A bad CRC is considered a fatal error.
            if (this.storedBlockCRC != this.computedBlockCRC)
            {
                // make next blocks readable without error
                // (repair feature, not yet documented, not tested)
                // this.computedCombinedCRC = (this.storedCombinedCRC << 1)
                //     | (this.storedCombinedCRC >> 31);
                // this.computedCombinedCRC ^= this.storedBlockCRC;
                var msg = String.Format("BZip2 CRC error (expected {0:X8}, computed {1:X8})",
                                        this.storedBlockCRC, this.computedBlockCRC);
                throw new IOException(msg);
            }
            // Console.WriteLine(" combined CRC (before): {0:X8}", this.computedCombinedCRC);
            this.computedCombinedCRC = (this.computedCombinedCRC << 1)
                | (this.computedCombinedCRC >> 31);
            this.computedCombinedCRC ^= this.computedBlockCRC;
            // Console.WriteLine(" computed block  CRC  : {0:X8}", this.computedBlockCRC);
            // Console.WriteLine(" combined CRC (after) : {0:X8}", this.computedCombinedCRC);
            // Console.WriteLine();
        }
        private void complete()
        {
            this.storedCombinedCRC = bsGetInt();
            this.currentState = CState.EOF;
            this.data = null;
            if (this.storedCombinedCRC != this.computedCombinedCRC)
            {
                var msg = String.Format("BZip2 CRC error (expected {0:X8}, computed {1:X8})",
                                      this.storedCombinedCRC, this.computedCombinedCRC);
                throw new IOException(msg);
            }
        }
        /// <summary>
        ///   Close the stream.
        /// </summary>
        public override void Close()
        {
            Stream inShadow = this.input;
            if (inShadow != null)
            {
                try
                {
                    if (!this._leaveOpen)
                        inShadow.Close();
                }
                finally
                {
                    this.data = null;
                    this.input = null;
                }
            }
        }
        /// <summary>
        ///   Read bitCount bits from input, right justifying the result.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     For example, if you read 1 bit, the result is either 0
        ///     or 1.
        ///   </para>
        /// </remarks>
        /// <param name ="bitCount">
        ///   The number of bits to read, always between 1 and 32.
        /// </param>
        private int GetBits(int bitCount)
        {
            int bsLiveShadow = this.bsLive;
            int bsBuffShadow = this.bsBuff;
            if (bsLiveShadow < bitCount)
            {
                do
                {
                    int byteValue = this.input.ReadByte();
                    if (byteValue < 0)
                        throw new IOException("unexpected end of stream");
                    // Console.WriteLine("R {0:X2}", byteValue);
                    bsBuffShadow = (bsBuffShadow << 8) | byteValue;
                    bsLiveShadow += 8;
                } while (bsLiveShadow < bitCount);
                this.bsBuff = bsBuffShadow;
            }
            this.bsLive = bsLiveShadow - bitCount;
            return (bsBuffShadow >> (bsLiveShadow - bitCount)) & ((1 << bitCount) - 1);
        }
        // private bool bsGetBit()
        // {
        //     int bsLiveShadow = this.bsLive;
        //     int bsBuffShadow = this.bsBuff;
        //
        //     if (bsLiveShadow < 1)
        //     {
        //         int thech = this.input.ReadByte();
        //
        //         if (thech < 0)
        //         {
        //             throw new IOException("unexpected end of stream");
        //         }
        //
        //         bsBuffShadow = (bsBuffShadow << 8) | thech;
        //         bsLiveShadow += 8;
        //         this.bsBuff = bsBuffShadow;
        //     }
        //
        //     this.bsLive = bsLiveShadow - 1;
        //     return ((bsBuffShadow >> (bsLiveShadow - 1)) & 1) != 0;
        // }
        private bool bsGetBit()
        {
            int bit = GetBits(1);
            return bit != 0;
        }
    private char bsGetUByte() => (char)GetBits(8);
    private uint bsGetInt() => (uint)((((((GetBits(8) << 8) | GetBits(8)) << 8) | GetBits(8)) << 8) | GetBits(8));
    /**
     * Called by createHuffmanDecodingTables() exclusively.
     */
    private static void hbCreateDecodeTables(int[] limit,
                                                 int[] baseArray, int[] perm,  char[] length,
                                                 int minLen, int maxLen, int alphaSize)
        {
            for (int i = minLen, permPosition = 0; i <= maxLen; i++)
            {
                for (int j = 0; j < alphaSize; j++)
                {
                    if (length[j] == i)
                    {
                        perm[permPosition++] = j;
                    }
                }
            }
            for (int i = BZip2.MaxCodeLength; --i > 0;)
            {
                baseArray[i] = 0;
                limit[i] = 0;
            }
            for (int i = 0; i < alphaSize; i++)
            {
                baseArray[length[i] + 1]++;
            }
            for (int i = 1, baseValue = baseArray[0]; i < BZip2.MaxCodeLength; i++)
            {
                baseValue += baseArray[i];
                baseArray[i] = baseValue;
            }
            for (int i = minLen, vectorValue = 0, baseValue =  baseArray[i]; i <= maxLen; i++)
            {
                int nextBase = baseArray[i + 1];
                vectorValue += nextBase - baseValue;
                baseValue = nextBase;
                limit[i] = vectorValue - 1;
                vectorValue <<= 1;
            }
            for (int i = minLen + 1; i <= maxLen; i++)
            {
                baseArray[i] = ((limit[i - 1] + 1) << 1) - baseArray[i];
            }
        }
        private void recvDecodingTables()
        {
            var state = this.data;
            bool[] inUse = state.inUse;
            byte[] pos = state.recvDecodingTables_pos;
            //byte[] selector = state.selector;
            int inUse16 = 0;
            /* Receive the mapping table */
            for (int i = 0; i < 16; i++)
            {
                if (bsGetBit())
                {
                    inUse16 |= 1 << i;
                }
            }
            for (int i = 256; --i >= 0;)
            {
                inUse[i] = false;
            }
            for (int i = 0; i < 16; i++)
            {
                if ((inUse16 & (1 << i)) != 0)
                {
                    int i16 = i << 4;
                    for (int j = 0; j < 16; j++)
                    {
                        if (bsGetBit())
                        {
                            inUse[i16 + j] = true;
                        }
                    }
                }
            }
            MakeMaps();
            int alphaSize = this.inUseCount + 2;
            /* Now the selectors */
            int nGroups = GetBits(3);
            int nSelectors = GetBits(15);
            for (int i = 0; i < nSelectors; i++)
            {
                int j = 0;
                while (bsGetBit())
                {
                    j++;
                }
                state.selectorMtf[i] = (byte) j;
            }
            /* Undo the MTF values for the selectors. */
            for (int valueIndex = nGroups; --valueIndex >= 0;)
            {
                pos[valueIndex] = (byte) valueIndex;
            }
            for (int i = 0; i < nSelectors; i++)
            {
                int valueIndex = state.selectorMtf[i];
                byte selectedValue = pos[valueIndex];
                while (valueIndex > 0)
                {
                    // nearly all times valueIndex is zero, 4 in most other cases
                    pos[valueIndex] = pos[valueIndex - 1];
                    valueIndex--;
                }
                pos[0] = selectedValue;
                state.selector[i] = selectedValue;
            }
            char[][] len = state.temp_charArray2d;
            /* Now the coding tables */
            for (int tableIndex = 0; tableIndex < nGroups; tableIndex++)
            {
                int curr = GetBits(5);
                char[] len_t = len[tableIndex];
                for (int i = 0; i < alphaSize; i++)
                {
                    while (bsGetBit())
                    {
                        curr += bsGetBit() ? -1 : 1;
                    }
                    len_t[i] = (char) curr;
                }
            }
            // finally create the Huffman tables
            createHuffmanDecodingTables(alphaSize, nGroups);
        }
        /**
         * Called by recvDecodingTables() exclusively.
         */
        private void createHuffmanDecodingTables(int alphaSize,
                                                 int nGroups)
        {
            var state = this.data;
            char[][] len = state.temp_charArray2d;
            for (int tableIndex = 0; tableIndex < nGroups; tableIndex++)
            {
                int minLen = 32;
                int maxLen = 0;
                char[] len_t = len[tableIndex];
                for (int i = alphaSize; --i >= 0;)
                {
                    char lent = len_t[i];
                    if (lent > maxLen)
                        maxLen = lent;
                    if (lent < minLen)
                        minLen = lent;
                }
                hbCreateDecodeTables(state.gLimit[tableIndex], state.gBase[tableIndex], state.gPerm[tableIndex], len[tableIndex], minLen,
                                     maxLen, alphaSize);
                state.gMinlen[tableIndex] = minLen;
            }
        }
        private void getAndMoveToFrontDecode()
        {
            var state = this.data;
            this.origPtr = GetBits(24);
            if (this.origPtr < 0)
                throw new IOException("BZ_DATA_ERROR");
            if (this.origPtr > 10 + BZip2.BlockSizeMultiple * this.blockSize100k)
                throw new IOException("BZ_DATA_ERROR");
            recvDecodingTables();
            byte[] yy = state.getAndMoveToFrontDecode_yy;
            int limitLast = this.blockSize100k * BZip2.BlockSizeMultiple;
            /*
             * Setting up the unzftab entries here is not strictly necessary, but it
             * does save having to do it later in a separate pass, and so saves a
             * block's worth of cache misses.
             */
            for (int i = 256; --i >= 0;)
            {
                yy[i] = (byte) i;
                state.unzftab[i] = 0;
            }
            int groupNo = 0;
            int groupPos = BZip2.G_SIZE - 1;
            int endOfBlock = this.inUseCount + 1;
            int nextSym = getAndMoveToFrontDecode0(0);
            int bsBuffShadow = this.bsBuff;
            int bsLiveShadow = this.bsLive;
            int lastShadow = -1;
            int tableIndex = state.selector[groupNo] & 0xff;
            int[] base_tableIndex = state.gBase[tableIndex];
            int[] limit_tableIndex = state.gLimit[tableIndex];
            int[] perm_tableIndex = state.gPerm[tableIndex];
            int minLens_tableIndex = state.gMinlen[tableIndex];
            while (nextSym != endOfBlock)
            {
                if ((nextSym == BZip2.RUNA) || (nextSym == BZip2.RUNB))
                {
                    int runLength = -1;
                    for (int multiplier = 1; true; multiplier <<= 1)
                    {
                        if (nextSym == BZip2.RUNA)
                        {
                            runLength += multiplier;
                        }
                        else if (nextSym == BZip2.RUNB)
                        {
                            runLength += multiplier << 1;
                        }
                        else
                        {
                            break;
                        }
                        if (groupPos == 0)
                        {
                            groupPos = BZip2.G_SIZE - 1;
                            tableIndex = state.selector[++groupNo] & 0xff;
                            base_tableIndex = state.gBase[tableIndex];
                            limit_tableIndex = state.gLimit[tableIndex];
                            perm_tableIndex = state.gPerm[tableIndex];
                            minLens_tableIndex = state.gMinlen[tableIndex];
                        }
                        else
                        {
                            groupPos--;
                        }
                        int codeLength = minLens_tableIndex;
                        // Inlined:
                        // int vectorValue = GetBits(codeLength);
                        while (bsLiveShadow < codeLength)
                        {
                            int byteValue = this.input.ReadByte();
                            if (byteValue >= 0)
                            {
                                bsBuffShadow = (bsBuffShadow << 8) | byteValue;
                                bsLiveShadow += 8;
                                continue;
                            }
                            else
                            {
                                throw new IOException("unexpected end of stream");
                            }
                        }
                        int vectorValue = (bsBuffShadow >> (bsLiveShadow - codeLength))
                            & ((1 << codeLength) - 1);
                        bsLiveShadow -= codeLength;
                        while (vectorValue > limit_tableIndex[codeLength])
                        {
                            codeLength++;
                            while (bsLiveShadow < 1)
                            {
                                int byteValue = this.input.ReadByte();
                                if (byteValue >= 0)
                                {
                                    bsBuffShadow = (bsBuffShadow << 8) | byteValue;
                                    bsLiveShadow += 8;
                                    continue;
                                }
                                else
                                {
                                    throw new IOException("unexpected end of stream");
                                }
                            }
                            bsLiveShadow--;
                            vectorValue = (vectorValue << 1)
                                | ((bsBuffShadow >> bsLiveShadow) & 1);
                        }
                        nextSym = perm_tableIndex[vectorValue - base_tableIndex[codeLength]];
                    }
                    byte character = state.seqToUnseq[yy[0]];
                    state.unzftab[character & 0xff] += runLength + 1;
                    while (runLength-- >= 0)
                    {
                        state.ll8[++lastShadow] = character;
                    }
                    if (lastShadow >= limitLast)
                        throw new IOException("block overrun");
                }
                else
                {
                    if (++lastShadow >= limitLast)
                        throw new IOException("block overrun");
                    byte symbolValue = yy[nextSym - 1];
                    state.unzftab[state.seqToUnseq[symbolValue] & 0xff]++;
                    state.ll8[lastShadow] = state.seqToUnseq[symbolValue];
                    /*
                     * This loop is hammered during decompression, hence avoid
                     * native method call overhead of System.Buffer.BlockCopy for very
                     * small ranges to copy.
                     */
                    if (nextSym <= 16)
                    {
                        for (int j = nextSym - 1; j > 0;)
                        {
                            yy[j] = yy[--j];
                        }
                    }
                    else
                    {
                        System.Buffer.BlockCopy(yy, 0, yy, 1, nextSym - 1);
                    }
                    yy[0] = symbolValue;
                    if (groupPos == 0)
                    {
                        groupPos = BZip2.G_SIZE - 1;
                        tableIndex = state.selector[++groupNo] & 0xff;
                        base_tableIndex = state.gBase[tableIndex];
                        limit_tableIndex = state.gLimit[tableIndex];
                        perm_tableIndex = state.gPerm[tableIndex];
                        minLens_tableIndex = state.gMinlen[tableIndex];
                    }
                    else
                    {
                        groupPos--;
                    }
                    int codeLength = minLens_tableIndex;
                    // Inlined:
                    // int vectorValue = GetBits(codeLength);
                    while (bsLiveShadow < codeLength)
                    {
                        int byteValue = this.input.ReadByte();
                        if (byteValue >= 0)
                        {
                            bsBuffShadow = (bsBuffShadow << 8) | byteValue;
                            bsLiveShadow += 8;
                            continue;
                        }
                        else
                        {
                            throw new IOException("unexpected end of stream");
                        }
                    }
                    int vectorValue = (bsBuffShadow >> (bsLiveShadow - codeLength))
                        & ((1 << codeLength) - 1);
                    bsLiveShadow -= codeLength;
                    while (vectorValue > limit_tableIndex[codeLength])
                    {
                        codeLength++;
                        while (bsLiveShadow < 1)
                        {
                            int byteValue = this.input.ReadByte();
                            if (byteValue >= 0)
                            {
                                bsBuffShadow = (bsBuffShadow << 8) | byteValue;
                                bsLiveShadow += 8;
                                continue;
                            }
                            else
                            {
                                throw new IOException("unexpected end of stream");
                            }
                        }
                        bsLiveShadow--;
                        vectorValue = (vectorValue << 1) | ((bsBuffShadow >> bsLiveShadow) & 1);
                    }
                    nextSym = perm_tableIndex[vectorValue - base_tableIndex[codeLength]];
                }
            }
            this.last = lastShadow;
            this.bsLive = bsLiveShadow;
            this.bsBuff = bsBuffShadow;
        }
        private int getAndMoveToFrontDecode0(int groupNo)
        {
            var state = this.data;
            int tableIndex = state.selector[groupNo] & 0xff;
            int[] limit_tableIndex = state.gLimit[tableIndex];
            int codeLength = state.gMinlen[tableIndex];
            int vectorValue = GetBits(codeLength);
            int bsLiveShadow = this.bsLive;
            int bsBuffShadow = this.bsBuff;
            while (vectorValue > limit_tableIndex[codeLength])
            {
                codeLength++;
                while (bsLiveShadow < 1)
                {
                    int byteValue = this.input.ReadByte();
                    if (byteValue >= 0)
                    {
                        bsBuffShadow = (bsBuffShadow << 8) | byteValue;
                        bsLiveShadow += 8;
                        continue;
                    }
                    else
                    {
                        throw new IOException("unexpected end of stream");
                    }
                }
                bsLiveShadow--;
                vectorValue = (vectorValue << 1) | ((bsBuffShadow >> bsLiveShadow) & 1);
            }
            this.bsLive = bsLiveShadow;
            this.bsBuff = bsBuffShadow;
            return state.gPerm[tableIndex][vectorValue - state.gBase[tableIndex][codeLength]];
        }
        private void SetupBlock()
        {
            if (this.data == null)
                return;
            int i;
            var state = this.data;
            int[] tt = state.initTT(this.last + 1);
            //       xxxx
            /* Check: unzftab entries in range. */
            for (i = 0; i <= 255; i++)
            {
                if (state.unzftab[i] < 0 || state.unzftab[i] > this.last)
                    throw new Exception("BZ_DATA_ERROR");
            }
            /* Actually generate cftab. */
            state.cftab[0] = 0;
            for (i = 1; i <= 256; i++) state.cftab[i] = state.unzftab[i-1];
            for (i = 1; i <= 256; i++) state.cftab[i] += state.cftab[i-1];
            /* Check: cftab entries in range. */
            for (i = 0; i <= 256; i++)
            {
                if (state.cftab[i] < 0 || state.cftab[i] > this.last+1)
                {
                    var msg = String.Format("BZ_DATA_ERROR: cftab[{0}]={1} last={2}",
                                            i, state.cftab[i], this.last);
                    throw new Exception(msg);
                }
            }
            /* Check: cftab entries non-descending. */
            for (i = 1; i <= 256; i++)
            {
                if (state.cftab[i-1] > state.cftab[i])
                    throw new Exception("BZ_DATA_ERROR");
            }
            int lastShadow;
            for (i = 0, lastShadow = this.last; i <= lastShadow; i++)
            {
                tt[state.cftab[state.ll8[i] & 0xff]++] = i;
            }
            if ((this.origPtr < 0) || (this.origPtr >= tt.Length))
                throw new IOException("stream corrupted");
            this.setupTPosition = tt[this.origPtr];
            this.setupCount = 0;
            this.setupIndex2 = 0;
            this.setupChar2 = 256; /* not a valid 8-bit byte value?, and not EOF */
            if (this.blockRandomised)
            {
                this.setupRandomToGo = 0;
                this.setupRandomPosition = 0;
                SetupRandPartA();
            }
            else
            {
                SetupNoRandPartA();
            }
        }
        private void SetupRandPartA()
        {
            if (this.setupIndex2 <= this.last)
            {
                this.setupPreviousChar = this.setupChar2;
                int setupChar2Shadow = this.data.ll8[this.setupTPosition] & 0xff;
                this.setupTPosition = this.data.tt[this.setupTPosition];
                if (this.setupRandomToGo == 0)
                {
                    this.setupRandomToGo = Rand.Rnums(this.setupRandomPosition) - 1;
                    if (++this.setupRandomPosition == 512)
                    {
                        this.setupRandomPosition = 0;
                    }
                }
                else
                {
                    this.setupRandomToGo--;
                }
                this.setupChar2 = setupChar2Shadow ^= (this.setupRandomToGo == 1) ? 1 : 0;
                this.setupIndex2++;
                this.currentChar = setupChar2Shadow;
                this.currentState = CState.RAND_PART_B;
                this.crc.UpdateCRC((byte)setupChar2Shadow);
            }
            else
            {
                EndBlock();
                InitBlock();
                SetupBlock();
            }
        }
        private void SetupNoRandPartA()
        {
            if (this.setupIndex2 <= this.last)
            {
                this.setupPreviousChar = this.setupChar2;
                int setupChar2Shadow = this.data.ll8[this.setupTPosition] & 0xff;
                this.setupChar2 = setupChar2Shadow;
                this.setupTPosition = this.data.tt[this.setupTPosition];
                this.setupIndex2++;
                this.currentChar = setupChar2Shadow;
                this.currentState = CState.NO_RAND_PART_B;
                this.crc.UpdateCRC((byte)setupChar2Shadow);
            }
            else
            {
                this.currentState = CState.NO_RAND_PART_A;
                EndBlock();
                InitBlock();
                SetupBlock();
            }
        }
        private void SetupRandPartB()
        {
            if (this.setupChar2 != this.setupPreviousChar)
            {
                this.currentState = CState.RAND_PART_A;
                this.setupCount = 1;
                SetupRandPartA();
            }
            else if (++this.setupCount >= 4)
            {
                this.setupZ = (char) (this.data.ll8[this.setupTPosition] & 0xff);
                this.setupTPosition = this.data.tt[this.setupTPosition];
                if (this.setupRandomToGo == 0)
                {
                    this.setupRandomToGo = Rand.Rnums(this.setupRandomPosition) - 1;
                    if (++this.setupRandomPosition == 512)
                    {
                        this.setupRandomPosition = 0;
                    }
                }
                else
                {
                    this.setupRandomToGo--;
                }
                this.setupJ2 = 0;
                this.currentState = CState.RAND_PART_C;
                if (this.setupRandomToGo == 1)
                {
                    this.setupZ ^= (char)1;
                }
                SetupRandPartC();
            }
            else
            {
                this.currentState = CState.RAND_PART_A;
                SetupRandPartA();
            }
        }
        private void SetupRandPartC()
        {
            if (this.setupJ2 < this.setupZ)
            {
                this.currentChar = this.setupChar2;
                this.crc.UpdateCRC((byte)this.setupChar2);
                this.setupJ2++;
            }
            else
            {
                this.currentState = CState.RAND_PART_A;
                this.setupIndex2++;
                this.setupCount = 0;
                SetupRandPartA();
            }
        }
        private void SetupNoRandPartB()
        {
            if (this.setupChar2 != this.setupPreviousChar)
            {
                this.setupCount = 1;
                SetupNoRandPartA();
            }
            else if (++this.setupCount >= 4)
            {
                this.setupZ = (char) (this.data.ll8[this.setupTPosition] & 0xff);
                this.setupTPosition = this.data.tt[this.setupTPosition];
                this.setupJ2 = 0;
                SetupNoRandPartC();
            }
            else
            {
                SetupNoRandPartA();
            }
        }
        private void SetupNoRandPartC()
        {
            if (this.setupJ2 < this.setupZ)
            {
                int setupChar2Shadow = this.setupChar2;
                this.currentChar = setupChar2Shadow;
                this.crc.UpdateCRC((byte)setupChar2Shadow);
                this.setupJ2++;
                this.currentState = CState.NO_RAND_PART_C;
            }
            else
            {
                this.setupIndex2++;
                this.setupCount = 0;
                SetupNoRandPartA();
            }
        }
        private sealed class DecompressionState
        {
            // (with blockSize 900k)
            readonly public bool[] inUse = new bool[256];
            readonly public byte[] seqToUnseq = new byte[256]; // 256 byte
            readonly public byte[] selector = new byte[BZip2.MaxSelectors]; // 18002 byte
            readonly public byte[] selectorMtf = new byte[BZip2.MaxSelectors]; // 18002 byte
            /**
             * Freq table collected to save a pass over the data during
             * decompression.
             */
            public readonly int[] unzftab;
            public readonly int[][] gLimit;
            public readonly int[][] gBase;
            public readonly int[][] gPerm;
            public readonly int[] gMinlen;
            public readonly int[] cftab;
            public readonly byte[] getAndMoveToFrontDecode_yy;
            public readonly char[][] temp_charArray2d;
            public readonly byte[] recvDecodingTables_pos;
            // ---------------
            // 60798 byte
            public int[] tt; // 3600000 byte
            public byte[] ll8; // 900000 byte
            // ---------------
            // 4560782 byte
            // ===============
            public DecompressionState(int blockSize100k)
            {
                this.unzftab = new int[256]; // 1024 byte
                this.gLimit = BZip2.InitRectangularArray<int>(BZip2.NGroups,BZip2.MaxAlphaSize);
                this.gBase = BZip2.InitRectangularArray<int>(BZip2.NGroups,BZip2.MaxAlphaSize);
                this.gPerm = BZip2.InitRectangularArray<int>(BZip2.NGroups,BZip2.MaxAlphaSize);
                this.gMinlen = new int[BZip2.NGroups]; // 24 byte
                this.cftab = new int[257]; // 1028 byte
                this.getAndMoveToFrontDecode_yy = new byte[256]; // 512 byte
                this.temp_charArray2d = BZip2.InitRectangularArray<char>(BZip2.NGroups,BZip2.MaxAlphaSize);
                this.recvDecodingTables_pos = new byte[BZip2.NGroups]; // 6 byte
                this.ll8 = new byte[blockSize100k * BZip2.BlockSizeMultiple];
            }
            /**
             * Initializes the tt array.
             *
             * This method is called when the required length of the array is known.
             * I don't initialize it at construction time to avoid unneccessary
             * memory allocation when compressing small files.
             */
            public int[] initTT(int length)
            {
                int[] ttShadow = this.tt;
                // tt.length should always be >= length, but theoretically
                // it can happen, if the compressor mixed small and large
                // blocks. Normally only the last block will be smaller
                // than others.
                if ((ttShadow == null) || (ttShadow.Length < length))
                {
                    this.tt = ttShadow = new int[length];
                }
                return ttShadow;
            }
        }
    }
    // /**
    //  * Checks if the signature matches what is expected for a bzip2 file.
    //  *
    //  * @param signature
    //  *            the bytes to check
    //  * @param length
    //  *            the number of bytes to check
    //  * @return true, if this stream is a bzip2 compressed stream, false otherwise
    //  *
    //  * @since Apache Commons Compress 1.1
    //  */
    // public static boolean MatchesSig(byte[] signature)
    // {
    //     if ((signature.Length < 3) ||
    //         (signature[0] != 'B') ||
    //         (signature[1] != 'Z') ||
    //         (signature[2] != 'h'))
    //         return false;
    //
    //     return true;
    // }
    internal static class BZip2
    {
            internal static T[][] InitRectangularArray<T>(int rowCount, int columnCount)
            {
                var array = new T[rowCount][];
                for (int i=0; i < rowCount; i++)
                {
                    array[i] = new T[columnCount];
                }
                return array;
            }
        public static readonly int BlockSizeMultiple       = 100000;
        public static readonly int MinBlockSize       = 1;
        public static readonly int MaxBlockSize       = 9;
        public static readonly int MaxAlphaSize        = 258;
        public static readonly int MaxCodeLength       = 23;
        public static readonly char RUNA                = (char) 0;
        public static readonly char RUNB                = (char) 1;
        public static readonly int NGroups             = 6;
        public static readonly int G_SIZE              = 50;
        public static readonly int N_ITERS             = 4;
        public static readonly int MaxSelectors        = (2 + (900000 / G_SIZE));
        public static readonly int NUM_OVERSHOOT_BYTES = 20;
    /*
     * <p> If you are ever unlucky/improbable enough to get a stack
     * overflow whilst sorting, increase the following constant and
     * try again. In practice I have never seen the stack go above 27
     * elems, so the following limit seems very generous.  </p>
     */
        internal static readonly int QSORT_STACK_SIZE = 1000;
    }