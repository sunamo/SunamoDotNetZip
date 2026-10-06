namespace Ionic.Zlib;

// ZlibCodec.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa and Microsoft Corporation.  
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
// last saved (in emacs): 
// Time-stamp: <2009-November-03 15:40:51>
//
// ------------------------------------------------------------------
//
// This module defines a Codec for ZLIB compression and
// decompression. This code extends code that was based the jzlib
// implementation of zlib, but this code is completely novel.  The codec
// class is new, and encapsulates some behaviors that are new, and some
// that were present in other classes in the jzlib code base.  In
// keeping with the license for jzlib, the copyright to the jzlib code
// is included below.
//
// ------------------------------------------------------------------
// 
// Copyright (c) 2000,2001,2002,2003 ymnk, JCraft,Inc. All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright 
// notice, this list of conditions and the following disclaimer in 
// the documentation and/or other materials provided with the distribution.
// 
// 3. The names of the authors may not be used to endorse or promote products
// derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL JCRAFT,
// INC. OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
// OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
// EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
// -----------------------------------------------------------------------
//
// This program is based on zlib-1.1.3; credit to authors
// Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
// and contributors of zlib.
//
// -----------------------------------------------------------------------
using Interop = System.Runtime.InteropServices;
#if !PCL
[Interop.GuidAttribute("ebc25cf6-9120-4283-b972-0e5520d0000D")]
[Interop.ComVisible(true)]
[Interop.ClassInterface(Interop.ClassInterfaceType.AutoDispatch)]
#endif
sealed public class ZlibCodec
{
    public byte[] InputBuffer;
    public int NextIn;
    public int AvailableBytesIn;
    public long TotalBytesIn;
    public byte[] OutputBuffer;
    public int NextOut;
    public int AvailableBytesOut;
    public long TotalBytesOut;
    public System.String Message;
    internal DeflateManager dstate;
    internal InflateManager istate;
    internal uint _Adler32;
    public CompressionLevel CompressLevel = CompressionLevel.Default;
    public int WindowBits = ZlibConstants.WindowBitsDefault;
    public CompressionStrategy Strategy = CompressionStrategy.Default;
    public int Adler32 { get { return (int)_Adler32; } }
    public ZlibCodec() { }
    public ZlibCodec(CompressionMode mode)
    {
        if (mode == CompressionMode.Compress)
        {
            int rc = InitializeDeflate();
            if (rc != ZlibConstants.Z_OK) throw new ZlibException("Cannot initialize for deflate.");
        }
        else if (mode == CompressionMode.Decompress)
        {
            int rc = InitializeInflate();
            if (rc != ZlibConstants.Z_OK) throw new ZlibException("Cannot initialize for inflate.");
        }
        else throw new ZlibException("Invalid ZlibStreamFlavor.");
    }
    public int InitializeInflate() => InitializeInflate(this.WindowBits);
    public int InitializeInflate(bool expectRfc1950Header) => InitializeInflate(this.WindowBits, expectRfc1950Header);
    public int InitializeInflate(int windowBits)
    {
        this.WindowBits = windowBits;
        return InitializeInflate(windowBits, true);
    }
    public int InitializeInflate(int windowBits, bool expectRfc1950Header)
    {
        this.WindowBits = windowBits;
        if (dstate != null) throw new ZlibException("You may not call InitializeInflate() after calling InitializeDeflate().");
        istate = new InflateManager(expectRfc1950Header);
        return istate.Initialize(this, windowBits);
    }
    public int Inflate() => istate == null ? throw new ZlibException("No Inflate State!") : istate.Inflate();
    public int EndInflate()
    {
        if (istate == null)
            throw new ZlibException("No Inflate State!");
        int result = istate.End();
        istate = null;
        return result;
    }
    public int SyncInflate() => istate == null ? throw new ZlibException("No Inflate State!") : istate.Sync();
    public int InitializeDeflate() => _InternalInitializeDeflate(true);
    public int InitializeDeflate(CompressionLevel level)
    {
        this.CompressLevel = level;
        return _InternalInitializeDeflate(true);
    }
    public int InitializeDeflate(CompressionLevel level, bool wantRfc1950Header)
    {
        this.CompressLevel = level;
        return _InternalInitializeDeflate(wantRfc1950Header);
    }
    public int InitializeDeflate(CompressionLevel level, int bits)
    {
        this.CompressLevel = level;
        this.WindowBits = bits;
        return _InternalInitializeDeflate(true);
    }
    public int InitializeDeflate(CompressionLevel level, int bits, bool wantRfc1950Header)
    {
        this.CompressLevel = level;
        this.WindowBits = bits;
        return _InternalInitializeDeflate(wantRfc1950Header);
    }
    private int _InternalInitializeDeflate(bool wantRfc1950Header)
    {
        if (istate != null) throw new ZlibException("You may not call InitializeDeflate() after calling InitializeInflate().");
        dstate = new DeflateManager
        {
            WantRfc1950HeaderBytes = wantRfc1950Header
        };
        return dstate.Initialize(this, this.CompressLevel, this.WindowBits, this.Strategy);
    }
    public int Deflate(FlushType flush) => dstate == null ? throw new ZlibException("No Deflate State!") : dstate.Deflate(flush);
    public int EndDeflate()
    {
        if (dstate == null)
            throw new ZlibException("No Deflate State!");
        // TODO: dinoch Tue, 03 Nov 2009  15:39 (test this)
        //int ret = dstate.End();
        dstate = null;
        return ZlibConstants.Z_OK; //ret;
    }
    public void ResetDeflate()
    {
        if (dstate == null)
            throw new ZlibException("No Deflate State!");
        dstate.Reset();
    }
    public int SetDeflateParams(CompressionLevel level, CompressionStrategy strategy) => dstate == null ? throw new ZlibException("No Deflate State!") : dstate.SetParams(level, strategy);
    public int SetDictionary(byte[] dictionary)
    {
        if (istate != null)
            return istate.SetDictionary(dictionary);
        return dstate != null ? dstate.SetDictionary(dictionary) : throw new ZlibException("No Inflate or Deflate state!");
    }
    public int SetDictionaryUnconditionally(byte[] dictionary)
    {
        if (istate != null)
            return istate.SetDictionary(dictionary, true);
        return dstate != null ? dstate.SetDictionary(dictionary) : throw new ZlibException("No Inflate or Deflate state!");
    }
    // Flush as much pending output as possible. All deflate() output goes
    // through this function so some applications may wish to modify it
    // to avoid allocating a large strm->next_out buffer and copying into it.
    // (See also read_buf()).
    internal void flush_pending()
    {
        int len = dstate.pendingCount;
        if (len > AvailableBytesOut)
            len = AvailableBytesOut;
        if (len == 0)
            return;
        if (dstate.pending.Length <= dstate.nextPending ||
            OutputBuffer.Length <= NextOut ||
            dstate.pending.Length < (dstate.nextPending + len) ||
            OutputBuffer.Length < (NextOut + len))
        {
            throw new ZlibException(String.Format("Invalid State. (pending.Length={0}, pendingCount={1})",
                dstate.pending.Length, dstate.pendingCount));
        }
        Array.Copy(dstate.pending, dstate.nextPending, OutputBuffer, NextOut, len);
        NextOut += len;
        dstate.nextPending += len;
        TotalBytesOut += len;
        AvailableBytesOut -= len;
        dstate.pendingCount -= len;
        if (dstate.pendingCount == 0)
        {
            dstate.nextPending = 0;
        }
    }
    // Read a new buffer from the current input stream, update the adler32
    // and total number of bytes read.  All deflate() input goes through
    // this function so some applications may wish to modify it to avoid
    // allocating a large strm->next_in buffer and copying from it.
    // (See also flush_pending()).
    internal int read_buf(byte[] buf, int start, int size)
    {
        int len = AvailableBytesIn;
        if (len > size)
            len = size;
        if (len == 0)
            return 0;
        AvailableBytesIn -= len;
        if (dstate.WantRfc1950HeaderBytes)
        {
            _Adler32 = Adler.Adler32(_Adler32, InputBuffer, NextIn, len);
        }
        Array.Copy(InputBuffer, NextIn, buf, start, len);
        NextIn += len;
        TotalBytesIn += len;
        return len;
    }
}