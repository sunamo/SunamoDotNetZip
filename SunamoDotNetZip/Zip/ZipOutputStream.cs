namespace Ionic.Zip;

// ZipOutputStream.cs
//
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa.
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
// Time-stamp: <2011-July-28 06:34:30>
//
// ------------------------------------------------------------------
//
// This module defines the ZipOutputStream class, which is a stream metaphor for
// generating zip files.  This class does not depend on Ionic.Zip.ZipFile, but rather
// stands alongside it as an alternative "container" for ZipEntry.  It replicates a
// subset of the properties, including these:
//
//  - Comment
//  - Encryption
//  - Password
//  - CodecBufferSize
//  - CompressionLevel
//  - CompressionMethod
//  - EnableZip64 (UseZip64WhenSaving)
//  - IgnoreCase (!CaseSensitiveRetrieval)
//
// It adds these novel methods:
//
//  - PutNextEntry
//
//
// ------------------------------------------------------------------
//
public class ZipOutputStream : Stream
{
    public ZipOutputStream(Stream stream) : this(stream, false) { }
    public ZipOutputStream(String fileName)
    {
        Stream stream = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        _Init(stream, false, fileName);
    }
    public ZipOutputStream(Stream stream, bool leaveOpen)
    {
        _Init(stream, leaveOpen, null);
    }
    private void _Init(Stream stream, bool leaveOpen, string name)
    {
        // workitem 9307
        _outputStream = stream.CanRead ? stream : new CountingStream(stream);
        CompressionLevel = CompressionLevel.Default;
        CompressionMethod = Ionic.Zip.CompressionMethod.Deflate;
        _encryption = EncryptionAlgorithm.None;
        _entriesWritten = new Dictionary<String, ZipEntry>(StringComparer.Ordinal);
        _zip64 = Zip64Option.Never;
        _leaveUnderlyingStreamOpen = leaveOpen;
        Strategy = CompressionStrategy.Default;
        _name = name ?? "(stream)";
        ParallelDeflateThreshold = -1L;
    }
    public override String ToString() => String.Format("ZipOutputStream::{0}(leaveOpen({1})))", _name, _leaveUnderlyingStreamOpen);
    public String Password
    {
        set
        {
            if (_disposed)
            {
                _exceptionPending = true;
                throw new System.InvalidOperationException("The stream has been closed.");
            }
            _password = value;
            if (_password == null)
            {
                _encryption = EncryptionAlgorithm.None;
            }
            else if (_encryption == EncryptionAlgorithm.None)
            {
                _encryption = EncryptionAlgorithm.PkzipWeak;
            }
        }
    }
    public EncryptionAlgorithm Encryption
    {
        get
        {
            return _encryption;
        }
        set
        {
            if (_disposed)
            {
                _exceptionPending = true;
                throw new System.InvalidOperationException("The stream has been closed.");
            }
            if (value == EncryptionAlgorithm.Unsupported)
            {
                _exceptionPending = true;
                throw new InvalidOperationException("You may not set Encryption to that value.");
            }
            _encryption = value;
        }
    }
    public int CodecBufferSize
    {
        get;
        set;
    }
    public CompressionStrategy Strategy
    {
        get;
        set;
    }
    public ZipEntryTimestamp Timestamp
    {
        get
        {
            return _timestamp;
        }
        set
        {
            if (_disposed)
            {
                _exceptionPending = true;
                throw new System.InvalidOperationException("The stream has been closed.");
            }
            _timestamp = value;
        }
    }
    public CompressionLevel CompressionLevel
    {
        get;
        set;
    }
    public Ionic.Zip.CompressionMethod CompressionMethod
    {
        get;
        set;
    }
    public string Comment
    {
        get { return _comment; }
        set
        {
            if (_disposed)
            {
                _exceptionPending = true;
                throw new System.InvalidOperationException("The stream has been closed.");
            }
            _comment = value;
        }
    }
    public Zip64Option EnableZip64
    {
        get
        {
            return _zip64;
        }
        set
        {
            if (_disposed)
            {
                _exceptionPending = true;
                throw new System.InvalidOperationException("The stream has been closed.");
            }
            _zip64 = value;
        }
    }
    public bool OutputUsedZip64
    {
        get
        {
            return _anyEntriesUsedZip64 || _directoryNeededZip64;
        }
    }
    public bool IgnoreCase
    {
        get
        {
            return !_DontIgnoreCase;
        }
        set
        {
            _DontIgnoreCase = !value;
        }
    }
    [Obsolete("Beginning with v1.9.1.6 of DotNetZip, this property is obsolete. It will be removed in a future version of the library. Use AlternateEncoding and AlternateEncodingUsage instead.")]
    public bool UseUnicodeAsNecessary
    {
        get
        {
            return (_alternateEncoding == System.Text.Encoding.UTF8) &&
                (AlternateEncodingUsage == ZipOption.AsNecessary);
        }
        set
        {
            if (value)
            {
                _alternateEncoding = System.Text.Encoding.UTF8;
                _alternateEncodingUsage = ZipOption.AsNecessary;
            }
            else
            {
                _alternateEncoding = Ionic.Zip.ZipOutputStream.DefaultEncoding;
                _alternateEncodingUsage = ZipOption.Never;
            }
        }
    }
    [Obsolete("use AlternateEncoding and AlternateEncodingUsage instead.")]
    public System.Text.Encoding ProvisionalAlternateEncoding
    {
        get
        {
            return _alternateEncodingUsage == ZipOption.AsNecessary ? _alternateEncoding : null;
        }
        set
        {
            _alternateEncoding = value;
            _alternateEncodingUsage = ZipOption.AsNecessary;
        }
    }
    public System.Text.Encoding AlternateEncoding
    {
        get
        {
            return _alternateEncoding;
        }
        set
        {
            _alternateEncoding = value;
        }
    }
    public ZipOption AlternateEncodingUsage
    {
        get
        {
            return _alternateEncodingUsage;
        }
        set
        {
            _alternateEncodingUsage = value;
        }
    }
    public static System.Text.Encoding DefaultEncoding
    {
        get
        {
#if NETCOREAPP2_0 || NETSTANDARD2_0
                return System.Text.CodePagesEncodingProvider.Instance.GetEncoding(1252);
#else
            return System.Text.Encoding.GetEncoding("IBM437");
#endif
        }
    }
    public long ParallelDeflateThreshold
    {
        set
        {
            if ((value != 0) && (value != -1) && (value < 64 * 1024))
                throw new ArgumentOutOfRangeException("value must be greater than 64k, or 0, or -1");
            _ParallelDeflateThreshold = value;
        }
        get
        {
            return _ParallelDeflateThreshold;
        }
    }
    public int ParallelDeflateMaxBufferPairs
    {
        get
        {
            return _maxBufferPairs;
        }
        set
        {
            if (value < 4)
                throw new ArgumentOutOfRangeException("ParallelDeflateMaxBufferPairs",
                                            "Value must be 4 or greater.");
            _maxBufferPairs = value;
        }
    }
    private void InsureUniqueEntry(ZipEntry ze1)
    {
        if (_entriesWritten.ContainsKey(ze1.FileName))
        {
            _exceptionPending = true;
            throw new ArgumentException(String.Format("The entry '{0}' already exists in the zip archive.", ze1.FileName));
        }
    }
    internal Stream OutputStream
    {
        get
        {
            return _outputStream;
        }
    }
    internal String Name
    {
        get
        {
            return _name;
        }
    }
    public bool ContainsEntry(string name) => _entriesWritten.ContainsKey(SharedUtilities.NormalizePathForUseInZipFile(name));
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (_disposed)
        {
            _exceptionPending = true;
            throw new System.InvalidOperationException("The stream has been closed.");
        }
        if (buffer == null)
        {
            _exceptionPending = true;
            throw new System.ArgumentNullException(nameof(buffer));
        }
        if (_currentEntry == null)
        {
            _exceptionPending = true;
            throw new System.InvalidOperationException("You must call PutNextEntry() before calling Write().");
        }
        if (_currentEntry.IsDirectory)
        {
            _exceptionPending = true;
            throw new System.InvalidOperationException("You cannot Write() data for an entry that is a directory.");
        }
        if (_needToWriteEntryHeader)
            _InitiateCurrentEntry(false);
        if (count != 0)
            _entryOutputStream.Write(buffer, offset, count);
    }
    public ZipEntry PutNextEntry(String entryName)
    {
        if (String.IsNullOrEmpty(entryName))
            throw new ArgumentNullException(nameof(entryName));
        if (_disposed)
        {
            _exceptionPending = true;
            throw new System.InvalidOperationException("The stream has been closed.");
        }
        _FinishCurrentEntry();
        _currentEntry = ZipEntry.CreateForZipOutputStream(entryName);
        _currentEntry._container = new ZipContainer(this);
        _currentEntry._BitField |= 0x0008;  // workitem 8932
        _currentEntry.SetEntryTimes(DateTime.Now, DateTime.Now, DateTime.Now);
        _currentEntry.CompressionLevel = this.CompressionLevel;
        _currentEntry.CompressionMethod = this.CompressionMethod;
        _currentEntry.Password = _password; // workitem 13909
        _currentEntry.Encryption = this.Encryption;
        // workitem 12634
        _currentEntry.AlternateEncoding = this.AlternateEncoding;
        _currentEntry.AlternateEncodingUsage = this.AlternateEncodingUsage;
        if (entryName.EndsWith("/")) _currentEntry.MarkAsDirectory();
        _currentEntry.EmitTimesInWindowsFormatWhenSaving = ((_timestamp & ZipEntryTimestamp.Windows) != 0);
        _currentEntry.EmitTimesInUnixFormatWhenSaving = ((_timestamp & ZipEntryTimestamp.Unix) != 0);
        InsureUniqueEntry(_currentEntry);
        _needToWriteEntryHeader = true;
        return _currentEntry;
    }
    private void _InitiateCurrentEntry(bool finishing)
    {
        // If finishing==true, this means we're initiating the entry at the time of
        // Close() or PutNextEntry().  If this happens, it means no data was written
        // for the entry - Write() was never called.  (The usual case us to call
        // _InitiateCurrentEntry(bool) from within Write().)  If finishing==true,
        // the entry could be either a zero-byte file or a directory.
        _entriesWritten.Add(_currentEntry.FileName, _currentEntry);
        _entryCount++; // could use _entriesWritten.Count, but I don't want to incur
                       // the cost.
        if (_entryCount > 65534 && _zip64 == Zip64Option.Never)
        {
            _exceptionPending = true;
            throw new System.InvalidOperationException("Too many entries. Consider setting ZipOutputStream.EnableZip64.");
        }
        // Write out the header.
        //
        // If finishing, and encryption is in use, then we don't want to emit the
        // normal encryption header.  Signal that with a cycle=99 to turn off
        // encryption for zero-byte entries or directories.
        //
        // If finishing, then we know the stream length is zero.  Else, unknown
        // stream length.  Passing stream length == 0 allows an optimization so as
        // not to setup an encryption or deflation stream, when stream length is
        // zero.
        _currentEntry.WriteHeader(_outputStream, finishing ? 99 : 0);
        _currentEntry.StoreRelativeOffset();
        if (!_currentEntry.IsDirectory)
        {
            _currentEntry.WriteSecurityMetadata(_outputStream);
            _currentEntry.PrepOutputStream(_outputStream,
                                           finishing ? 0 : -1,
                                           out _outputCounter,
                                           out _encryptor,
                                           out _deflater,
                                           out _entryOutputStream);
        }
        _needToWriteEntryHeader = false;
    }
    private void _FinishCurrentEntry()
    {
        if (_currentEntry != null)
        {
            if (_needToWriteEntryHeader)
                _InitiateCurrentEntry(true); // an empty entry - no writes
            _currentEntry.FinishOutputStream(_outputStream, _outputCounter, _encryptor, _deflater, _entryOutputStream);
            _currentEntry.PostProcessOutput(_outputStream);
            // workitem 12964
            if (_currentEntry.OutputUsedZip64 != null)
                _anyEntriesUsedZip64 |= _currentEntry.OutputUsedZip64.Value;
            // reset all the streams
            _outputCounter = null; _encryptor = _deflater = null; _entryOutputStream = null;
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) // not called from finalizer
        {
            // handle pending exceptions
            if (!_exceptionPending)
            {
                _FinishCurrentEntry();
                _directoryNeededZip64 = ZipOutput.WriteCentralDirectoryStructure(_outputStream,
                                                                                 _entriesWritten.Values,
                                                                                 1, // _numberOfSegmentsForMostRecentSave,
                                                                                 _zip64,
                                                                                 Comment,
                                                                                 new ZipContainer(this));
                Stream wrappedStream;
                if (_outputStream is CountingStream cs)
                {
                    wrappedStream = cs.WrappedStream;
                    cs.Dispose();
                }
                else
                {
                    wrappedStream = _outputStream;
                }
                if (!_leaveUnderlyingStreamOpen)
                {
                    wrappedStream.Dispose();
                }
                _outputStream = null;
            }
        }
        _disposed = true;
    }
    public override bool CanRead { get { return false; } }
    public override bool CanSeek { get { return false; } }
    public override bool CanWrite { get { return true; } }
    public override long Length { get { throw new NotSupportedException(); } }
    public override long Position
    {
        get { return _outputStream.Position; }
        set { throw new NotSupportedException(); }
    }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("Read");
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("Seek");
    public override void SetLength(long value) => throw new NotSupportedException();
    private EncryptionAlgorithm _encryption;
    private ZipEntryTimestamp _timestamp;
    internal String _password;
    private String _comment;
    private Stream _outputStream;
    private ZipEntry _currentEntry;
    internal Zip64Option _zip64;
    private Dictionary<String, ZipEntry> _entriesWritten;
    private int _entryCount;
    private ZipOption _alternateEncodingUsage = ZipOption.Never;
    private System.Text.Encoding _alternateEncoding
        = System.Text.Encoding.GetEncoding("IBM437"); // default = IBM437
    private bool _leaveUnderlyingStreamOpen;
    private bool _disposed;
    private bool _exceptionPending; // **see note below
    private bool _anyEntriesUsedZip64, _directoryNeededZip64;
    private CountingStream _outputCounter;
    private Stream _encryptor;
    private Stream _deflater;
    private Ionic.Zlib.CrcCalculatorStream _entryOutputStream;
    private bool _needToWriteEntryHeader;
    private string _name;
    private bool _DontIgnoreCase;
    internal ParallelDeflateOutputStream ParallelDeflater;
    private long _ParallelDeflateThreshold;
    private int _maxBufferPairs = 16;
    // **Note regarding exceptions:
    // When ZipOutputStream is employed within a using clause, which
    // is the typical scenario, and an exception is thrown within
    // the scope of the using, Close()/Dispose() is invoked
    // implicitly before processing the initial exception.  In that
    // case, _exceptionPending is true, and we don't want to try to
    // write anything in the Close/Dispose logic.  Doing so can
    // cause additional exceptions that mask the original one. So,
    // the _exceptionPending flag is used to track that, and to
    // allow the original exception to be propagated to the
    // application without extra "noise."
}
internal class ZipContainer
{
    private readonly ZipFile _zf;
    private readonly ZipOutputStream _zos;
    private readonly ZipInputStream _zis;
    public ZipContainer(Object o)
    {
        _zf = (o as ZipFile);
        _zos = (o as ZipOutputStream);
        _zis = (o as ZipInputStream);
    }
    public ZipFile ZipFile
    {
        get { return _zf; }
    }
    public ZipOutputStream ZipOutputStream
    {
        get { return _zos; }
    }
    public string Name
    {
        get
        {
            if (_zf != null) return _zf.Name;
            return _zis != null ? throw new NotSupportedException() : _zos.Name;
        }
    }
    public string Password
    {
        get
        {
            if (_zf != null) return _zf._Password;
            return _zis != null ? _zis._Password : _zos._password;
        }
    }
    public Zip64Option Zip64
    {
        get
        {
            if (_zf != null) return _zf._zip64;
            return _zis != null ? throw new NotSupportedException() : _zos._zip64;
        }
    }
    public int BufferSize
    {
        get
        {
            if (_zf != null) return _zf.BufferSize;
            return _zis != null ? throw new NotSupportedException() : 0;
        }
    }
    public ParallelDeflateOutputStream ParallelDeflater
    {
        get
        {
            if (_zf != null) return _zf.ParallelDeflater;
            return _zis != null ? null : _zos.ParallelDeflater;
        }
        set
        {
            if (_zf != null) _zf.ParallelDeflater = value;
            else if (_zos != null) _zos.ParallelDeflater = value;
        }
    }
    public long ParallelDeflateThreshold
    {
        get
        {
            return _zf != null ? _zf.ParallelDeflateThreshold : _zos.ParallelDeflateThreshold;
        }
    }
    public int ParallelDeflateMaxBufferPairs
    {
        get
        {
            return _zf != null ? _zf.ParallelDeflateMaxBufferPairs : _zos.ParallelDeflateMaxBufferPairs;
        }
    }
    public int CodecBufferSize
    {
        get
        {
            if (_zf != null) return _zf.CodecBufferSize;
            return _zis != null ? _zis.CodecBufferSize : _zos.CodecBufferSize;
        }
    }
    public CompressionStrategy Strategy
    {
        get
        {
            return _zf != null ? _zf.Strategy : _zos.Strategy;
        }
    }
    public Zip64Option UseZip64WhenSaving
    {
        get
        {
            return _zf != null ? _zf.UseZip64WhenSaving : _zos.EnableZip64;
        }
    }
    public System.Text.Encoding AlternateEncoding
    {
        get
        {
            if (_zf != null) return _zf.AlternateEncoding;
            return _zos != null ? _zos.AlternateEncoding : null;
        }
    }
    public System.Text.Encoding DefaultEncoding
    {
        get
        {
            if (_zf != null) return ZipFile.DefaultEncoding;
            return _zos != null ? ZipOutputStream.DefaultEncoding : null;
        }
    }
    public ZipOption AlternateEncodingUsage
    {
        get
        {
            if (_zf != null) return _zf.AlternateEncodingUsage;
            if (_zos != null) return _zos.AlternateEncodingUsage;
            return ZipOption.Never; // n/a
        }
    }
    public Stream ReadStream
    {
        get
        {
            return _zf != null ? _zf.ReadStream : _zis.ReadStream;
        }
    }
}
