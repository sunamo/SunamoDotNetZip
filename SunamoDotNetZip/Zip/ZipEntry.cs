namespace Ionic.Zip;

// ZipEntry.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2006-2010 Dino Chiesa.
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
// Time-stamp: <2011-August-06 17:25:53>
//
// ------------------------------------------------------------------
//
// This module defines the ZipEntry class, which models the entries within a zip file.
//
// Created: Tue, 27 Mar 2007  15:30
//
// ------------------------------------------------------------------
using Interop = System.Runtime.InteropServices;
    [Interop.GuidAttribute("ebc25cf6-9120-4283-b972-0e5520d00004")]
    [Interop.ComVisible(true)]
    [Interop.ClassInterface(Interop.ClassInterfaceType.AutoDispatch)]  // AutoDual
    public partial class ZipEntry
    {
        public ZipEntry()
        {
            _CompressionMethod = (Int16)CompressionMethod.Deflate;
            _CompressionLevel = CompressionLevel.Default;
            _Encryption = EncryptionAlgorithm.None;
            _Source = ZipEntrySource.None;
#if NETCOREAPP2_0 || NETSTANDARD2_0
            AlternateEncoding = System.Text.CodePagesEncodingProvider.Instance.GetEncoding(1252);
#else
            AlternateEncoding = System.Text.Encoding.GetEncoding("IBM437");
#endif
            AlternateEncodingUsage = ZipOption.Never;
        }
        public DateTime LastModified
        {
            get { return _LastModified.ToLocalTime(); }
            set
            {
                _LastModified = (value.Kind == DateTimeKind.Unspecified)
                    ? DateTime.SpecifyKind(value, DateTimeKind.Local)
                    : value.ToLocalTime();
                _Mtime = Ionic.Zip.SharedUtilities.AdjustTime_Reverse(_LastModified).ToUniversalTime();
                _metadataChanged = true;
            }
        }
        public bool DontEmitLastModified
        {
            get { return _dontEmitLastModified; }
            set { _dontEmitLastModified = value; }
        }
        int BufferSize
        {
            get
            {
                return _container.BufferSize;
            }
        }
        public DateTime ModifiedTime
        {
            get { return _Mtime; }
            set
            {
                SetEntryTimes(_Ctime, _Atime, value);
            }
        }
        public DateTime AccessedTime
        {
            get { return _Atime; }
            set
            {
                SetEntryTimes(_Ctime, value, _Mtime);
            }
        }
        public DateTime CreationTime
        {
            get { return _Ctime; }
            set
            {
                SetEntryTimes(value, _Atime, _Mtime);
            }
        }
        public void SetEntryTimes(DateTime created, DateTime accessed, DateTime modified)
        {
            _ntfsTimesAreSet = true;
            if (created == _zeroHour && created.Kind == _zeroHour.Kind) created = _win32Epoch;
            if (accessed == _zeroHour && accessed.Kind == _zeroHour.Kind) accessed = _win32Epoch;
            if (modified == _zeroHour && modified.Kind == _zeroHour.Kind) modified = _win32Epoch;
            _Ctime = created.ToUniversalTime();
            _Atime = accessed.ToUniversalTime();
            _Mtime = modified.ToUniversalTime();
            _LastModified = _Mtime;
            if (!_emitUnixTimes && !_emitNtfsTimes)
                _emitNtfsTimes = true;
            _metadataChanged = true;
        }
        public bool EmitTimesInWindowsFormatWhenSaving
        {
            get
            {
                return _emitNtfsTimes;
            }
            set
            {
                _emitNtfsTimes = value;
                _metadataChanged = true;
            }
        }
        public bool EmitTimesInUnixFormatWhenSaving
        {
            get
            {
                return _emitUnixTimes;
            }
            set
            {
                _emitUnixTimes = value;
                _metadataChanged = true;
            }
        }
        public ZipEntryTimestamp Timestamp
        {
            get
            {
                return _timestamp;
            }
        }
        public System.IO.FileAttributes Attributes
        {
            // workitem 7071
            get { return (System.IO.FileAttributes)_ExternalFileAttrs; }
            set
            {
                _ExternalFileAttrs = (int)value;
                // Since the application is explicitly setting the attributes, overwriting
                // whatever was there, we will explicitly set the Version made by field.
                // workitem 7926 - "version made by" OS should be zero for compat with WinZip
                _VersionMadeBy = (0 << 8) + 45;  // v4.5 of the spec
                _metadataChanged = true;
            }
        }
        internal string LocalFileName
        {
            get { return _LocalFileName; }
        }
        public string FileName
        {
            get { return _FileNameInArchive; }
            set
            {
                if (_container != null && _container.ZipFile == null)
                    throw new ZipException("Cannot rename; this is not supported in ZipOutputStream/ZipInputStream.");
                // rename the entry!
                if (String.IsNullOrEmpty(value)) throw new ZipException("The FileName must be non empty and non-null.");
                var filename = ZipEntry.NameInArchive(value, null);
                // workitem 8180
                if (_FileNameInArchive == filename) return; // nothing to do
                if (_container != null)
                {
                    // workitem 8047 - when renaming, must remove old and then add a new entry
                    this._container.ZipFile.RemoveEntry(this);
                    this._container.ZipFile.InternalAddEntry(filename, this);
                }
                _FileNameInArchive = filename;
                _container?.ZipFile.NotifyEntryChanged();
                _metadataChanged = true;
            }
        }
        public Stream InputStream
        {
            get { return _sourceStream; }
            set
            {
                if (this._Source != ZipEntrySource.Stream)
                    throw new ZipException("You must not set the input stream for this entry.");
                _sourceWasJitProvided = true;
                _sourceStream = value;
            }
        }
        public bool InputStreamWasJitProvided
        {
            get { return _sourceWasJitProvided; }
        }
        public ZipEntrySource Source
        {
            get { return _Source; }
        }
        public Int16 VersionNeeded
        {
            get { return _VersionNeeded; }
        }
        public string Comment
        {
            get { return _Comment; }
            set
            {
                _Comment = value;
                _metadataChanged = true;
            }
        }
        public Nullable<bool> RequiresZip64
        {
            get
            {
                return _entryRequiresZip64;
            }
        }
        public Nullable<bool> OutputUsedZip64
        {
            get { return _OutputUsesZip64; }
        }
        public Int16 BitField
        {
            get { return _BitField; }
        }
        public CompressionMethod CompressionMethod
        {
            get { return (CompressionMethod)_CompressionMethod; }
            set
            {
                if (value == (CompressionMethod)_CompressionMethod) return; // nothing to do.
                if (value != CompressionMethod.None && value != CompressionMethod.Deflate
#if BZIP
                    && value != CompressionMethod.BZip2
#endif
                    )
                    throw new InvalidOperationException("Unsupported compression method.");
                // If the source is a zip archive and there was encryption on the
                // entry, changing the compression method is not supported.
                //                 if (this._Source == ZipEntrySource.ZipFile && _sourceIsEncrypted)
                //                     throw new InvalidOperationException("Cannot change compression method on encrypted entries read from archives.");
                _CompressionMethod = (Int16)value;
                if (_CompressionMethod == (Int16)Ionic.Zip.CompressionMethod.None)
                    _CompressionLevel = CompressionLevel.None;
                else if (CompressionLevel == CompressionLevel.None)
                    _CompressionLevel = CompressionLevel.Default;
                _container.ZipFile?.NotifyEntryChanged();
                _restreamRequiredOnSave = true;
            }
        }
        public CompressionLevel CompressionLevel
        {
            get
            {
                return _CompressionLevel;
            }
            set
            {
                if (_CompressionMethod != (short)CompressionMethod.Deflate &&
                    _CompressionMethod != (short)CompressionMethod.None)
                    return; // no effect
                if (value == CompressionLevel.Default &&
                    _CompressionMethod == (short)CompressionMethod.Deflate) return; // nothing to do
                _CompressionLevel = value;
                if (value == CompressionLevel.None &&
                    _CompressionMethod == (short)CompressionMethod.None)
                    return; // nothing more to do
                if (_CompressionLevel == CompressionLevel.None)
                    _CompressionMethod = (short)Ionic.Zip.CompressionMethod.None;
                else
                    _CompressionMethod = (short)Ionic.Zip.CompressionMethod.Deflate;
                _container.ZipFile?.NotifyEntryChanged();
                _restreamRequiredOnSave = true;
            }
        }
        public Int64 CompressedSize
        {
            get { return _CompressedSize; }
        }
        public Int64 UncompressedSize
        {
            get { return _UncompressedSize; }
        }
        public Double CompressionRatio
        {
            get
            {
            return UncompressedSize == 0 ? 0 : 100 * (1.0 - (1.0 * CompressedSize) / (1.0 * UncompressedSize));
        }
    }
        public Int32 Crc
        {
            get { return _Crc32; }
        }
        public bool IsDirectory
        {
            get { return _IsDirectory; }
        }
        public bool UsesEncryption
        {
            get { return (_Encryption_FromZipFile != EncryptionAlgorithm.None); }
        }
        public EncryptionAlgorithm Encryption
        {
            get
            {
                return _Encryption;
            }
            set
            {
                if (value == _Encryption) return; // no change
                if (value == EncryptionAlgorithm.Unsupported)
                    throw new InvalidOperationException("You may not set Encryption to that value.");
                // If the source is a zip archive and there was encryption
                // on the entry, this will not work. <XXX>
                //if (this._Source == ZipEntrySource.ZipFile && _sourceIsEncrypted)
                //    throw new InvalidOperationException("You cannot change the encryption method on encrypted entries read from archives.");
                _Encryption = value;
                _restreamRequiredOnSave = true;
                _container.ZipFile?.NotifyEntryChanged();
            }
        }
        public string Password
        {
            set
            {
                _Password = value;
                if (_Password == null)
                {
                    _Encryption = EncryptionAlgorithm.None;
                }
                else
                {
                    // We're setting a non-null password.
                    // For entries obtained from a zip file that are encrypted, we cannot
                    // simply restream (recompress, re-encrypt) the file data, because we
                    // need the old password in order to decrypt the data, and then we
                    // need the new password to encrypt.  So, setting the password is
                    // never going to work on an entry that is stored encrypted in a zipfile.
                    // But it is not en error to set the password, obviously: callers will
                    // set the password in order to Extract encrypted archives.
                    // If the source is a zip archive and there was previously no encryption
                    // on the entry, then we must re-stream the entry in order to encrypt it.
                    if (this._Source == ZipEntrySource.ZipFile && !_sourceIsEncrypted)
                        _restreamRequiredOnSave = true;
                    if (Encryption == EncryptionAlgorithm.None)
                    {
                        _Encryption = EncryptionAlgorithm.PkzipWeak;
                    }
                }
            }
            private get { return _Password; }
        }
        internal bool IsChanged
        {
            get
            {
                return _restreamRequiredOnSave | _metadataChanged;
            }
        }
        public ExtractExistingFileAction ExtractExistingFile
        {
            get;
            set;
        }
        public ZipErrorAction ZipErrorAction
        {
            get;
            set;
        }
        public bool IncludedInMostRecentSave
        {
            get
            {
                return !_skippedDuringSave;
            }
        }
        public SetCompressionCallback SetCompression
        {
            get;
            set;
        }
        [Obsolete("Beginning with v1.9.1.6 of DotNetZip, this property is obsolete.  It will be removed in a future version of the library. Your applications should  use AlternateEncoding and AlternateEncodingUsage instead.")]
        public bool UseUnicodeAsNecessary
        {
            get
            {
                return (AlternateEncoding == System.Text.Encoding.GetEncoding("UTF-8")) &&
                    (AlternateEncodingUsage == ZipOption.AsNecessary);
            }
            set
            {
                if (value)
                {
                    AlternateEncoding = System.Text.Encoding.GetEncoding("UTF-8");
                    AlternateEncodingUsage = ZipOption.AsNecessary;
                }
                else
                {
                    AlternateEncoding = Ionic.Zip.ZipFile.DefaultEncoding;
                    AlternateEncodingUsage = ZipOption.Never;
                }
            }
        }
        [Obsolete("This property is obsolete since v1.9.1.6. Use AlternateEncoding and AlternateEncodingUsage instead.", true)]
        public System.Text.Encoding ProvisionalAlternateEncoding
        {
            get; set;
        }
        public System.Text.Encoding AlternateEncoding
        {
            get; set;
        }
        public ZipOption AlternateEncodingUsage
        {
            get; set;
        }
        // /// <summary>
        // /// The text encoding actually used for this ZipEntry.
        // /// </summary>
        // ///
        // /// <remarks>
        // ///
        // /// <para>
        // ///   This read-only property describes the encoding used by the
        // ///   <c>ZipEntry</c>.  If the entry has been read in from an existing ZipFile,
        // ///   then it may take the value UTF-8, if the entry is coded to specify UTF-8.
        // ///   If the entry does not specify UTF-8, the typical case, then the encoding
        // ///   used is whatever the application specified in the call to
        // ///   <c>ZipFile.Read()</c>. If the application has used one of the overloads of
        // ///   <c>ZipFile.Read()</c> that does not accept an encoding parameter, then the
        // ///   encoding used is IBM437, which is the default encoding described in the
        // ///   ZIP specification.  </para>
        // ///
        // /// <para>
        // ///   If the entry is being created, then the value of ActualEncoding is taken
        // ///   according to the logic described in the documentation for <see
        // ///   cref="ZipFile.ProvisionalAlternateEncoding" />.  </para>
        // ///
        // /// <para>
        // ///   An application might be interested in retrieving this property to see if
        // ///   an entry read in from a file has used Unicode (UTF-8).  </para>
        // ///
        // /// </remarks>
        // ///
        // /// <seealso cref="ZipFile.ProvisionalAlternateEncoding" />
        // public System.Text.Encoding ActualEncoding
        // {
        //     get
        //     {
        //         return _actualEncoding;
        //     }
        // }
        internal static string NameInArchive(String filename, string directoryPathInArchive)
        {
        string result;
        if (directoryPathInArchive == null)
                result = filename;
            else
            {
                if (String.IsNullOrEmpty(directoryPathInArchive))
                {
                    result = Path.GetFileName(filename);
                }
                else
                {
                    // explicitly specify a pathname for this file
                    result = Path.Combine(directoryPathInArchive, Path.GetFileName(filename));
                }
            }
            //result = Path.GetFullPath(result);
            result = SharedUtilities.NormalizePathForUseInZipFile(result);
            return result;
        }
    // workitem 9073
    internal static ZipEntry CreateFromNothing(String nameInArchive) => Create(nameInArchive, ZipEntrySource.None, null, null);
    internal static ZipEntry CreateFromFile(String filename, string nameInArchive) => Create(nameInArchive, ZipEntrySource.FileSystem, filename, null);
    internal static ZipEntry CreateForStream(String entryName, Stream stream) => Create(entryName, ZipEntrySource.Stream, stream, null);
    internal static ZipEntry CreateForWriter(String entryName, WriteDelegate writeDelegate) => Create(entryName, ZipEntrySource.WriteDelegate, writeDelegate, null);
    internal static ZipEntry CreateForJitStreamProvider(string nameInArchive, OpenDelegate opener, CloseDelegate closer) => Create(nameInArchive, ZipEntrySource.JitStream, opener, closer);
    internal static ZipEntry CreateForZipOutputStream(string nameInArchive) => Create(nameInArchive, ZipEntrySource.ZipOutputStream, null, null);
    private static ZipEntry Create(string nameInArchive, ZipEntrySource source, Object arg1, Object arg2)
        {
            if (String.IsNullOrEmpty(nameInArchive))
                throw new Ionic.Zip.ZipException("The entry name must be non-null and non-empty.");
        ZipEntry entry = new()
        {
            // workitem 7071
            // workitem 7926 - "version made by" OS should be zero for compat with WinZip
            _VersionMadeBy = (0 << 8) + 45, // indicates the attributes are FAT Attributes, and v4.5 of the spec
            _Source = source
        };
        entry._Mtime = entry._Atime = entry._Ctime = DateTime.UtcNow;
            if (source == ZipEntrySource.Stream)
            {
                entry._sourceStream = (arg1 as Stream);         // may  or may not be null
            }
            else if (source == ZipEntrySource.WriteDelegate)
            {
                entry._WriteDelegate = (arg1 as WriteDelegate); // may  or may not be null
            }
            else if (source == ZipEntrySource.JitStream)
            {
                entry._OpenDelegate = (arg1 as OpenDelegate);   // may  or may not be null
                entry._CloseDelegate = (arg2 as CloseDelegate); // may  or may not be null
            }
            else if (source == ZipEntrySource.ZipOutputStream)
            {
            }
            // workitem 9073
            else if (source == ZipEntrySource.None)
            {
                // make this a valid value, for later.
                entry._Source = ZipEntrySource.FileSystem;
            }
            else
            {
                String filename = (arg1 as String);   // must not be null
                if (String.IsNullOrEmpty(filename))
                    throw new Ionic.Zip.ZipException("The filename must be non-null and non-empty.");
                try
                {
                    // The named file may or may not exist at this time.  For
                    // example, when adding a directory by name.  We test existence
                    // when necessary: when saving the ZipFile, or when getting the
                    // attributes, and so on.
                    // workitem 6878??
                    entry._Mtime = File.GetLastWriteTime(filename).ToUniversalTime();
                    entry._Ctime = File.GetCreationTime(filename).ToUniversalTime();
                    entry._Atime = File.GetLastAccessTime(filename).ToUniversalTime();
                    // workitem 7071
                    // can only get attributes on files that exist.
                    if (File.Exists(filename) || Directory.Exists(filename))
                        entry._ExternalFileAttrs = (int)File.GetAttributes(filename);
                    entry._ntfsTimesAreSet = true;
                    entry._LocalFileName = Path.GetFullPath(filename); // workitem 8813
                }
                catch (System.IO.PathTooLongException ptle)
                {
                    // workitem 14035
                    var msg = String.Format("The path is too long, filename={0}",
                                            filename);
                    throw new ZipException(msg, ptle);
                }
            }
            entry._LastModified = entry._Mtime;
            entry._FileNameInArchive = SharedUtilities.NormalizePathForUseInZipFile(nameInArchive);
            // We don't actually slurp in the file data until the caller invokes Write on this entry.
            return entry;
        }
        public ZipEntry CloneForNewZipFile(ZipFile newZipFile)
        {
            if (this.Source != ZipEntrySource.ZipFile)
            {
                throw new InvalidOperationException("The entry you are trying to add wasn't loaded from a zip file.");
            }
            if (this.IsChanged)
            {
                throw new InvalidOperationException("The entry you are trying to add was modified.");
            }
            if (this.IsDirectory)
            {
                throw new InvalidOperationException("The entry you are trying to add is a directory.");
            }
            if (!this.CompressionMethod.Equals(newZipFile.CompressionMethod))
            {
                throw new InvalidOperationException("The entry you are trying to add uses another compression method.");
            }
            if (this.ArchiveStream is ZipSegmentedStream)
            {
                throw new InvalidOperationException("The entry you are trying to add is from a multi-part zip.");
            }
            // Create a clone of the current entry.
            var clone = new ZipEntry
            {
                __FileDataPosition = this.__FileDataPosition,
                _actualEncoding = this._actualEncoding,
                _archiveStream = this._archiveStream,
                _Atime = this._Atime,
                _Comment = this._Comment,
                _CompressedSize = this._CompressedSize,
                _CompressionLevel = this._CompressionLevel,
                _CompressionMethod = this._CompressionMethod,
                _Crc32 = this._Crc32,
                _Ctime = this._Ctime,
                _emitNtfsTimes = this._emitNtfsTimes,
                _emitUnixTimes = this._emitUnixTimes,
                _Encryption = this._Encryption,
                _Encryption_FromZipFile = this._Encryption_FromZipFile,
                _entryRequiresZip64 = this._entryRequiresZip64,
                _FileNameInArchive = this._FileNameInArchive,
                _LastModified = this._LastModified,
                _LengthOfHeader = this._LengthOfHeader,
                _metadataChanged = this._metadataChanged,
                _Mtime = this._Mtime,
                _ntfsTimesAreSet = this._ntfsTimesAreSet,
                _Password = this._Password,
                _presumeZip64 = this._presumeZip64,
                _RelativeOffsetOfLocalHeader = this._RelativeOffsetOfLocalHeader,
                _restreamRequiredOnSave = this._restreamRequiredOnSave,
                _sourceIsEncrypted = this._sourceIsEncrypted,
                _TimeBlob = this._TimeBlob,
                _timestamp = this._timestamp,
                _UncompressedSize = this._UncompressedSize,
                _VersionMadeBy = this._VersionMadeBy,
                _VersionNeeded = this._VersionNeeded,
                AlternateEncoding = this.AlternateEncoding,
                AlternateEncodingUsage = this.AlternateEncodingUsage,
                ExtractExistingFile = this.ExtractExistingFile,
                SetCompression = this.SetCompression,
                ZipErrorAction = this.ZipErrorAction,
                _Source = this._Source
            };
            return clone;
        }
        internal void MarkAsDirectory()
        {
            _IsDirectory = true;
            // workitem 6279
            if (!_FileNameInArchive.EndsWith("/"))
                _FileNameInArchive += "/";
        }
        public bool IsText
        {
            // workitem 7801
            get { return _IsText; }
            set { _IsText = value; }
        }
    public override String ToString() => String.Format("ZipEntry::{0}", FileName);
    internal Stream ArchiveStream
        {
            get
            {
                if (_archiveStream == null)
                {
                    if (_container.ZipFile != null)
                    {
                        var zf = _container.ZipFile;
                        zf.Reset(false);
                        _archiveStream = zf.StreamForDiskNumber(_diskNumber);
                    }
                    else
                    {
                        _archiveStream = _container.ZipOutputStream.OutputStream;
                    }
                }
                return _archiveStream;
            }
        }
        private void SetFdpLoh()
        {
            // The value for FileDataPosition has not yet been set.
            // Therefore, seek to the local header, and figure the start of file data.
            // workitem 8098: ok (restore)
            long origPosition = this.ArchiveStream.Position;
            try
            {
                this.ArchiveStream.Seek(this._RelativeOffsetOfLocalHeader, SeekOrigin.Begin);
            }
            catch (IOException exc1)
            {
                var description = String.Format("Exception seeking  entry({0}) offset(0x{1:X8}) len(0x{2:X8})",
                                                   this.FileName, this._RelativeOffsetOfLocalHeader,
                                                   this.ArchiveStream.Length);
                throw new BadStateException(description, exc1);
            }
            byte[] block = new byte[30];
            this.ArchiveStream.Read(block, 0, block.Length);
            // At this point we could verify the contents read from the local header
            // with the contents read from the central header.  We could, but don't need to.
            // So we won't.
            Int16 filenameLength = (short)(block[26] + block[27] * 256);
            Int16 extraFieldLength = (short)(block[28] + block[29] * 256);
            // Console.WriteLine("  pos  0x{0:X8} ({0})", this.ArchiveStream.Position);
            // Console.WriteLine("  seek 0x{0:X8} ({0})", filenameLength + extraFieldLength);
            this.ArchiveStream.Seek(filenameLength + extraFieldLength, SeekOrigin.Current);
            this._LengthOfHeader = 30 + extraFieldLength + filenameLength +
                GetLengthOfCryptoHeaderBytes(_Encryption_FromZipFile);
            // Console.WriteLine("  ROLH  0x{0:X8} ({0})", _RelativeOffsetOfLocalHeader);
            // Console.WriteLine("  LOH   0x{0:X8} ({0})", _LengthOfHeader);
            // workitem 8098: ok (arithmetic)
            this.__FileDataPosition = _RelativeOffsetOfLocalHeader + _LengthOfHeader;
            // Console.WriteLine("  FDP   0x{0:X8} ({0})", __FileDataPosition);
            // restore file position:
            // workitem 8098: ok (restore)
            this.ArchiveStream.Seek(origPosition, SeekOrigin.Begin);
        }
#if AESCRYPTO
        private static int GetKeyStrengthInBits(EncryptionAlgorithm a)
        {
            if (a == EncryptionAlgorithm.WinZipAes256) return 256;
            else if (a == EncryptionAlgorithm.WinZipAes128) return 128;
            return -1;
        }
#endif
        internal static int GetLengthOfCryptoHeaderBytes(EncryptionAlgorithm a)
        {
            //if ((_BitField & 0x01) != 0x01) return 0;
            if (a == EncryptionAlgorithm.None) return 0;
#if AESCRYPTO
            if (a == EncryptionAlgorithm.WinZipAes128 ||
                a == EncryptionAlgorithm.WinZipAes256)
            {
                int KeyStrengthInBits = GetKeyStrengthInBits(a);
                int sizeOfSaltAndPv = ((KeyStrengthInBits / 8 / 2) + 2);
                return sizeOfSaltAndPv;
            }
#endif
        return a == EncryptionAlgorithm.PkzipWeak ? 12 : throw new ZipException("internal error");
    }
    internal long FileDataPosition
        {
            get
            {
                if (__FileDataPosition == -1)
                    SetFdpLoh();
                return __FileDataPosition;
            }
        }
        private int LengthOfHeader
        {
            get
            {
                if (_LengthOfHeader == 0)
                    SetFdpLoh();
                return _LengthOfHeader;
            }
        }
        private ZipCrypto _zipCrypto_forExtract;
        private ZipCrypto _zipCrypto_forWrite;
#if AESCRYPTO
        private WinZipAesCrypto _aesCrypto_forExtract;
        private WinZipAesCrypto _aesCrypto_forWrite;
        private Int16 _WinZipAesMethod;
#endif
        internal DateTime _LastModified;
        private bool _dontEmitLastModified;
        private DateTime _Mtime, _Atime, _Ctime;  // workitem 6878: NTFS quantities
        private bool _ntfsTimesAreSet;
        private bool _emitNtfsTimes = true;
        private bool _emitUnixTimes;  // by default, false
        private readonly bool _TrimVolumeFromFullyQualifiedPaths = true;  // by default, trim them.
        internal string _LocalFileName;
        private string _FileNameInArchive;
        internal Int16 _VersionNeeded;
        internal Int16 _BitField;
        internal Int16 _CompressionMethod;
        private Int16 _CompressionMethod_FromZipFile;
        private CompressionLevel _CompressionLevel;
        internal string _Comment;
        private bool _IsDirectory;
        private byte[] _CommentBytes;
        internal Int64 _CompressedSize;
        internal Int64 _CompressedFileDataSize; // CompressedSize less 12 bytes for the encryption header, if any
        internal Int64 _UncompressedSize;
        internal Int32 _TimeBlob;
        private bool _crcCalculated;
        internal Int32 _Crc32;
        internal byte[] _Extra;
        private bool _metadataChanged;
        private bool _restreamRequiredOnSave;
        private bool _sourceIsEncrypted;
        private bool _skippedDuringSave;
        private UInt32 _diskNumber;
#if NETSTANDARD2_0 || NETCOREAPP2_0
        private static System.Text.Encoding ibm437 = System.Text.CodePagesEncodingProvider.Instance.GetEncoding(1252);
#else
        private static readonly System.Text.Encoding ibm437 = System.Text.Encoding.GetEncoding("IBM437");
#endif
        //private System.Text.Encoding _provisionalAlternateEncoding = System.Text.Encoding.GetEncoding("IBM437");
        private System.Text.Encoding _actualEncoding;
        internal ZipContainer _container;
        private long __FileDataPosition = -1;
        private byte[] _EntryHeader;
        internal Int64 _RelativeOffsetOfLocalHeader;
        private Int64 _future_ROLH;
        private Int64 _TotalEntrySize;
        private int _LengthOfHeader;
        private int _LengthOfTrailer;
        internal bool _InputUsesZip64;
        private UInt32 _UnsupportedAlgorithmId;
        internal string _Password;
        internal ZipEntrySource _Source;
        internal EncryptionAlgorithm _Encryption;
        internal EncryptionAlgorithm _Encryption_FromZipFile;
        internal byte[] _WeakEncryptionHeader;
        internal Stream _archiveStream;
        private Stream _sourceStream;
        private Nullable<Int64> _sourceStreamOriginalPosition;
        private bool _sourceWasJitProvided;
        private bool _ioOperationCanceled;
        private bool _presumeZip64;
        private Nullable<bool> _entryRequiresZip64;
        private Nullable<bool> _OutputUsesZip64;
        private bool _IsText; // workitem 7801
        private ZipEntryTimestamp _timestamp;
        private static readonly System.DateTime _unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly System.DateTime _win32Epoch = System.DateTime.FromFileTimeUtc(0L);
        private static readonly System.DateTime _zeroHour = new(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private WriteDelegate _WriteDelegate;
        private OpenDelegate _OpenDelegate;
        private CloseDelegate _CloseDelegate;
        // summary
        // The default size of the IO buffer for ZipEntry instances. Currently it is 8192 bytes.
        // summary
        //public const int IO_BUFFER_SIZE_DEFAULT = 8192; // 0x8000; // 0x4400
    }
    [Flags]
    public enum ZipEntryTimestamp
    {
        None = 0,
        DOS = 1,
        Windows = 2,
        Unix = 4,
        InfoZip1 = 8,
    }
    public enum CompressionMethod
    {
        None = 0,
        Deflate = 8,
        Deflate64 = 9,
#if BZIP
        BZip2 = 12,
#endif
    }