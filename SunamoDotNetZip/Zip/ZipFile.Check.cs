namespace Ionic.Zip;

// ZipFile.Check.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2011 Dino Chiesa.
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
// Time-stamp: <2011-July-31 14:40:50>
//
// ------------------------------------------------------------------
//
// This module defines the methods for doing Checks on zip files.
// These are not necessary to include in the Reduced version
// of the library.
//
// ------------------------------------------------------------------
//
    public partial class ZipFile
    {
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public static bool CheckZip(string zipFileName) => CheckZip(zipFileName, false, null);
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
    public static bool CheckZip(string zipFileName, bool fixIfNecessary,
                                    TextWriter writer)
        {
            ZipFile zip1 = null, zip2 = null;
            bool isOk = true;
            try
            {
            zip1 = new ZipFile
            {
                FullScan = true
            };
            zip1.Initialize(zipFileName);
                zip2 = ZipFile.Read(zipFileName);
                foreach (var e1 in zip1)
                {
                    foreach (var e2 in zip2)
                    {
                        if (e1.FileName == e2.FileName)
                        {
                            if (e1._RelativeOffsetOfLocalHeader != e2._RelativeOffsetOfLocalHeader)
                            {
                                isOk = false;
                                writer?.WriteLine("{0}: mismatch in RelativeOffsetOfLocalHeader  (0x{1:X16} != 0x{2:X16})",
                                                        e1.FileName, e1._RelativeOffsetOfLocalHeader,
                                                        e2._RelativeOffsetOfLocalHeader);
                            }
                            if (e1._CompressedSize != e2._CompressedSize)
                            {
                                isOk = false;
                                writer?.WriteLine("{0}: mismatch in CompressedSize  (0x{1:X16} != 0x{2:X16})",
                                                        e1.FileName, e1._CompressedSize,
                                                        e2._CompressedSize);
                            }
                            if (e1._UncompressedSize != e2._UncompressedSize)
                            {
                                isOk = false;
                                writer?.WriteLine("{0}: mismatch in UncompressedSize  (0x{1:X16} != 0x{2:X16})",
                                                        e1.FileName, e1._UncompressedSize,
                                                        e2._UncompressedSize);
                            }
                            if (e1.CompressionMethod != e2.CompressionMethod)
                            {
                                isOk = false;
                                writer?.WriteLine("{0}: mismatch in CompressionMethod  (0x{1:X4} != 0x{2:X4})",
                                                        e1.FileName, e1.CompressionMethod,
                                                        e2.CompressionMethod);
                            }
                            if (e1.Crc != e2.Crc)
                            {
                                isOk = false;
                                writer?.WriteLine("{0}: mismatch in Crc32  (0x{1:X4} != 0x{2:X4})",
                                                        e1.FileName, e1.Crc,
                                                        e2.Crc);
                            }
                            // found a match, so stop the inside loop
                            break;
                        }
                    }
                }
                zip2.Dispose();
                zip2 = null;
                if (!isOk && fixIfNecessary)
                {
                    string newFileName = Path.GetFileNameWithoutExtension(zipFileName);
                    newFileName = System.String.Format("{0}_fixed.zip", newFileName);
                    zip1.Save(newFileName);
                }
            }
            finally
            {
                zip1?.Dispose();
                zip2?.Dispose();
            }
            return isOk;
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
        public static void FixZipDirectory(string zipFileName)
        {
        using var zip = new ZipFile();
        zip.FullScan = true;
        zip.Initialize(zipFileName);
        zip.Save(zipFileName);
    }
        ///
        ///
        ///
        ///
        public static bool CheckZipPassword(string zipFileName, string password)
        {
            // workitem 13664
            bool success = false;
            try
            {
                using (ZipFile zip1 = ZipFile.Read(zipFileName))
                {
                    foreach (var e in zip1)
                    {
                        if (!e.IsDirectory && e.UsesEncryption)
                        {
                            e.ExtractWithPassword(System.IO.Stream.Null, password);
                        }
                    }
                }
                success = true;
            }
            catch(Ionic.Zip.BadPasswordException) { }
            return success;
        }
        ///
        public string Info
        {
            get
            {
                var builder = new System.Text.StringBuilder();
                builder.Append(string.Format("          ZipFile: {0}\n", this.Name));
                if (!string.IsNullOrEmpty(this._Comment))
                {
                    builder.Append(string.Format("          Comment: {0}\n", this._Comment));
                }
                if (this._versionMadeBy != 0)
                {
                    builder.Append(string.Format("  version made by: 0x{0:X4}\n", this._versionMadeBy));
                }
                if (this._versionNeededToExtract != 0)
                {
                    builder.Append(string.Format("needed to extract: 0x{0:X4}\n", this._versionNeededToExtract));
                }
                builder.Append(string.Format("       uses ZIP64: {0}\n", this.InputUsesZip64));
                builder.Append(string.Format("     disk with CD: {0}\n", this._diskNumberWithCd));
                if (this._OffsetOfCentralDirectory == 0xFFFFFFFF)
                    builder.Append(string.Format("      CD64 offset: 0x{0:X16}\n", this._OffsetOfCentralDirectory64));
                else
                    builder.Append(string.Format("        CD offset: 0x{0:X8}\n", this._OffsetOfCentralDirectory));
                builder.Append('\n');
                foreach (ZipEntry entry in this._entries.Values)
                {
                    builder.Append(entry.Info);
                }
                return builder.ToString();
            }
        }
    }