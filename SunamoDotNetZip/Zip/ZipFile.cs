namespace Ionic.Zip;

// ZipFile.cs
//
// Copyright (c) 2006-2010 Dino Chiesa
// All rights reserved.
//
// This module is part of DotNetZip, a zipfile class library.
// The class library reads and writes zip files, according to the format
// described by PKware, at:
// http://www.pkware.com/business_and_developers/developer/popups/appnote.txt
//
//
// There are other Zip class libraries available.
//
// - it is possible to read and write zip files within .NET via the J# runtime.
//   But some people don't like to install the extra DLL, which is no longer
//   supported by MS. And also, the J# libraries don't support advanced zip
//   features, like ZIP64, spanned archives, or AES encryption.
//
// - There are third-party GPL and LGPL libraries available. Some people don't
//   like the license, and some of them don't support all the ZIP features, like AES.
//
// - Finally, there are commercial tools (From ComponentOne, XCeed, etc).  But
//   some people don't want to incur the cost.
//
// This alternative implementation is **not** GPL licensed. It is free of cost, and
// does not require J#. It balances a good set of features, with ease of use and
// speed of performance.
//
// This code is released under the Microsoft Public License .
// See the License.txt for details.
//
//
// NB: This implementation originally relied on the
// System.IO.Compression.DeflateStream base class in the .NET Framework
// v2.0 base class library, but now includes a managed-code port of Zlib.
//
// Thu, 08 Oct 2009  17:04
//
using Interop = System.Runtime.InteropServices;
///
///
///
///
///
///
///
///
///
///
[Interop.GuidAttribute("ebc25cf6-9120-4283-b972-0e5520d00005")]
[Interop.ComVisible(true)]
[Interop.ClassInterface(Interop.ClassInterfaceType.AutoDispatch)]
public partial class ZipFile :
System.Collections.IEnumerable,
System.Collections.Generic.IEnumerable<ZipEntry>,
IDisposable
{
    #region public properties
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public bool FullScan
    {
        get;
        set;
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public bool SortEntriesBeforeSaving
    {
        get;
        set;
    }
    ///
    ///
    /// <example>
    /// <code lang="C#">
    /// using (var zip = new ZipFile())
    /// {
    ///     zip.AddDirectoryWillTraverseReparsePoints = false;
    ///     zip.AddDirectory(dirToZip,"fodder");
    ///     zip.Save(zipFileToCreate);
    /// }
    /// </code>
    /// </example>
    ///
    ///
    public bool AddDirectoryWillTraverseReparsePoints { get; set; }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// <example>
    /// This example shows how you might set a large buffer size for efficiency when
    /// dealing with zip entries that are larger than 1gb.
    /// <code lang="C#">
    /// using (ZipFile zip = new ZipFile())
    /// {
    ///     zip.SaveProgress += this.zip1_SaveProgress;
    ///     zip.AddDirectory(directoryToZip, "");
    ///     zip.UseZip64WhenSaving = Zip64Option.Always;
    ///     zip.BufferSize = 65536*8; // 65536 * 8 = 512k
    ///     zip.Save(ZipFileToCreate);
    /// }
    /// </code>
    /// </example>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public int BufferSize
    {
        get { return _BufferSize; }
        set { _BufferSize = value; }
    }
    ///
    /// <remarks>
    ///   <para>
    ///     When doing ZLIB or Deflate compression, the library fills a buffer,
    ///     then passes it to the compressor for compression. Then the library
    ///     reads out the compressed bytes. This happens repeatedly until there
    ///     is no more uncompressed data to compress. This property sets the
    ///     size of the buffer that will be used for chunk-wise compression. In
    ///     order for the setting to take effect, your application needs to set
    ///     this property before calling one of the <c>ZipFile.Save()</c>
    ///     overloads.
    ///   </para>
    ///   <para>
    ///     Setting this affects the performance and memory efficiency of
    ///     compression and decompression. For larger files, setting this to a
    ///     larger size may improve compression performance, but the exact
    ///     numbers vary depending on available memory, the size of the streams
    ///     you are compressing, and a bunch of other variables. I don't have
    ///     good firm recommendations on how to set it.  You'll have to test it
    ///     yourself. Or just leave it alone and accept the default.
    ///   </para>
    /// </remarks>
    ///
    public int CodecBufferSize
    {
        get;
        set;
    }
    ///
    ///
    ///
    ///
    /// </remarks>
    ///
    ///
    ///
    ///
    public bool FlattenFoldersOnExtract
    {
        get;
        set;
    }
    ///
    /// <remarks>
    ///   Set the Strategy used by the ZLIB-compatible compressor, when
    ///   compressing entries using the DEFLATE method. Different compression
    ///   strategies work better on different sorts of data. The strategy
    ///   parameter can affect the compression ratio and the speed of
    ///   compression but not the correctness of the compresssion.  For more
    ///   information see <see
    ///   cref="CompressionStrategy">CompressionStrategy</see>.
    /// </remarks>
    ///
    public CompressionStrategy Strategy
    {
        get { return _Strategy; }
        set { _Strategy = value; }
    }
    ///
    ///
    ///
    /// <para>
    ///   If you use the no-argument constructor, and you then explicitly set this
    ///   property, when you call <see cref="ZipFile.Save()"/>, this name will
    ///   specify the name of the zip file created.  Doing so is equivalent to
    ///   calling <see cref="ZipFile.Save(String)"/>.  When instantiating a
    ///   <c>ZipFile</c> by reading from a stream or byte array, the <c>Name</c>
    ///   property remains <c>null</c>.  When saving to a stream, the <c>Name</c>
    ///   property is implicitly set to <c>null</c>.
    /// </para>
    /// </remarks>
    ///
    ///
    ///
    public string Name
    {
        get { return _name; }
        set { _name = value; }
    }
    ///
    ///
    ///
    ///  <para>
    ///    If you do not set this property, the default compression level is used,
    ///    which normally gives a good balance of compression efficiency and
    ///    compression speed.  In some tests, using <c>BestCompression</c> can
    ///    double the time it takes to compress, while delivering just a small
    ///    increase in compression efficiency.  This behavior will vary with the
    ///    type of data you compress.  If you are in doubt, just leave this setting
    ///    alone, and accept the default.
    ///  </para>
    /// </remarks>
    ///
    ///
    ///
    public CompressionLevel CompressionLevel
    {
        get;
        set;
    }
    public Ionic.Zip.CompressionMethod CompressionMethod
    {
        get
        {
            return _compressionMethod;
        }
        set
        {
            _compressionMethod = value;
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    /// </remarks>
    ///
    ///
    ///
    ///
    ///
    ///
    public string Comment
    {
        get { return _Comment; }
        set
        {
            _Comment = value;
            _contentsChanged = true;
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// <seealso cref="ZipEntry.EmitTimesInWindowsFormatWhenSaving" />
    /// <seealso cref="EmitTimesInUnixFormatWhenSaving" />
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public bool EmitTimesInWindowsFormatWhenSaving
    {
        get
        {
            return _emitNtfsTimes;
        }
        set
        {
            _emitNtfsTimes = value;
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// <seealso cref="ZipEntry.EmitTimesInUnixFormatWhenSaving" />
    /// <seealso cref="EmitTimesInWindowsFormatWhenSaving" />
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public bool EmitTimesInUnixFormatWhenSaving
    {
        get
        {
            return _emitUnixTimes;
        }
        set
        {
            _emitUnixTimes = value;
        }
    }
    ///
    /// <remarks>
    ///   This is a <em>synthetic</em> property.  It returns true if the <see
    ///   cref="StatusMessageTextWriter"/> is non-null.
    /// </remarks>
    ///
    internal bool Verbose
    {
        get { return (_StatusMessageTextWriter != null); }
    }
    ///
    public bool ContainsEntry(string name) =>
        // workitem 12534
        RetrievalEntries.ContainsKey(SharedUtilities.NormalizePathForUseInZipFile(name));
    ///
    /// <remarks>
    ///   The default value is <c>false</c>, which means don't do case-sensitive
    ///   matching. In other words, retrieving zip["ReadMe.Txt"] is the same as
    ///   zip["readme.txt"].  It really makes sense to set this to <c>true</c> only
    ///   if you are not running on Windows, which has case-insensitive
    ///   filenames. But since this library is not built for non-Windows platforms,
    ///   in most cases you should just leave this property alone.
    /// </remarks>
    ///
    public bool CaseSensitiveRetrieval
    {
        get
        {
            return _CaseSensitiveRetrieval;
        }
        set
        {
            _CaseSensitiveRetrieval = value;
        }
    }
    private Dictionary<string, ZipEntry> RetrievalEntries
    {
        get { return CaseSensitiveRetrieval ? _entries : _entriesInsensitive; }
    }
    public bool IgnoreDuplicateFiles
    {
        get { return _IgnoreDuplicateFiles; }
        set { _IgnoreDuplicateFiles = value; }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// <para>
    ///   Rather than specify the encoding in a binary fashion using this flag, an
    ///   application can specify an arbitrary encoding via the <see
    ///   cref="ProvisionalAlternateEncoding"/> property.  Setting the encoding
    ///   explicitly when creating zip archives will result in non-compliant zip
    ///   files that, curiously, are fairly interoperable.  The challenge is, the
    ///   PKWare specification does not provide for a way to specify that an entry
    ///   in a zip archive uses a code page that is neither IBM437 nor UTF-8.
    ///   Therefore if you set the encoding explicitly when creating a zip archive,
    ///   you must take care upon reading the zip archive to use the same code page.
    ///   If you get it wrong, the behavior is undefined and may result in incorrect
    ///   filenames, exceptions, stomach upset, hair loss, and acne.
    /// </para>
    /// </remarks>
    /// <seealso cref="ProvisionalAlternateEncoding"/>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    [Obsolete("Beginning with v1.9.1.6 of DotNetZip, this property is obsolete.  It will be removed in a future version of the library. Your applications should  use AlternateEncoding and AlternateEncodingUsage instead.")]
    public bool UseUnicodeAsNecessary
    {
        get
        {
            return (_alternateEncoding == System.Text.Encoding.GetEncoding("UTF-8")) &&
                (_alternateEncodingUsage == ZipOption.AsNecessary);
        }
        set
        {
            if (value)
            {
                _alternateEncoding = System.Text.Encoding.GetEncoding("UTF-8");
                _alternateEncodingUsage = ZipOption.AsNecessary;
            }
            else
            {
                _alternateEncoding = Ionic.Zip.ZipFile.DefaultEncoding;
                _alternateEncodingUsage = ZipOption.Never;
            }
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// </remarks>
    /// <seealso cref="RequiresZip64"/>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public Zip64Option UseZip64WhenSaving
    {
        get
        {
            return _zip64;
        }
        set
        {
            _zip64 = value;
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    /// </remarks>
    /// <seealso cref="UseZip64WhenSaving"/>
    /// <seealso cref="OutputUsedZip64"/>
    ///
    ///
    ///
    ///
    ///
    ///
    public Nullable<bool> RequiresZip64
    {
        get
        {
            if (_entries.Count > 65534)
                return new Nullable<bool>(true);
            // If the <c>ZipFile</c> has not been saved or if the contents have changed, then
            // it is not known if ZIP64 is required.
            if (!_hasBeenSaved || _contentsChanged) return null;
            // Whether ZIP64 is required is knowable.
            foreach (ZipEntry e in _entries.Values)
            {
                if (e.RequiresZip64.Value) return new Nullable<bool>(true);
            }
            return new Nullable<bool>(false);
        }
    }
    ///
    ///
    ///
    ///
    /// </remarks>
    /// <seealso cref="UseZip64WhenSaving"/>
    /// <seealso cref="RequiresZip64"/>
    ///
    ///
    ///
    ///
    public Nullable<bool> OutputUsedZip64
    {
        get
        {
            return _OutputUsesZip64;
        }
    }
    ///
    /// <remarks>
    ///   This property will return null (Nothing in VB) if you've added an entry after reading
    ///   the zip file.
    /// </remarks>
    ///
    public Nullable<bool> InputUsesZip64
    {
        get
        {
            if (_entries.Count > 65534)
                return true;
            foreach (ZipEntry e in this)
            {
                // if any entry was added after reading the zip file, then the result is null
                if (e.Source != ZipEntrySource.ZipFile) return null;
                // if any entry read from the zip used zip64, then the result is true
                if (e._InputUsesZip64) return true;
            }
            return false;
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// <seealso cref="Ionic.Zip.ZipFile.DefaultEncoding">DefaultEncoding</seealso>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    [Obsolete("use AlternateEncoding instead.")]
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
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// </code>
    /// </example>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public TextWriter StatusMessageTextWriter
    {
        get { return _StatusMessageTextWriter; }
        set { _StatusMessageTextWriter = value; }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public String TempFileFolder
    {
        get { return _TempFileFolder; }
        set
        {
            _TempFileFolder = value;
            if (value == null) return;
            if (!Directory.Exists(value))
                throw new FileNotFoundException(String.Format("That directory ({0}) does not exist.", value));
        }
    }
    ///
    ///
    ///
    ///
    ///
    /// <seealso cref="Ionic.Zip.ZipFile.Encryption">ZipFile.Encryption</seealso>
    /// <seealso cref="Ionic.Zip.ZipEntry.Password">ZipEntry.Password</seealso>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public String Password
    {
        set
        {
            _Password = value;
            if (_Password == null)
            {
                Encryption = EncryptionAlgorithm.None;
            }
            else if (Encryption == EncryptionAlgorithm.None)
            {
                Encryption = EncryptionAlgorithm.PkzipWeak;
            }
        }
        private get
        {
            return _Password;
        }
    }
    ///
    ///
    /// <para>
    ///   This property has no effect when extracting to a stream, or when the file
    ///   to be extracted does not already exist.
    /// </para>
    /// </remarks>
    /// <seealso cref="Ionic.Zip.ZipEntry.ExtractExistingFile"/>
    ///
    ///
    public ExtractExistingFileAction ExtractExistingFile
    {
        get;
        set;
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// <seealso cref="Ionic.Zip.ZipEntry.ZipErrorAction"/>
    /// <seealso cref="Ionic.Zip.ZipFile.ZipError"/>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public ZipErrorAction ZipErrorAction
    {
        get
        {
            if (ZipError != null)
                _zipErrorAction = ZipErrorAction.InvokeErrorEvent;
            return _zipErrorAction;
        }
        set
        {
            _zipErrorAction = value;
            if (_zipErrorAction != ZipErrorAction.InvokeErrorEvent && ZipError != null)
                ZipError = null;
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// <seealso cref="Ionic.Zip.ZipFile.Password">ZipFile.Password</seealso>
    /// <seealso cref="Ionic.Zip.ZipEntry.Encryption">ZipEntry.Encryption</seealso>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public EncryptionAlgorithm Encryption
    {
        get
        {
            return _Encryption;
        }
        set
        {
            if (value == EncryptionAlgorithm.Unsupported)
                throw new InvalidOperationException("You may not set Encryption to that value.");
            _Encryption = value;
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    /// </remarks>
    ///
    ///
    ///
    ///
    ///
    ///
    public SetCompressionCallback SetCompression
    {
        get;
        set;
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// <seealso cref="NumberOfSegmentsForMostRecentSave"/>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public Int32 MaxOutputSegmentSize
    {
        get
        {
            return _maxOutputSegmentSize > Int32.MaxValue
                ? throw new ZipException("MaxOutputSegmentSize is too large, use MaxOutputSegmentSize64 instead.")
                : (Int32)_maxOutputSegmentSize;
        }
        set
        {
            if (value < 65536 && value != 0)
                throw new ZipException("The minimum acceptable segment size is 65536.");
            _maxOutputSegmentSize = value;
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// <seealso cref="NumberOfSegmentsForMostRecentSave"/>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public Int64 MaxOutputSegmentSize64
    {
        get
        {
            return _maxOutputSegmentSize;
        }
        set
        {
            if (value < 65536 && value != 0)
                throw new ZipException("The minimum acceptable segment size is 65536.");
            _maxOutputSegmentSize = value;
        }
    }
    public Int32 NumberOfSegmentsForMostRecentSave
    {
        get
        {
            return unchecked((Int32)_numberOfSegmentsForMostRecentSave + 1);
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public long ParallelDeflateThreshold
    {
        set
        {
            if ((value != 0) && (value != -1) && (value < 64 * 1024))
                throw new Exception("ParallelDeflateThreshold should be -1, 0, or > 65536");
            _ParallelDeflateThreshold = value;
        }
        get
        {
            return _ParallelDeflateThreshold;
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
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
    public override String ToString() => String.Format("ZipFile::{0}", Name);
    ///
    /// <remarks>
    ///   <para>
    ///     This property is exposed as a convenience.  Callers could also get the
    ///     version value by retrieving GetName().Version on the
    ///     System.Reflection.Assembly object pointing to the DotNetZip
    ///     assembly. But sometimes it is not clear which assembly is being loaded.
    ///     This property makes it clear.
    ///   </para>
    ///   <para>
    ///     This static property is primarily useful for diagnostic purposes.
    ///   </para>
    /// </remarks>
    ///
    public static System.Version LibraryVersion
    {
        get
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        }
    }
    internal void NotifyEntryChanged() => _contentsChanged = true;
    internal Stream StreamForDiskNumber(uint diskNumber)
    {
        if (diskNumber + 1 == this._diskNumberWithCd ||
            (diskNumber == 0 && this._diskNumberWithCd == 0))
        {
            //return (this.ReadStream as FileStream);
            return this.ReadStream;
        }
        return ZipSegmentedStream.ForReading(this._readName ?? this._name,
                                             diskNumber, _diskNumberWithCd);
    }
    // called by ZipEntry in ZipEntry.Extract(), when there is no stream set for the
    // ZipEntry.
    internal void Reset(bool whileSaving)
    {
        if (_JustSaved)
        {
            // read in the just-saved zip archive
            using (ZipFile x = new())
            {
                if (File.Exists(this._readName ?? this._name))
                {
                    // workitem 10735
                    x._readName = x._name = whileSaving
                        ? (this._readName ?? this._name)
                        : this._name;
                }
                else // if we just saved to a stream no file is available to read from
                {
                    if (_readstream.CanSeek)
                        _readstream.Seek(0, SeekOrigin.Begin);
                    x._readstream = _readstream;
                }
                x.AlternateEncoding = this.AlternateEncoding;
                x.AlternateEncodingUsage = this.AlternateEncodingUsage;
                ReadIntoInstance(x);
                // copy the contents of the entries.
                // cannot just replace the entries - the app may be holding them
                foreach (ZipEntry e1 in x)
                {
                    var e2 = this[e1.FileName];
                    if (e2 != null && !e2.IsChanged)
                    {
                        e2.CopyMetaData(e1);
                    }
                }
            }
            _JustSaved = false;
        }
    }
    #endregion
    #region Constructors
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public ZipFile(string fileName)
    {
        if (DefaultEncoding == null)
        {
            _alternateEncoding = System.Text.Encoding.UTF8;
            AlternateEncodingUsage = ZipOption.Always;
        }
        else
        {
            _alternateEncoding = DefaultEncoding;
        }
        try
        {
            _InitInstance(fileName, null);
        }
        catch (Exception e1)
        {
            throw new ZipException(String.Format("Could not read {0} as a zip file", fileName), e1);
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    /// <param name="fileName">The filename to use for the new zip archive.</param>
    /// <param name="encoding">The Encoding is used as the default alternate
    /// encoding for entries with filenames or comments that cannot be encoded
    /// with the IBM437 code page. </param>
    ///
    ///
    ///
    ///
    ///
    ///
    public ZipFile(string fileName, System.Text.Encoding encoding)
    {
        try
        {
            AlternateEncoding = encoding;
            AlternateEncodingUsage = ZipOption.Always;
            _InitInstance(fileName, null);
        }
        catch (Exception e1)
        {
            throw new ZipException(String.Format("{0} is not a valid zip file", fileName), e1);
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    /// <code lang="VB">
    /// Using zip As New ZipFile
    ///     ' Store all files found in the top level directory, into the zip archive.
    ///     ' note: this code does not recurse subdirectories!
    ///     Dim filenames As String() = System.IO.Directory.GetFiles(DirectoryToZip)
    ///     zip.AddFiles(filenames, "files")
    ///     zip.Save("Backup.zip")
    /// End Using
    /// </code>
    /// </example>
    ///
    ///
    ///
    ///
    ///
    ///
    public ZipFile()
    {
        if (DefaultEncoding == null)
        {
            _alternateEncoding = System.Text.Encoding.UTF8;
            AlternateEncodingUsage = ZipOption.Always;
        }
        else
        {
            _alternateEncoding = DefaultEncoding;
        }
        _InitInstance(null, null);
    }
    ///
    ///
    ///
    /// <param name="encoding">
    /// The Encoding is used as the default alternate encoding for entries with
    /// filenames or comments that cannot be encoded with the IBM437 code page.
    /// </param>
    ///
    ///
    ///
    public ZipFile(System.Text.Encoding encoding)
    {
        AlternateEncoding = encoding;
        AlternateEncodingUsage = ZipOption.Always;
        _InitInstance(null, null);
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// <param name="fileName">The filename to use for the new zip archive.</param>
    /// <param name="statusMessageWriter">A TextWriter to use for writing
    /// verbose status messages.</param>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public ZipFile(string fileName, TextWriter statusMessageWriter)
    {
        if (DefaultEncoding == null)
        {
            _alternateEncoding = System.Text.Encoding.UTF8;
            AlternateEncodingUsage = ZipOption.Always;
        }
        else
        {
            _alternateEncoding = DefaultEncoding;
        }
        try
        {
            _InitInstance(fileName, statusMessageWriter);
        }
        catch (Exception e1)
        {
            throw new ZipException(String.Format("{0} is not a valid zip file", fileName), e1);
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// <param name="fileName">The filename to use for the new zip archive.</param>
    /// <param name="statusMessageWriter">A TextWriter to use for writing verbose
    /// status messages.</param>
    /// <param name="encoding">
    /// The Encoding is used as the default alternate encoding for entries with
    /// filenames or comments that cannot be encoded with the IBM437 code page.
    /// </param>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public ZipFile(string fileName, TextWriter statusMessageWriter,
                   System.Text.Encoding encoding)
    {
        try
        {
            AlternateEncoding = encoding;
            AlternateEncodingUsage = ZipOption.Always;
            _InitInstance(fileName, statusMessageWriter);
        }
        catch (Exception e1)
        {
            throw new ZipException(String.Format("{0} is not a valid zip file", fileName), e1);
        }
    }
    ///
    ///
    ///
    ///
    /// </remarks>
    /// <param name="fileName">the name of the existing zip file to read in.</param>
    ///
    ///
    ///
    ///
    public void Initialize(string fileName)
    {
        try
        {
            _InitInstance(fileName, null);
        }
        catch (Exception e1)
        {
            throw new ZipException(String.Format("{0} is not a valid zip file", fileName), e1);
        }
    }
    private void _InitInstance(string zipFileName, TextWriter statusMessageWriter)
    {
        // create a new zipfile
        _name = zipFileName;
        _StatusMessageTextWriter = statusMessageWriter;
        _contentsChanged = true;
        AddDirectoryWillTraverseReparsePoints = true;  // workitem 8617
        CompressionLevel = CompressionLevel.Default;
        ParallelDeflateThreshold = 512 * 1024;
        // workitem 7685, 9868
        _entries = new Dictionary<string, ZipEntry>(StringComparer.Ordinal);
        _entriesInsensitive = new Dictionary<string, ZipEntry>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(_name))
        {
            if (FullScan)
                ReadIntoInstance_Orig(this);
            else
                ReadIntoInstance(this);
            this._fileAlreadyExists = true;
        }
        return;
    }
    #endregion
    #region Indexers and Collections
    private List<ZipEntry> ZipEntriesAsList
    {
        get
        {
            _zipEntriesAsList ??= new List<ZipEntry>(_entries.Values);
            return _zipEntriesAsList;
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public ZipEntry this[int ix]
    {
        // workitem 6402
        get
        {
            return ZipEntriesAsList[ix];
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public ZipEntry this[String fileName]
    {
        get
        {
            var entries = RetrievalEntries;
            var key = SharedUtilities.NormalizePathForUseInZipFile(fileName);
            if (entries.TryGetValue(key, out ZipEntry value2))
                return value2;
            // workitem 11056
            key = key.Replace("/", "\\");
            return entries.TryGetValue(key, out ZipEntry value3) ? value3 : null;
#if MESSY
                foreach (ZipEntry e in _entries.Values)
                {
                    if (this.CaseSensitiveRetrieval)
                    {
                        // check for the file match with a case-sensitive comparison.
                        if (e.FileName == fileName) return e;
                        // also check for equivalence
                        if (fileName.Replace("\\", "/") == e.FileName) return e;
                        if (e.FileName.Replace("\\", "/") == fileName) return e;
                        // check for a difference only in trailing slash
                        if (e.FileName.EndsWith("/"))
                        {
                            var fileNameNoSlash = e.FileName.Trim("/".ToCharArray());
                            if (fileNameNoSlash == fileName) return e;
                            // also check for equivalence
                            if (fileName.Replace("\\", "/") == fileNameNoSlash) return e;
                            if (fileNameNoSlash.Replace("\\", "/") == fileName) return e;
                        }
                    }
                    else
                    {
                        // check for the file match in a case-insensitive manner.
                        if (String.Compare(e.FileName, fileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                        // also check for equivalence
                        if (String.Compare(fileName.Replace("\\", "/"), e.FileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                        if (String.Compare(e.FileName.Replace("\\", "/"), fileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                        // check for a difference only in trailing slash
                        if (e.FileName.EndsWith("/"))
                        {
                            var fileNameNoSlash = e.FileName.Trim("/".ToCharArray());
                            if (String.Compare(fileNameNoSlash, fileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                            // also check for equivalence
                            if (String.Compare(fileName.Replace("\\", "/"), fileNameNoSlash, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                            if (String.Compare(fileNameNoSlash.Replace("\\", "/"), fileName, StringComparison.CurrentCultureIgnoreCase) == 0) return e;
                        }
                    }
                }
                return null;
#endif
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public System.Collections.Generic.ICollection<String> EntryFileNames
    {
        get
        {
            return _entries.Keys;
        }
    }
    ///
    ///
    /// <para>
    ///   If there are no entries in the current <c>ZipFile</c>, the value returned is a
    ///   non-null zero-element collection.  If there are entries in the zip file,
    ///   the elements are returned in no particular order.
    /// </para>
    /// <para>
    ///   This is the implied enumerator on the <c>ZipFile</c> class.  If you use a
    ///   <c>ZipFile</c> instance in a context that expects an enumerator, you will
    ///   get this collection.
    /// </para>
    /// </remarks>
    /// <seealso cref="EntriesSorted"/>
    ///
    ///
    public System.Collections.Generic.ICollection<ZipEntry> Entries
    {
        get
        {
            return _entries.Values;
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// <seealso cref="Entries"/>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public System.Collections.Generic.ICollection<ZipEntry> EntriesSorted
    {
        get
        {
            var coll = new System.Collections.Generic.List<ZipEntry>();
            foreach (var e in this.Entries)
            {
                coll.Add(e);
            }
            StringComparison sc = (CaseSensitiveRetrieval) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            coll.Sort((x, y) => { return String.Compare(x.FileName, y.FileName, sc); });
            return coll.AsReadOnly();
        }
    }
    public int Count
    {
        get
        {
            return _entries.Count;
        }
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public void RemoveEntry(ZipEntry entry)
    {
        //if (!_entries.Values.Contains(entry))
        //    throw new ArgumentException("The entry you specified does not exist in the zip archive.");
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        var path = SharedUtilities.NormalizePathForUseInZipFile(entry.FileName);
        _entries.Remove(path);
        if (!AnyCaseInsensitiveMatches(path))
            _entriesInsensitive.Remove(path);
        _zipEntriesAsList = null;
#if NOTNEEDED
            if (_direntries != null)
            {
                bool FoundAndRemovedDirEntry = false;
                foreach (ZipDirEntry de1 in _direntries)
                {
                    if (entry.FileName == de1.FileName)
                    {
                        _direntries.Remove(de1);
                        FoundAndRemovedDirEntry = true;
                        break;
                    }
                }
                if (!FoundAndRemovedDirEntry)
                    throw new BadStateException("The entry to be removed was not found in the directory.");
            }
#endif
        _contentsChanged = true;
    }
    private bool AnyCaseInsensitiveMatches(string path)
    {
        // this has to search _entries rather than _caseInsensitiveEntries because it's used to determine whether to update the latter
        foreach (var entry in _entries.Values)
        {
            if (String.Equals(entry.FileName, path, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public void RemoveEntry(String fileName)
    {
        string modifiedName = ZipEntry.NameInArchive(fileName, null);
        ZipEntry e = this[modifiedName] ?? throw new ArgumentException("The entry you specified was not found in the zip archive.");
        RemoveEntry(e);
    }
    #endregion
    #region Destructors and Disposers
    //         /// <summary>
    //         /// This is the class Destructor, which gets called implicitly when the instance
    //         /// is destroyed.  Because the <c>ZipFile</c> type implements IDisposable, this
    //         /// method calls Dispose(false).
    //         /// </summary>
    //         ~ZipFile()
    //         {
    //             // call Dispose with false.  Since we're in the
    //             // destructor call, the managed resources will be
    //             // disposed of anyways.
    //             Dispose(false);
    //         }
    ///
    ///
    ///
    /// <code lang="VB">
    /// Using zip As ZipFile = ZipFile.Read(zipfile)
    ///     Dim e As ZipEntry
    ///     For Each e In zip
    ///       If WantThisEntry(e.FileName) Then
    ///           zip.Extract(e.FileName, Console.OpenStandardOutput())
    ///       End If
    ///     Next
    /// End Using ' Dispose is implicity called here
    /// </code>
    /// </example>
    ///
    ///
    ///
    public void Dispose()
    {
        // dispose of the managed and unmanaged resources
        Dispose(true);
        // tell the GC that the Finalize process no longer needs
        // to be run for this object.
        GC.SuppressFinalize(this);
    }
    ///
    ///
    /// <param name="disposeManagedResources">
    ///   indicates whether the method should dispose streams or not.
    /// </param>
    ///
    ///
    protected virtual void Dispose(bool disposeManagedResources)
    {
        if (!this._disposed)
        {
            if (disposeManagedResources)
            {
                // dispose managed resources
                if (_ReadStreamIsOurs)
                {
                    if (_readstream != null)
                    {
                        // workitem 7704
                        _readstream.Dispose();
                        _readstream = null;
                    }
                }
                // only dispose the writestream if there is a backing file
                if ((_temporaryFileName != null) && (_name != null))
                    if (_writestream != null)
                    {
                        // workitem 7704
                        _writestream.Dispose();
                        _writestream = null;
                    }
                // workitem 10030
                if (this.ParallelDeflater != null)
                {
                    this.ParallelDeflater.Dispose();
                    this.ParallelDeflater = null;
                }
            }
            this._disposed = true;
        }
    }
    #endregion
    #region private properties
    internal Stream ReadStream
    {
        get
        {
            if (_readstream == null)
            {
                if (_readName != null || _name != null)
                {
                    _readstream = File.Open(_readName ?? _name,
                                            FileMode.Open,
                                            FileAccess.Read,
                                            FileShare.Read | FileShare.Write);
                    _ReadStreamIsOurs = true;
                }
            }
            return _readstream;
        }
    }
    private Stream WriteStream
    {
        // workitem 9763
        get
        {
            if (_writestream != null) return _writestream;
            if (_name == null) return _writestream;
            if (_maxOutputSegmentSize != 0)
            {
                _writestream = ZipSegmentedStream.ForWriting(this._name, _maxOutputSegmentSize);
                return _writestream;
            }
            SharedUtilities.CreateAndOpenUniqueTempFile(TempFileFolder ?? Path.GetDirectoryName(_name),
                                                        out _writestream,
                                                        out _temporaryFileName);
            return _writestream;
        }
        set
        {
            if (value != null)
                throw new ZipException("Cannot set the stream to a non-null value.");
            _writestream = null;
        }
    }
    #endregion
    #region private fields
    private TextWriter _StatusMessageTextWriter;
    private bool _CaseSensitiveRetrieval;
    private bool _IgnoreDuplicateFiles;
    private Stream _readstream;
    private Stream _writestream;
    private UInt16 _versionMadeBy;
    private UInt16 _versionNeededToExtract;
    private UInt32 _diskNumberWithCd;
    private Int64 _maxOutputSegmentSize;
    private UInt32 _numberOfSegmentsForMostRecentSave;
    private ZipErrorAction _zipErrorAction;
    private bool _disposed;
    //private System.Collections.Generic.List<ZipEntry> _entries;
    private System.Collections.Generic.Dictionary<String, ZipEntry> _entries;
    private System.Collections.Generic.Dictionary<String, ZipEntry> _entriesInsensitive;
    private List<ZipEntry> _zipEntriesAsList;
    private string _name;
    private string _readName;
    private string _Comment;
    internal string _Password;
    private bool _emitNtfsTimes = true;
    private bool _emitUnixTimes;
    private CompressionStrategy _Strategy = CompressionStrategy.Default;
    private Ionic.Zip.CompressionMethod _compressionMethod = Ionic.Zip.CompressionMethod.Deflate;
    private bool _fileAlreadyExists;
    private string _temporaryFileName;
    private bool _contentsChanged;
    private bool _hasBeenSaved;
    private String _TempFileFolder;
    private bool _ReadStreamIsOurs = true;
#if NET9_0_OR_GREATER
    private readonly Lock LOCK = new();
#else
    private readonly object LOCK = new();
#endif
    private bool _saveOperationCanceled;
    private bool _extractOperationCanceled;
    private bool _addOperationCanceled;
    private EncryptionAlgorithm _Encryption;
    private bool _JustSaved;
    private long _locEndOfCDS = -1;
    private uint _OffsetOfCentralDirectory;
    private Int64 _OffsetOfCentralDirectory64;
    private Nullable<bool> _OutputUsesZip64;
    internal bool _inExtractAll;
    private System.Text.Encoding _alternateEncoding = null;
    private ZipOption _alternateEncodingUsage = ZipOption.Never;
    private int _BufferSize = BufferSizeDefault;
    internal ParallelDeflateOutputStream ParallelDeflater;
    private long _ParallelDeflateThreshold;
    private int _maxBufferPairs = 16;
    internal Zip64Option _zip64 = Zip64Option.Never;
#pragma warning disable 649
    private readonly bool _SavingSfx;
#pragma warning restore 649
    public static readonly int BufferSizeDefault = 32768;
    #endregion
}
///
///
///
///
///
///
public enum Zip64Option
{
    Never = 0,
    AsNecessary = 1,
    Always
}
public enum ZipOption
{
    Never = 0,
    AsNecessary = 1,
    Always
}
enum AddOrUpdateAction
{
    AddOnly = 0,
    AddOrUpdate
}
// ==================================================================
//
// Information on the ZIP format:
//
// From
// http://www.pkware.com/documents/casestudies/APPNOTE.TXT
//
//  Overall .ZIP file format:
//
//     [local file header 1]
//     [file data 1]
//     [data descriptor 1]  ** sometimes
//     .
//     .
//     .
//     [local file header n]
//     [file data n]
//     [data descriptor n]   ** sometimes
//     [archive decryption header]
//     [archive extra data record]
//     [central directory]
//     [zip64 end of central directory record]
//     [zip64 end of central directory locator]
//     [end of central directory record]
//
// Local File Header format:
//         local file header signature ... 4 bytes  (0x04034b50)
//         version needed to extract ..... 2 bytes
//         general purpose bit field ..... 2 bytes
//         compression method ............ 2 bytes
//         last mod file time ............ 2 bytes
//         last mod file date............. 2 bytes
//         crc-32 ........................ 4 bytes
//         compressed size................ 4 bytes
//         uncompressed size.............. 4 bytes
//         file name length............... 2 bytes
//         extra field length ............ 2 bytes
//         file name                       varies
//         extra field                     varies
//
//
// Data descriptor:  (used only when bit 3 of the general purpose bitfield is set)
//         (although, I have found zip files where bit 3 is not set, yet this descriptor is present!)
//         local file header signature     4 bytes  (0x08074b50)  ** sometimes!!! Not always
//         crc-32                          4 bytes
//         compressed size                 4 bytes
//         uncompressed size               4 bytes
//
//
//   Central directory structure:
//
//       [file header 1]
//       .
//       .
//       .
//       [file header n]
//       [digital signature]
//
//
//       File header:  (This is a ZipDirEntry)
//         central file header signature   4 bytes  (0x02014b50)
//         version made by                 2 bytes
//         version needed to extract       2 bytes
//         general purpose bit flag        2 bytes
//         compression method              2 bytes
//         last mod file time              2 bytes
//         last mod file date              2 bytes
//         crc-32                          4 bytes
//         compressed size                 4 bytes
//         uncompressed size               4 bytes
//         file name length                2 bytes
//         extra field length              2 bytes
//         file comment length             2 bytes
//         disk number start               2 bytes
//         internal file attributes **     2 bytes
//         external file attributes ***    4 bytes
//         relative offset of local header 4 bytes
//         file name (variable size)
//         extra field (variable size)
//         file comment (variable size)
//
// ** The internal file attributes, near as I can tell,
// uses 0x01 for a file and a 0x00 for a directory.
//
// ***The external file attributes follows the MS-DOS file attribute byte, described here:
// at http://support.microsoft.com/kb/q125019/
// 0x0010 => directory
// 0x0020 => file
//
//
// End of central directory record:
//
//         end of central dir signature    4 bytes  (0x06054b50)
//         number of this disk             2 bytes
//         number of the disk with the
//         start of the central directory  2 bytes
//         total number of entries in the
//         central directory on this disk  2 bytes
//         total number of entries in
//         the central directory           2 bytes
//         size of the central directory   4 bytes
//         offset of start of central
//         directory with respect to
//         the starting disk number        4 bytes
//         .ZIP file comment length        2 bytes
//         .ZIP file comment       (variable size)
//
// date and time are packed values, as MSDOS did them
// time: bits 0-4 : seconds (divided by 2)
//            5-10: minute
//            11-15: hour
// date  bits 0-4 : day
//            5-8: month
//            9-15 year (since 1980)
//
// see http://msdn.microsoft.com/en-us/library/ms724274(VS.85).aspx