namespace Ionic.Zlib;

// ZlibStream.cs
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
// Time-stamp: <2011-July-31 14:53:33>
//
// ------------------------------------------------------------------
//
// This module defines the ZlibStream class, which is similar in idea to
// the System.IO.Compression.DeflateStream and
// System.IO.Compression.GZipStream classes in the .NET BCL.
//
// ------------------------------------------------------------------
public class ZlibStream : System.IO.Stream
{
    internal ZlibBaseStream _baseStream;
    bool _disposed;
    public ZlibStream(System.IO.Stream stream, CompressionMode mode)
        : this(stream, mode, CompressionLevel.Default, false)
    {
    }
    public ZlibStream(System.IO.Stream stream, CompressionMode mode, CompressionLevel level)
        : this(stream, mode, level, false)
    {
    }
    public ZlibStream(System.IO.Stream stream, CompressionMode mode, bool leaveOpen)
        : this(stream, mode, CompressionLevel.Default, leaveOpen)
    {
    }
    public ZlibStream(System.IO.Stream stream, CompressionMode mode, CompressionLevel level, bool leaveOpen)
    {
        _baseStream = new ZlibBaseStream(stream, mode, level, ZlibStreamFlavor.ZLIB, leaveOpen);
    }
    #region Zlib properties
    virtual public FlushType FlushMode
    {
        get { return (this._baseStream._flushMode); }
        set
        {
            if (_disposed) throw new ObjectDisposedException("ZlibStream");
            this._baseStream._flushMode = value;
        }
    }
    public int BufferSize
    {
        get
        {
            return this._baseStream._bufferSize;
        }
        set
        {
            if (_disposed) throw new ObjectDisposedException("ZlibStream");
            if (this._baseStream._workingBuffer != null)
                throw new ZlibException("The working buffer is already set.");
            if (value < ZlibConstants.WorkingBufferSizeMin)
                throw new ZlibException(String.Format("Don't be silly. {0} bytes?? Use a bigger buffer, at least {1}.", value, ZlibConstants.WorkingBufferSizeMin));
            this._baseStream._bufferSize = value;
        }
    }
    virtual public long TotalIn
    {
        get { return this._baseStream._z.TotalBytesIn; }
    }
    virtual public long TotalOut
    {
        get { return this._baseStream._z.TotalBytesOut; }
    }
    #endregion
    #region System.IO.Stream methods
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (!_disposed)
            {
                if (disposing && (this._baseStream != null))
                    this._baseStream.Dispose();
                _disposed = true;
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }
    public override bool CanRead
    {
        get
        {
            return _disposed ? throw new ObjectDisposedException("ZlibStream") : _baseStream._stream.CanRead;
        }
    }
    public override bool CanSeek
    {
        get { return false; }
    }
    public override bool CanWrite
    {
        get
        {
            return _disposed ? throw new ObjectDisposedException("ZlibStream") : _baseStream._stream.CanWrite;
        }
    }
    public override void Flush()
    {
        if (_disposed) throw new ObjectDisposedException("ZlibStream");
        _baseStream.Flush();
    }
    public override long Length
    {
        get { throw new NotSupportedException(); }
    }
    public override long Position
    {
        get
        {
            if (this._baseStream._streamMode == ZlibBaseStream.StreamMode.Writer)
                return this._baseStream._z.TotalBytesOut;
            return this._baseStream._streamMode == ZlibBaseStream.StreamMode.Reader ? _baseStream._z.TotalBytesIn : 0;
        }
        set { throw new NotSupportedException(); }
    }
    public override int Read(byte[] buffer, int offset, int count) => _disposed ? throw new ObjectDisposedException("ZlibStream") : _baseStream.Read(buffer, offset, count);
    public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (_disposed) throw new ObjectDisposedException("ZlibStream");
        _baseStream.Write(buffer, offset, count);
    }
    #endregion
    public static byte[] CompressString(String text)
    {
        using var ms = new MemoryStream();
        Stream compressor =
            new ZlibStream(ms, CompressionMode.Compress, CompressionLevel.BestCompression);
        ZlibBaseStream.CompressString(text, compressor);
        return ms.ToArray();
    }
    public static byte[] CompressBuffer(byte[] buffer)
    {
        using var ms = new MemoryStream();
        Stream compressor =
            new ZlibStream(ms, CompressionMode.Compress, CompressionLevel.BestCompression);
        ZlibBaseStream.CompressBuffer(buffer, compressor);
        return ms.ToArray();
    }
    public static String UncompressString(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        Stream decompressor =
            new ZlibStream(input, CompressionMode.Decompress);
        return ZlibBaseStream.UncompressString(decompressor);
    }
    public static byte[] UncompressBuffer(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        Stream decompressor =
            new ZlibStream(input, CompressionMode.Decompress);
        return ZlibBaseStream.UncompressBuffer(decompressor);
    }
}