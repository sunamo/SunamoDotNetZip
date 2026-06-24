namespace Ionic.Zip;

// ZipFile.AddUpdate.cs
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
// Time-stamp: <2011-November-01 13:56:58>
//
// ------------------------------------------------------------------
//
// This module defines the methods for Adding and Updating entries in
// the ZipFile.
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
    public ZipEntry AddItem(string fileOrDirectoryName) => AddItem(fileOrDirectoryName, null);
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
    public ZipEntry AddItem(String fileOrDirectoryName, String directoryPathInArchive)
        {
            if (File.Exists(fileOrDirectoryName))
                return AddFile(fileOrDirectoryName, directoryPathInArchive);
        return Directory.Exists(fileOrDirectoryName)
                ? AddDirectory(fileOrDirectoryName, directoryPathInArchive)
                : throw new FileNotFoundException(String.Format("That file or directory ({0}) does not exist!",
                                                          fileOrDirectoryName));
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
    public ZipEntry AddFile(string fileName) => AddFile(fileName, null);
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
    public ZipEntry AddFile(string fileName, String directoryPathInArchive)
        {
            string nameInArchive = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            ZipEntry ze = ZipEntry.CreateFromFile(fileName, nameInArchive);
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", fileName);
            return _InternalAddEntry(ze);
        }
        ///
        ///
        public void RemoveEntries(System.Collections.Generic.ICollection<ZipEntry> entriesToRemove)
        {
        if (entriesToRemove == null) throw new ArgumentNullException(nameof(entriesToRemove));
        foreach (ZipEntry e in entriesToRemove)
            {
                this.RemoveEntry(e);
            }
        }
        ///
        ///
        public void RemoveEntries(System.Collections.Generic.ICollection<String> entriesToRemove)
        {
        if (entriesToRemove == null) throw new ArgumentNullException(nameof(entriesToRemove));
        foreach (String e in entriesToRemove)
            {
                this.RemoveEntry(e);
            }
        }
    ///
    ///
    ///
    ///
    ///
    ///
    public void AddFiles(System.Collections.Generic.IEnumerable<String> fileNames) => this.AddFiles(fileNames, null);
    ///
    ///
    ///
    ///
    public void UpdateFiles(System.Collections.Generic.IEnumerable<String> fileNames) => this.UpdateFiles(fileNames, null);
    ///
    ///
    ///
    ///
    ///
    public void AddFiles(System.Collections.Generic.IEnumerable<String> fileNames, String directoryPathInArchive) => AddFiles(fileNames, false, directoryPathInArchive);
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public void AddFiles(System.Collections.Generic.IEnumerable<String> fileNames,
                             bool preserveDirHierarchy,
                             String directoryPathInArchive)
        {
        if (fileNames == null) throw new ArgumentNullException(nameof(fileNames));
        _addOperationCanceled = false;
            OnAddStarted();
            if (preserveDirHierarchy)
            {
                foreach (var f in fileNames)
                {
                    if (_addOperationCanceled) break;
                    if (directoryPathInArchive != null)
                    {
                        //string s = SharedUtilities.NormalizePath(Path.Combine(directoryPathInArchive, Path.GetDirectoryName(f)));
                        string s = Path.GetFullPath(Path.Combine(directoryPathInArchive, Path.GetDirectoryName(f)));
                        this.AddFile(f, s);
                    }
                    else
                        this.AddFile(f, null);
                }
            }
            else
            {
                foreach (var f in fileNames)
                {
                    if (_addOperationCanceled) break;
                    this.AddFile(f, directoryPathInArchive);
                }
            }
            if (!_addOperationCanceled)
                OnAddCompleted();
        }
        ///
        ///
        ///
        ///
        ///
        ///
        public void UpdateFiles(System.Collections.Generic.IEnumerable<String> fileNames, String directoryPathInArchive)
        {
        if (fileNames == null) throw new ArgumentNullException(nameof(fileNames));
        OnAddStarted();
            foreach (var f in fileNames)
                this.UpdateFile(f, directoryPathInArchive);
            OnAddCompleted();
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
    public ZipEntry UpdateFile(string fileName) => UpdateFile(fileName, null);
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public ZipEntry UpdateFile(string fileName, String directoryPathInArchive)
        {
            // ideally this would all be transactional!
            var key = ZipEntry.NameInArchive(fileName, directoryPathInArchive);
            if (this[key] != null)
                this.RemoveEntry(key);
            return this.AddFile(fileName, directoryPathInArchive);
        }
    ///
    ///
    ///
    ///
    public ZipEntry UpdateDirectory(string directoryName) => UpdateDirectory(directoryName, null);
    ///
    ///
    ///
    ///
    ///
    public ZipEntry UpdateDirectory(string directoryName, String directoryPathInArchive) => this.AddOrUpdateDirectoryImpl(directoryName, directoryPathInArchive, AddOrUpdateAction.AddOrUpdate);
    ///
    ///
    ///
    ///
    public void UpdateItem(string itemName) => UpdateItem(itemName, null);
    ///
    ///
    ///
    ///
    ///
    public void UpdateItem(string itemName, string directoryPathInArchive)
        {
            if (File.Exists(itemName))
                UpdateFile(itemName, directoryPathInArchive);
            else if (Directory.Exists(itemName))
                UpdateDirectory(itemName, directoryPathInArchive);
            else
                throw new FileNotFoundException(String.Format("That file or directory ({0}) does not exist!", itemName));
        }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public ZipEntry AddEntry(string entryName, string content) => AddEntry(entryName, content, System.Text.Encoding.Default);
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
    public ZipEntry AddEntry(string entryName, string content, System.Text.Encoding encoding)
        {
            // cannot employ a using clause here.  We need the stream to
            // persist after exit from this method.
            var ms = new MemoryStream();
            // cannot use a using clause here; StreamWriter takes
            // ownership of the stream and Disposes it before we are ready.
            var sw = new StreamWriter(ms, encoding);
            sw.Write(content);
            sw.Flush();
            // reset to allow reading later
            ms.Seek(0, SeekOrigin.Begin);
            return AddEntry(entryName, ms);
            // must not dispose the MemoryStream - it will be used later.
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
        public ZipEntry AddEntry(string entryName, Stream stream)
        {
            ZipEntry ze = ZipEntry.CreateForStream(entryName, stream);
            ze.SetEntryTimes(DateTime.Now,DateTime.Now,DateTime.Now);
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", entryName);
            return _InternalAddEntry(ze);
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
        ///
        public ZipEntry AddEntry(string entryName, WriteDelegate writer)
        {
            ZipEntry ze = ZipEntry.CreateForWriter(entryName, writer);
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", entryName);
            return _InternalAddEntry(ze);
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
        public ZipEntry AddEntry(string entryName, OpenDelegate opener, CloseDelegate closer)
        {
            ZipEntry ze = ZipEntry.CreateForJitStreamProvider(entryName, opener, closer);
            ze.SetEntryTimes(DateTime.Now,DateTime.Now,DateTime.Now);
            if (Verbose) StatusMessageTextWriter.WriteLine("adding {0}...", entryName);
            return _InternalAddEntry(ze);
        }
        public void AddEntry(ZipEntry ze)
        {
            if (ze._container != null)
            {
                throw new InvalidOperationException("Entry already belongs to a zip file");
            }
            ze._container = new ZipContainer(this);
            InternalAddEntry(ze.FileName, ze);
            AfterAddEntry(ze);
        }
        private ZipEntry _InternalAddEntry(ZipEntry ze)
        {
            // stamp all the props onto the entry
            ze._container = new ZipContainer(this);
            ze.CompressionMethod = this.CompressionMethod;
            ze.CompressionLevel = this.CompressionLevel;
            ze.ExtractExistingFile = this.ExtractExistingFile;
            ze.ZipErrorAction = this.ZipErrorAction;
            ze.SetCompression = this.SetCompression;
            ze.AlternateEncoding = this.AlternateEncoding;
            ze.AlternateEncodingUsage = this.AlternateEncodingUsage;
            ze.Password = this._Password;
            ze.Encryption = this.Encryption;
            ze.EmitTimesInWindowsFormatWhenSaving = this._emitNtfsTimes;
            ze.EmitTimesInUnixFormatWhenSaving = this._emitUnixTimes;
            //string key = DictionaryKeyForEntry(ze);
            InternalAddEntry(ze.FileName,ze);
            AfterAddEntry(ze);
            return ze;
        }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public ZipEntry UpdateEntry(string entryName, string content) => UpdateEntry(entryName, content, System.Text.Encoding.Default);
    ///
    ///
    ///
    ///
    ///
    ///
    public ZipEntry UpdateEntry(string entryName, string content, System.Text.Encoding encoding)
        {
            RemoveEntryForUpdate(entryName);
            return AddEntry(entryName, content, encoding);
        }
        ///
        ///
        ///
        ///
        ///
        public ZipEntry UpdateEntry(string entryName, WriteDelegate writer)
        {
            RemoveEntryForUpdate(entryName);
            return AddEntry(entryName, writer);
        }
        ///
        ///
        ///
        ///
        ///
        public ZipEntry UpdateEntry(string entryName, OpenDelegate opener, CloseDelegate closer)
        {
            RemoveEntryForUpdate(entryName);
            return AddEntry(entryName, opener, closer);
        }
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        public ZipEntry UpdateEntry(string entryName, Stream stream)
        {
            RemoveEntryForUpdate(entryName);
            return AddEntry(entryName, stream);
        }
        private void RemoveEntryForUpdate(string entryName)
        {
            if (String.IsNullOrEmpty(entryName))
                throw new ArgumentNullException(nameof(entryName));
            string directoryPathInArchive = null;
            if (entryName.IndexOf('\\') != -1)
            {
                directoryPathInArchive = Path.GetDirectoryName(entryName);
                entryName = Path.GetFileName(entryName);
            }
            var key = ZipEntry.NameInArchive(entryName, directoryPathInArchive);
            if (this[key] != null)
                this.RemoveEntry(key);
        }
        ///
        ///
        ///
        public ZipEntry AddEntry(string entryName, byte[] byteContent)
        {
            if (byteContent == null) throw new ArgumentException("bad argument", nameof(byteContent));
            var ms = new MemoryStream(byteContent);
            return AddEntry(entryName, ms);
        }
        ///
        ///
        ///
        ///
        ///
        public ZipEntry UpdateEntry(string entryName, byte[] byteContent)
        {
            RemoveEntryForUpdate(entryName);
            return AddEntry(entryName, byteContent);
        }
    //         private string DictionaryKeyForEntry(ZipEntry ze1)
    //         {
    //             var filename = SharedUtilities.NormalizePathForUseInZipFile(ze1.FileName);
    //             return filename;
    //         }
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    ///
    public ZipEntry AddDirectory(string directoryName) => AddDirectory(directoryName, null);
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
    public ZipEntry AddDirectory(string directoryName, string directoryPathInArchive) => AddOrUpdateDirectoryImpl(directoryName, directoryPathInArchive, AddOrUpdateAction.AddOnly);
    ///
    ///
    ///
    ///
    ///
    public ZipEntry AddDirectoryByName(string directoryNameInArchive)
        {
            // workitem 9073
            ZipEntry dir = ZipEntry.CreateFromNothing(directoryNameInArchive);
            dir._container = new ZipContainer(this);
            dir.MarkAsDirectory();
            dir.AlternateEncoding = this.AlternateEncoding;  // workitem 8984
            dir.AlternateEncodingUsage = this.AlternateEncodingUsage;
            dir.SetEntryTimes(DateTime.Now,DateTime.Now,DateTime.Now);
            dir.EmitTimesInWindowsFormatWhenSaving = _emitNtfsTimes;
            dir.EmitTimesInUnixFormatWhenSaving = _emitUnixTimes;
            dir._Source = ZipEntrySource.Stream;
            //string key = DictionaryKeyForEntry(dir);
            InternalAddEntry(dir.FileName,dir);
            AfterAddEntry(dir);
            return dir;
        }
        private ZipEntry AddOrUpdateDirectoryImpl(string directoryName,
                                                  string rootDirectoryPathInArchive,
                                                  AddOrUpdateAction action)
        {
            if (rootDirectoryPathInArchive == null)
            {
                rootDirectoryPathInArchive = "";
            }
            return AddOrUpdateDirectoryImpl(directoryName, rootDirectoryPathInArchive, action, true, 0);
        }
        internal void InternalAddEntry(String name, ZipEntry entry)
        {
            _entries.Add(name, entry);
            _entriesInsensitive.TryAdd(name, entry);
        _zipEntriesAsList = null;
            _contentsChanged = true;
        }
        private ZipEntry AddOrUpdateDirectoryImpl(string directoryName,
                                                  string rootDirectoryPathInArchive,
                                                  AddOrUpdateAction action,
                                                  bool recurse,
                                                  int level)
        {
            if (Verbose)
                StatusMessageTextWriter.WriteLine("{0} {1}...",
                                                  (action == AddOrUpdateAction.AddOnly) ? "adding" : "Adding or updating",
                                                  directoryName);
            if (level == 0)
            {
                _addOperationCanceled = false;
                OnAddStarted();
            }
            // workitem 13371
            if (_addOperationCanceled)
                return null;
            string dirForEntries = rootDirectoryPathInArchive;
            ZipEntry baseDir = null;
            if (level > 0)
            {
                int f = directoryName.Length;
                for (int i = level; i > 0; i--)
                    f = directoryName.LastIndexOfAny("/\\".ToCharArray(), f - 1, f - 1);
                dirForEntries = directoryName[(f + 1)..];
                dirForEntries = Path.Combine(rootDirectoryPathInArchive, dirForEntries);
            }
            // if not top level, or if the root is non-empty, then explicitly add the directory
            if (level > 0 || rootDirectoryPathInArchive != "")
            {
                baseDir = ZipEntry.CreateFromFile(directoryName, dirForEntries);
                baseDir._container = new ZipContainer(this);
                baseDir.AlternateEncoding = this.AlternateEncoding;  // workitem 6410
                baseDir.AlternateEncodingUsage = this.AlternateEncodingUsage;
                baseDir.MarkAsDirectory();
                baseDir.EmitTimesInWindowsFormatWhenSaving = _emitNtfsTimes;
                baseDir.EmitTimesInUnixFormatWhenSaving = _emitUnixTimes;
                // add the directory only if it does not exist.
                // It's not an error if it already exists.
                if (!_entries.ContainsKey(baseDir.FileName))
                {
                    InternalAddEntry(baseDir.FileName,baseDir);
                    AfterAddEntry(baseDir);
                }
                dirForEntries = baseDir.FileName;
            }
            if (!_addOperationCanceled)
            {
                String[] filenames = Directory.GetFiles(directoryName);
                if (recurse)
                {
                    // add the files:
                    foreach (String filename in filenames)
                    {
                        if (_addOperationCanceled) break;
                        if (action == AddOrUpdateAction.AddOnly)
                            AddFile(filename, dirForEntries);
                        else
                            UpdateFile(filename, dirForEntries);
                    }
                    if (!_addOperationCanceled)
                    {
                        // add the subdirectories:
                        String[] dirnames = Directory.GetDirectories(directoryName);
                        foreach (String dir in dirnames)
                        {
                            // workitem 8617: Optionally traverse reparse points
                            FileAttributes fileAttrs = System.IO.File.GetAttributes(dir);
                            if (this.AddDirectoryWillTraverseReparsePoints
                                || ((fileAttrs & FileAttributes.ReparsePoint) == 0)
                                )
                                AddOrUpdateDirectoryImpl(dir, rootDirectoryPathInArchive, action, recurse, level + 1);
                        }
                    }
                }
            }
            if (level == 0)
                OnAddCompleted();
            return baseDir;
        }
    }