namespace Ionic.Zip;

// ZipEntry.Extract.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2011 Dino Chiesa
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
// Time-stamp: <2011-August-06 18:08:21>
//
// ------------------------------------------------------------------
//
// This module defines logic for Extract methods on the ZipEntry class.
//
// ------------------------------------------------------------------
public partial class ZipEntry
{
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public void Extract() => InternalExtractToBaseDir(".", null, _container, _Source, FileName);
    ///
    ///
    public void Extract(ExtractExistingFileAction extractExistingFile)
    {
        ExtractExistingFile = extractExistingFile;
        InternalExtractToBaseDir(".", null, _container, _Source, FileName);
    }
    ///
    ///
    ///
    public void Extract(Stream stream) => InternalExtractToStream(stream, null, _container, _Source, FileName);
    ///
    ///
    ///
    ///
    ///
    ///
    public void Extract(string baseDirectory) => InternalExtractToBaseDir(baseDirectory, null, _container, _Source, FileName);
    ///
    ///
    ///
    ///
    public void Extract(string baseDirectory, ExtractExistingFileAction extractExistingFile)
    {
        ExtractExistingFile = extractExistingFile;
        InternalExtractToBaseDir(baseDirectory, null, _container, _Source, FileName);
    }
    ///
    ///
    ///
    ///
    ///
    ///
    public void ExtractWithPassword(string password) => InternalExtractToBaseDir(".", password, _container, _Source, FileName);
    ///
    ///
    ///
    ///
    public void ExtractWithPassword(string baseDirectory, string password) => InternalExtractToBaseDir(baseDirectory, password, _container, _Source, FileName);
    ///
    ///
    ///
    public void ExtractWithPassword(ExtractExistingFileAction extractExistingFile, string password)
    {
        ExtractExistingFile = extractExistingFile;
        InternalExtractToBaseDir(".", password, _container, _Source, FileName);
    }
    ///
    ///
    ///
    ///
    public void ExtractWithPassword(string baseDirectory, ExtractExistingFileAction extractExistingFile, string password)
    {
        ExtractExistingFile = extractExistingFile;
        InternalExtractToBaseDir(baseDirectory, password, _container, _Source, FileName);
    }
    ///
    ///
    ///
    public void ExtractWithPassword(Stream stream, string password) => InternalExtractToStream(stream, password, _container, _Source, FileName);
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public CrcCalculatorStream OpenReader()
    {
        // workitem 10923
        if (_container.ZipFile == null)
            throw new InvalidOperationException("Use OpenReader() only with ZipFile.");
        // use the entry password if it is non-null,
        // else use the zipfile password, which is possibly null
        return InternalOpenReader(_Password ?? _container.Password);
    }
    ///
    ///
    public Ionic.Zlib.CrcCalculatorStream OpenReader(string password) =>
        // workitem 10923
        _container.ZipFile == null
            ? throw new InvalidOperationException("Use OpenReader() only with ZipFile.")
            : InternalOpenReader(password);
    internal CrcCalculatorStream InternalOpenReader(string password)
    {
        ValidateCompression(_CompressionMethod_FromZipFile, FileName, GetUnsupportedCompressionMethod(_CompressionMethod));
        ValidateEncryption(Encryption, FileName, _UnsupportedAlgorithmId);
        SetupCryptoForExtract(password);
        // workitem 7958
        if (this._Source != ZipEntrySource.ZipFile)
            throw new BadStateException("You must call ZipFile.Save before calling OpenReader");
        // LeftToRead is a count of bytes remaining to be read (out)
        // from the stream AFTER decompression and decryption.
        // It is the uncompressed size, unless ... there is no compression in which
        // case ...?  :< I'm not sure why it's not always UncompressedSize
        var leftToRead = (_CompressionMethod_FromZipFile == (short)CompressionMethod.None)
            ? _CompressedFileDataSize
            : UncompressedSize;
        this.ArchiveStream.Seek(this.FileDataPosition, SeekOrigin.Begin);
        _inputDecryptorStream = GetExtractDecryptor(ArchiveStream);
        var input3 = GetExtractDecompressor(_inputDecryptorStream);
        return new CrcCalculatorStream(input3, leftToRead);
    }
    void OnExtractProgress(Int64 bytesWritten, Int64 totalBytesToWrite)
    {
        if (_container.ZipFile != null)
            _ioOperationCanceled = _container.ZipFile.OnExtractBlock(this, bytesWritten, totalBytesToWrite);
    }
    static void OnBeforeExtract(ZipEntry zipEntryInstance, string path, ZipFile zipFile)
    {
        // When in the context of a ZipFile.ExtractAll, the events are generated from
        // the ZipFile method, not from within the ZipEntry instance. (why?)
        // Therefore we suppress the events originating from the ZipEntry method.
        if (zipFile == null) return;
        if (zipFile._inExtractAll) return;
        // returned boolean is always ignored for all callers of OnBeforeExtract
        zipFile.OnSingleEntryExtract(zipEntryInstance, path, true);
    }
    private void OnAfterExtract(string path)
    {
        // When in the context of a ZipFile.ExtractAll, the events are generated from
        // the ZipFile method, not from within the ZipEntry instance. (why?)
        // Therefore we suppress the events originating from the ZipEntry method.
        if (_container.ZipFile == null) return;
        if (_container.ZipFile._inExtractAll) return;
        _container.ZipFile.OnSingleEntryExtract(this, path, false);
    }
    private void OnExtractExisting(string path)
    {
        if (_container.ZipFile != null)
            _ioOperationCanceled = _container.ZipFile.OnExtractExisting(this, path);
    }
    private static void ReallyDelete(string fileName)
    {
        // workitem 7881
        // reset ReadOnly bit if necessary
        if ((File.GetAttributes(fileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            File.SetAttributes(fileName, FileAttributes.Normal);
        File.Delete(fileName);
    }
    void WriteStatus(string format, params Object[] args)
    {
        if (_container.ZipFile != null && _container.ZipFile.Verbose)
            _container.ZipFile.StatusMessageTextWriter.WriteLine(format, args);
    }
    void InternalExtractToBaseDir(string baseDir, string password, ZipContainer zipContainer, ZipEntrySource zipEntrySource, string fileName)
    {
        if (baseDir == null) throw new ArgumentNullException(nameof(baseDir));
        // workitem 7958
        if (zipContainer == null)
            throw new BadStateException("This entry is an orphan");
        // workitem 10355
        if (zipContainer.ZipFile == null)
            throw new InvalidOperationException("Use Extract() only with ZipFile.");
        zipContainer.ZipFile.Reset(false);
        if (zipEntrySource != ZipEntrySource.ZipFile)
            throw new BadStateException("You must call ZipFile.Save before calling any Extract method");
        OnBeforeExtract(this, baseDir, zipContainer.ZipFile);
        _ioOperationCanceled = false;
        var fileExistsBeforeExtraction = false;
        var checkLaterForResetDirTimes = false;
        string targetFileName = null;
        try
        {
            ValidateCompression(_CompressionMethod_FromZipFile, fileName, GetUnsupportedCompressionMethod(_CompressionMethod));
            ValidateEncryption(Encryption, fileName, _UnsupportedAlgorithmId);
            if (IsDoneWithOutputToBaseDir(baseDir, out targetFileName))
            {
                WriteStatus("extract dir {0}...", targetFileName);
                // if true, then the entry was a directory and has been created.
                // We need to fire the Extract Event.
                OnAfterExtract(baseDir);
                return;
            }
            // workitem 10639
            // do we want to extract to a regular filesystem file?
            // Check for extracting to a previously existing file. The user
            // can specify bejavior for that case: overwrite, don't
            // overwrite, and throw.  Also, if the file exists prior to
            // extraction, it affects exception handling: whether to delete
            // the target of extraction or not. This check needs to be done
            // before the password check is done, because password check may
            // throw a BadPasswordException, which triggers the catch,
            // wherein the existing file may be deleted if not flagged as
            // pre-existing.
            if (File.Exists(targetFileName))
            {
                fileExistsBeforeExtraction = true;
                int rc = CheckExtractExistingFile(baseDir, targetFileName);
                if (rc == 2) goto ExitTry; // cancel
                if (rc == 1) return; // do not overwrite
            }
            // If no password explicitly specified, use the password on the entry itself,
            // or on the zipfile itself.
            if (_Encryption_FromZipFile != EncryptionAlgorithm.None)
                EnsurePassword(password);
            // set up the output stream
            var tmpName = SharedUtilities.InternalGetTempFileName();
            var tmpPath = Path.Combine(Path.GetDirectoryName(targetFileName), tmpName);
            WriteStatus("extract file {0}...", targetFileName);
            using (var output = OpenFileStream(tmpPath, ref checkLaterForResetDirTimes))
            {
                if (ExtractToStream(ArchiveStream, output, Encryption, _Crc32))
                    goto ExitTry;
                output.Close();
            }
            MoveFileInPlace(fileExistsBeforeExtraction, targetFileName, tmpPath, checkLaterForResetDirTimes);
            OnAfterExtract(baseDir);
        ExitTry:;
        }
        catch (Exception)
        {
            _ioOperationCanceled = true;
            throw;
        }
        finally
        {
            if (_ioOperationCanceled && targetFileName != null)
            {
                // An exception has occurred. If the file exists, check
                // to see if it existed before we tried extracting.  If
                // it did not, attempt to remove the target file. There
                // is a small possibility that the existing file has
                // been extracted successfully, overwriting a previously
                // existing file, and an exception was thrown after that
                // but before final completion (setting times, etc). In
                // that case the file will remain, even though some
                // error occurred.  Nothing to be done about it.
                if (File.Exists(targetFileName) && !fileExistsBeforeExtraction)
                    File.Delete(targetFileName);
            }
        }
    }
    void InternalExtractToStream(Stream outStream, string password, ZipContainer zipContainer, ZipEntrySource zipEntrySource, string fileName)
    {
        // workitem 7958
        if (zipContainer == null)
            throw new BadStateException("This entry is an orphan");
        // workitem 10355
        if (zipContainer.ZipFile == null)
            throw new InvalidOperationException("Use Extract() only with ZipFile.");
        zipContainer.ZipFile.Reset(false);
        if (zipEntrySource != ZipEntrySource.ZipFile)
            throw new BadStateException("You must call ZipFile.Save before calling any Extract method");
        OnBeforeExtract(this, null, zipContainer.ZipFile);
        _ioOperationCanceled = false;
        try
        {
            ValidateCompression(_CompressionMethod_FromZipFile, fileName, GetUnsupportedCompressionMethod(_CompressionMethod));
            ValidateEncryption(Encryption, fileName, _UnsupportedAlgorithmId);
            if (IsDoneWithOutputToStream())
            {
                WriteStatus("extract dir {0}...", null);
                // if true, then the entry was a directory and has been created.
                // We need to fire the Extract Event.
                OnAfterExtract(null);
                return;
            }
            // If no password explicitly specified, use the password on the entry itself,
            // or on the zipfile itself.
            if (_Encryption_FromZipFile != EncryptionAlgorithm.None)
                EnsurePassword(password);
            WriteStatus("extract entry {0} to stream...", fileName);
            var archiveStream = ArchiveStream;
            if (ExtractToStream(archiveStream, outStream, Encryption, _Crc32))
                goto ExitTry;
            OnAfterExtract(null);
        ExitTry:;
        }
        catch (Exception)
        {
            _ioOperationCanceled = true;
            throw;
        }
    }
    bool ExtractToStream(Stream archiveStream, Stream output, EncryptionAlgorithm encryptionAlgorithm, int expectedCrc32)
    {
        if (_ioOperationCanceled)
            return true;
        try
        {
            var calculatedCrc32 = ExtractAndCrc(archiveStream, output,
                _CompressionMethod_FromZipFile, _CompressedFileDataSize,
                UncompressedSize);
            if (_ioOperationCanceled)
                return true;
            VerifyCrcAfterExtract(calculatedCrc32, encryptionAlgorithm, expectedCrc32, archiveStream, UncompressedSize);
            return false;
        }
        finally
        {
            if (archiveStream is ZipSegmentedStream zss)
            {
                // need to dispose it
                zss.Dispose();
                _archiveStream = null;
            }
        }
    }
    void MoveFileInPlace(
        bool fileExistsBeforeExtraction,
        string targetFileName,
        string tmpPath, bool checkLaterForResetDirTimes)
    {
        // workitem 10639
        // move file to permanent home
        string zombie = null;
        if (fileExistsBeforeExtraction)
        {
            // An AV program may hold the target file open, which means
            // File.Delete() will succeed, though the actual deletion
            // remains pending. This will prevent a subsequent
            // File.Move() from succeeding. To avoid this, when the file
            // already exists, we need to replace it in 3 steps:
            //
            //     1. rename the existing file to a zombie name;
            //     2. rename the extracted file from the temp name to
            //        the target file name;
            //     3. delete the zombie.
            //
            zombie = targetFileName + Path.GetRandomFileName() + ".PendingOverwrite";
            File.Move(targetFileName, zombie);
        }
        File.Move(tmpPath, targetFileName);
        _SetTimes(targetFileName, true);
        if (zombie != null && File.Exists(zombie))
            ReallyDelete(zombie);
        // workitem 8264
        if (checkLaterForResetDirTimes)
        {
            // This is sort of a hack.  What I do here is set the time on
            // the parent directory, every time a file is extracted into
            // it.  If there is a directory with 1000 files, then I set
            // the time on the dir, 1000 times. This allows the directory
            // to have times that reflect the actual time on the entry in
            // the zip archive.
            if (FileName.Contains("/"))
            {
                var dirname = Path.GetDirectoryName(FileName);
                if (_container.ZipFile[dirname] == null)
                    _SetTimes(Path.GetDirectoryName(targetFileName), false);
            }
        }
        // workitem 7071
        //
        // We can only apply attributes if they are relevant to the NTFS
        // OS.  Must do this LAST because it may involve a ReadOnly bit,
        // which would prevent us from setting the time, etc.
        //
        // workitem 7926 - version made by OS can be zero (FAT) or 10
        // (NTFS)
        if ((_VersionMadeBy & 0xFF00) == 0x0a00 || (_VersionMadeBy & 0xFF00) == 0x0000)
        {
            var validAttrs = FileAttributeHelper.StripInvalidAttributes((FileAttributes)_ExternalFileAttrs);
            File.SetAttributes(targetFileName, validAttrs);
        }
    }
    void EnsurePassword(string password)
    {
        var parameter = (password ?? _Password ?? _container.Password) ?? throw new BadPasswordException();
        SetupCryptoForExtract(parameter);
    }
    FileStream OpenFileStream(string tmpPath, ref bool checkLaterForResetDirTimes)
    {
        var dirName = Path.GetDirectoryName(tmpPath);
        // ensure the target path exists
        if (!Directory.Exists(dirName))
        {
            // we create the directory here, but we do not set the
            // create/modified/accessed times on it because it is being
            // created implicitly, not explcitly. There's no entry in the
            // zip archive for the directory.
            Directory.CreateDirectory(dirName);
        }
        else
        {
            // workitem 8264
            if (_container.ZipFile != null)
                checkLaterForResetDirTimes = _container.ZipFile._inExtractAll;
        }
        // File.Create(CreateNew) will overwrite any existing file.
        return new FileStream(tmpPath, FileMode.CreateNew);
    }
#if NOT
        internal void CalcWinZipAesMac(Stream input)
        {
            if (Encryption == EncryptionAlgorithm.WinZipAes128 ||
                Encryption == EncryptionAlgorithm.WinZipAes256)
            {
                if (input is WinZipAesCipherStream)
                    wzs = input as WinZipAesCipherStream;
                else if (input is CrcCalculatorStream)
                {
                    xxx;
                }
            }
        }
#endif
    internal void VerifyCrcAfterExtract(Int32 calculatedCrc32, EncryptionAlgorithm encryptionAlgorithm, int expectedCrc32, Stream archiveStream, long uncompressedSize)
    {
#if AESCRYPTO
                // After extracting, Validate the CRC32
                if (calculatedCrc32 != expectedCrc32)
                {
                    // CRC is not meaningful with WinZipAES and AES method 2 (AE-2)
                    if ((encryptionAlgorithm != EncryptionAlgorithm.WinZipAes128 &&
                         encryptionAlgorithm != EncryptionAlgorithm.WinZipAes256)
                        || _WinZipAesMethod != 0x02)
                        throw new BadCrcException("CRC error: the file being extracted appears to be corrupted. " +
                                                  String.Format("Expected 0x{0:X8}, Actual 0x{1:X8}", expectedCrc32, calculatedCrc32));
                }
                // ignore MAC if the size of the file is zero
                if (uncompressedSize == 0)
                    return;
                // calculate the MAC
                if (encryptionAlgorithm == EncryptionAlgorithm.WinZipAes128 ||
                    encryptionAlgorithm == EncryptionAlgorithm.WinZipAes256)
                {
                    var wzs = _inputDecryptorStream as WinZipAesCipherStream;
                    // sometimes there are extra bytes in the WinZipAES stream that were not required to generate
                    // the decryption output. Read and ignore these bytes, so that the CRC can be calculated:
                    byte[] throwAwayBuffer = new byte[256];
                    wzs.Read(throwAwayBuffer, 0, 256);
                    _aesCrypto_forExtract.CalculatedMac = wzs.FinalAuthentication;
                    _aesCrypto_forExtract.ReadAndVerifyMac(archiveStream); // throws if MAC is bad
                    // side effect: advances file position.
                }
#else
        if (calculatedCrc32 != expectedCrc32)
            throw new BadCrcException("CRC error: the file being extracted appears to be corrupted. " +
                                      String.Format("Expected 0x{0:X8}, Actual 0x{1:X8}", expectedCrc32, calculatedCrc32));
#endif
    }
    int CheckExtractExistingFile(string baseDir, string targetFileName)
    {
        int loop = 0;
        // returns: 0 == extract, 1 = don't, 2 = cancel
        do
        {
            switch (ExtractExistingFile)
            {
                case ExtractExistingFileAction.OverwriteSilently:
                    WriteStatus("the file {0} exists; will overwrite it...", targetFileName);
                    return 0;
                case ExtractExistingFileAction.DoNotOverwrite:
                    WriteStatus("the file {0} exists; not extracting entry...", FileName);
                    OnAfterExtract(baseDir);
                    return 1;
                case ExtractExistingFileAction.InvokeExtractProgressEvent:
                    if (loop > 0)
                        throw new ZipException(String.Format("The file {0} already exists.", targetFileName));
                    OnExtractExisting(baseDir);
                    if (_ioOperationCanceled)
                        return 2;
                    // loop around
                    break;
                case ExtractExistingFileAction.Throw:
                default:
                    throw new ZipException(String.Format("The file {0} already exists.", targetFileName));
            }
            loop++;
        }
        while (true);
    }
    void _CheckRead(int nbytes)
    {
        if (nbytes == 0)
            throw new BadReadException(String.Format("bad read of entry {0} from compressed archive.",
                         FileName));
    }
    Stream _inputDecryptorStream;
    int ExtractAndCrc(Stream archiveStream, Stream targetOutput,
        short compressionMethod,
        long compressedFileDataSize,
        long uncompressedSize)
    {
        int crcResult;
        var input = archiveStream;
        // change for workitem 8098
        input.Seek(FileDataPosition, SeekOrigin.Begin);
        var bytes = new byte[BufferSize];
        // The extraction process varies depending on how the entry was
        // stored.  It could have been encrypted, and it coould have
        // been compressed, or both, or neither. So we need to check
        // both the encryption flag and the compression flag, and take
        // the proper action in all cases.
        var leftToRead = (compressionMethod != (short)CompressionMethod.None)
            ? uncompressedSize
            : compressedFileDataSize;
        // Get a stream that either decrypts or not.
        _inputDecryptorStream = GetExtractDecryptor(input);
        var input3 = GetExtractDecompressor(_inputDecryptorStream);
        var bytesWritten = 0L;
        // As we read, we maybe decrypt, and then we maybe decompress. Then we write.
        using (var s1 = new CrcCalculatorStream(input3))
        {
            while (leftToRead > 0)
            {
                //Console.WriteLine("ExtractOne: LeftToRead {0}", LeftToRead);
                // Casting LeftToRead down to an int is ok here in the else clause,
                // because that only happens when it is less than bytes.Length,
                // which is much less than MAX_INT.
                int len = (leftToRead > bytes.Length) ? bytes.Length : (int)leftToRead;
                int bytesRead = s1.Read(bytes, 0, len);
                // must check data read - essential for detecting corrupt zip files
                _CheckRead(bytesRead);
                targetOutput.Write(bytes, 0, bytesRead);
                leftToRead -= bytesRead;
                bytesWritten += bytesRead;
                // fire the progress event, check for cancels
                OnExtractProgress(bytesWritten, uncompressedSize);
                if (_ioOperationCanceled)
                    break;
            }
            crcResult = s1.Crc;
        }
        return crcResult;
    }
    Stream GetExtractDecompressor(Stream input2)
    {
        if (input2 == null) throw new ArgumentNullException(nameof(input2));
        // get a stream that either decompresses or not.
        switch (_CompressionMethod_FromZipFile)
        {
            case (short)CompressionMethod.None:
                return input2;
            case (short)CompressionMethod.Deflate:
                return new Zlib.DeflateStream(input2, Zlib.CompressionMode.Decompress, true);
            case (short)CompressionMethod.Deflate64:
                return new Deflate64.Deflate64Stream(input2);
#if BZIP
                case (short)CompressionMethod.BZip2:
                    return new BZip2.BZip2InputStream(input2, true);
#endif
        }
        throw new Exception(string.Format("Failed to find decompressor matching {0}",
            _CompressionMethod_FromZipFile));
    }
    Stream GetExtractDecryptor(Stream input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        Stream input2;
        if (_Encryption_FromZipFile == EncryptionAlgorithm.PkzipWeak)
            input2 = new ZipCipherStream(input, _zipCrypto_forExtract, CryptoMode.Decrypt);
#if AESCRYPTO
            else if (_Encryption_FromZipFile == EncryptionAlgorithm.WinZipAes128 ||
                 _Encryption_FromZipFile == EncryptionAlgorithm.WinZipAes256)
                input2 = new WinZipAesCipherStream(input, _aesCrypto_forExtract, _CompressedFileDataSize, CryptoMode.Decrypt);
#endif
        else
            input2 = input;
        return input2;
    }
    internal void _SetTimes(string fileOrDirectory, bool isFile)
    {
        // workitem 8807:
        // Because setting the time is not considered to be a fatal error,
        // and because other applications can interfere with the setting
        // of a time on a directory, we're going to swallow IO exceptions
        // in this method.
        try
        {
            if (_ntfsTimesAreSet)
            {
                if (isFile)
                {
                    // It's possible that the extract was cancelled, in which case,
                    // the file does not exist.
                    if (File.Exists(fileOrDirectory))
                    {
                        File.SetCreationTimeUtc(fileOrDirectory, _Ctime);
                        File.SetLastAccessTimeUtc(fileOrDirectory, _Atime);
                        File.SetLastWriteTimeUtc(fileOrDirectory, _Mtime);
                    }
                }
                else
                {
                    // It's possible that the extract was cancelled, in which case,
                    // the directory does not exist.
                    if (Directory.Exists(fileOrDirectory))
                    {
                        Directory.SetCreationTimeUtc(fileOrDirectory, _Ctime);
                        Directory.SetLastAccessTimeUtc(fileOrDirectory, _Atime);
                        Directory.SetLastWriteTimeUtc(fileOrDirectory, _Mtime);
                    }
                }
            }
            else
            {
                // workitem 6191
                DateTime AdjustedLastModified = Ionic.Zip.SharedUtilities.AdjustTime_Reverse(LastModified);
                if (isFile)
                    File.SetLastWriteTime(fileOrDirectory, AdjustedLastModified);
                else
                    Directory.SetLastWriteTime(fileOrDirectory, AdjustedLastModified);
            }
        }
        catch (System.IO.IOException ioexc1)
        {
            WriteStatus("failed to set time on {0}: {1}", fileOrDirectory, ioexc1.Message);
        }
    }
    #region Support methods
    // workitem 7968
    static string GetUnsupportedAlgorithm(uint unsupportedAlgorithmId)
    {
        string alg = unsupportedAlgorithmId switch
        {
            0 => "--",
            0x6601 => "DES",
            // - RC2 (version needed to extract < 5.2)
            0x6602 => "RC2",
            // - 3DES 168
            0x6603 => "3DES-168",
            // - 3DES 112
            0x6609 => "3DES-112",
            // - AES 128
            0x660E => "PKWare AES128",
            // - AES 192
            0x660F => "PKWare AES192",
            // - AES 256
            0x6610 => "PKWare AES256",
            // - RC2 (version needed to extract >= 5.2)
            0x6702 => "RC2",
            // - Blowfish
            0x6720 => "Blowfish",
            // - Twofish
            0x6721 => "Twofish",
            // - RC4
            0x6801 => "RC4",
            // - Unknown algorithm
            _ => String.Format("Unknown (0x{0:X4})", unsupportedAlgorithmId),
        };
        return alg;
    }
    // workitem 7968
    static string GetUnsupportedCompressionMethod(short compressionMethod)
    {
        string meth = (int)compressionMethod switch
        {
            0 => "Store",
            1 => "Shrink",
            8 => "DEFLATE",
            9 => "Deflate64",
            12 => "BZIP2",// only if BZIP not compiled in
            14 => "LZMA",
            19 => "LZ77",
            98 => "PPMd",
            _ => String.Format("Unknown (0x{0:X4})", compressionMethod),
        };
        return meth;
    }
    static void ValidateEncryption(EncryptionAlgorithm encryptionAlgorithm, string fileName, uint unsupportedAlgorithmId)
    {
        if (encryptionAlgorithm != EncryptionAlgorithm.PkzipWeak &&
#if AESCRYPTO
                encryptionAlgorithm != EncryptionAlgorithm.WinZipAes128 &&
                encryptionAlgorithm != EncryptionAlgorithm.WinZipAes256 &&
#endif
            encryptionAlgorithm != EncryptionAlgorithm.None)
        {
            // workitem 7968
            if (unsupportedAlgorithmId != 0)
                throw new ZipException(string.Format("Cannot extract: Entry {0} is encrypted with an algorithm not supported by DotNetZip: {1}",
                                                     fileName, GetUnsupportedAlgorithm(unsupportedAlgorithmId)));
            throw new ZipException(string.Format("Cannot extract: Entry {0} uses an unsupported encryption algorithm ({1:X2})",
                                                 fileName, (int)encryptionAlgorithm));
        }
    }
    static void ValidateCompression(short compressionMethod, string fileName, string compressionMethodName)
    {
        if ((compressionMethod != (short)CompressionMethod.None) &&
            (compressionMethod != (short)CompressionMethod.Deflate) &&
            (compressionMethod != (short)CompressionMethod.Deflate64)
#if BZIP
                && (compressionMethod != (short)CompressionMethod.BZip2)
#endif
            )
            throw new ZipException(String.Format("Entry {0} uses an unsupported compression method (0x{1:X2}, {2})",
                                                      fileName, compressionMethod, compressionMethodName));
    }
    void SetupCryptoForExtract(string password)
    {
        //if (password == null) return;
        if (_Encryption_FromZipFile == EncryptionAlgorithm.None) return;
        if (_Encryption_FromZipFile == EncryptionAlgorithm.PkzipWeak)
        {
            if (password == null)
                throw new ZipException("Missing password.");
            this.ArchiveStream.Seek(this.FileDataPosition - 12, SeekOrigin.Begin);
            _zipCrypto_forExtract = ZipCrypto.ForRead(password, this);
        }
#if AESCRYPTO
            else if (_Encryption_FromZipFile == EncryptionAlgorithm.WinZipAes128 ||
                 _Encryption_FromZipFile == EncryptionAlgorithm.WinZipAes256)
            {
                if (password == null)
                    throw new ZipException("Missing password.");
                // If we already have a WinZipAesCrypto object in place, use it.
                // It can be set up in the ReadDirEntry(), or during a previous Extract.
                if (_aesCrypto_forExtract != null)
                {
                    _aesCrypto_forExtract.Password = password;
                }
                else
                {
                    int sizeOfSaltAndPv = GetLengthOfCryptoHeaderBytes(_Encryption_FromZipFile);
                    this.ArchiveStream.Seek(this.FileDataPosition - sizeOfSaltAndPv, SeekOrigin.Begin);
                    int keystrength = GetKeyStrengthInBits(_Encryption_FromZipFile);
                    _aesCrypto_forExtract = WinZipAesCrypto.ReadFromStream(password, keystrength, this.ArchiveStream);
                }
            }
#endif
    }
    bool IsDoneWithOutputToBaseDir(string baseDir, out string outFileName)
    {
        if (baseDir == null) throw new ArgumentNullException(nameof(baseDir));
        // Sometimes the name on the entry starts with a slash.
        // Rather than unpack to the root of the volume, we're going to
        // drop the slash and unpack to the specified base directory.
        var fileName = FileName.Replace(Path.DirectorySeparatorChar, '/');
        // workitem 11772: remove drive letter with separator
        if (fileName.IndexOf(':') == 1)
            fileName = fileName[2..];
        if (fileName.StartsWith("/"))
            fileName = fileName[1..];
        fileName = SharedUtilities.SanitizePath(fileName);
        outFileName = _container.ZipFile.FlattenFoldersOnExtract
            ? Path.Combine(baseDir, fileName.Contains("/") ? Path.GetFileName(fileName) : fileName)
            : Path.Combine(baseDir, fileName);
        // workitem 10639
        outFileName = outFileName.Replace('/', Path.DirectorySeparatorChar);
        // Resolve any directory traversal sequence and compare the result with the intended base directory
        // where the file or folder will be created.
        // https://gist.github.com/thomas-chauchefoin-bentley-systems/855218959116f870f08857cce2aec731
        var canonicalOutPath = Path.GetFullPath(outFileName);
        var canonicalBaseDir = Path.GetFullPath(baseDir);
        if (!canonicalOutPath.StartsWith(canonicalBaseDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException(string.Format("Extracting {0} would write to {1}, outside of {2}; rejecting.", outFileName, canonicalOutPath, canonicalBaseDir));
        }
        // check if it is a directory
        if (IsDirectory || FileName.EndsWith("/"))
        {
            if (!Directory.Exists(outFileName))
            {
                Directory.CreateDirectory(outFileName);
                _SetTimes(outFileName, false);
            }
            else
            {
                // the dir exists, maybe we want to overwrite times.
                if (ExtractExistingFile == ExtractExistingFileAction.OverwriteSilently)
                    _SetTimes(outFileName, false);
            }
            return true;  // true == all done, caller will return
        }
        return false;  // false == work to do by caller.
    }
    bool IsDoneWithOutputToStream() => IsDirectory || FileName.EndsWith("/");
    #endregion
}