namespace Ionic.Zip;

// ZipFile.Extract.cs
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
// Time-stamp: <2011-July-31 14:45:18>
//
// ------------------------------------------------------------------
//
// This module defines the methods for Extract operations on zip files.
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
    public void ExtractAll(string path) => _InternalExtractAll(path, true);
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    /// <param name="extractExistingFile">
    /// The action to take if extraction would overwrite an existing file.
    /// </param>
    /// <seealso cref="ExtractSelectedEntries(String,ExtractExistingFileAction)"/>
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public void ExtractAll(string path, ExtractExistingFileAction extractExistingFile)
        {
            ExtractExistingFile = extractExistingFile;
            _InternalExtractAll(path, true);
        }
        private void _InternalExtractAll(string path, bool overrideExtractExistingProperty)
        {
            bool header = Verbose;
            _inExtractAll = true;
            try
            {
                OnExtractAllStarted(path);
                int n = 0;
                foreach (ZipEntry e in _entries.Values)
                {
                    if (header)
                    {
                        StatusMessageTextWriter.WriteLine("\n{1,-22} {2,-8} {3,4}   {4,-8}  {0}",
                                  "Name", "Modified", "Size", "Ratio", "Packed");
                        StatusMessageTextWriter.WriteLine(new System.String('-', 72));
                        header = false;
                    }
                    if (Verbose)
                    {
                        StatusMessageTextWriter.WriteLine("{1,-22} {2,-8} {3,4:F0}%   {4,-8} {0}",
                                  e.FileName,
                                  e.LastModified.ToString("yyyy-MM-dd HH:mm:ss"),
                                  e.UncompressedSize,
                                  e.CompressionRatio,
                                  e.CompressedSize);
                        if (!String.IsNullOrEmpty(e.Comment))
                            StatusMessageTextWriter.WriteLine("  Comment: {0}", e.Comment);
                    }
                    e.Password = _Password;  // this may be null
                    OnExtractEntry(n, true, e, path);
                    if (overrideExtractExistingProperty)
                        e.ExtractExistingFile = this.ExtractExistingFile;
                    e.Extract(path);
                    n++;
                    OnExtractEntry(n, false, e, path);
                    if (_extractOperationCanceled)
                        break;
                }
                if (!_extractOperationCanceled)
                {
                    // workitem 8264:
                    // now, set times on directory entries, again.
                    // The problem is, extracting a file changes the times on the parent
                    // directory.  So after all files have been extracted, we have to
                    // run through the directories again.
                    foreach (ZipEntry e in _entries.Values)
                    {
                        // check if it is a directory
                        if ((e.IsDirectory) || (e.FileName.EndsWith("/")))
                        {
                            string outputFile = (e.FileName.StartsWith("/"))
                                ? Path.Combine(path, e.FileName[1..])
                                : Path.Combine(path, e.FileName);
                            e._SetTimes(outputFile, false);
                        }
                    }
                    OnExtractAllCompleted(path);
                }
            }
            finally
            {
                _inExtractAll = false;
            }
        }
    }