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
    /// <returns>The <c>ZipEntry</c> added.</returns>
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
    /// <code lang="VB">
    ///   Dim itemnames As String() = _
    ///     New String() { "c:\fixedContent\Readme.txt", _
    ///                    "MyProposal.docx", _
    ///                    "SupportFiles", _
    ///                    "images\Image1.jpg" }
    ///   Try
    ///       Using zip As New ZipFile
    ///           Dim i As Integer
    ///           For i = 1 To itemnames.Length - 1
    ///               ' will add Files or Dirs, recursing and flattening subdirectories.
    ///               zip.AddItem(itemnames(i), "flat")
    ///           Next i
    ///           zip.Save(ZipToCreate)
    ///       End Using
    ///   Catch ex1 As Exception
    ///       Console.Error.WriteLine("exception: {0}", ex1.ToString())
    ///   End Try
    /// </code>
    /// </example>
    /// <returns>The <c>ZipEntry</c> added.</returns>
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
    /// <param name="fileName">
    ///   The name of the file to add. It should refer to a file in the filesystem.
    ///   The name of the file may be a relative path or a fully-qualified path.
    /// </param>
    /// <returns>The <c>ZipEntry</c> corresponding to the File added.</returns>
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
    /// <returns>The <c>ZipEntry</c> corresponding to the file added.</returns>
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
        /// <seealso cref="Ionic.Zip.ZipFile.SelectEntries(String)" />
        /// <seealso cref="Ionic.Zip.ZipFile.RemoveSelectedEntries(String)" />
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
        /// <seealso cref="Ionic.Zip.ZipFile.SelectEntries(String)" />
        /// <seealso cref="Ionic.Zip.ZipFile.RemoveSelectedEntries(String)" />
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
    /// <seealso cref="Ionic.Zip.ZipFile.AddSelectedFiles(String, String)" />
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
    ///
    ///
    ///
    public void UpdateFiles(System.Collections.Generic.IEnumerable<String> fileNames) => this.UpdateFiles(fileNames, null);
    ///
    ///
    ///
    ///
    ///
    /// <seealso cref="Ionic.Zip.ZipFile.AddSelectedFiles(String, String)" />
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
    /// <param name="preserveDirHierarchy">
    ///   whether the entries in the zip archive will reflect the directory
    ///   hierarchy that is present in the various filenames.  For example, if
    ///   <paramref name="fileNames"/> includes two paths,
    ///   \Animalia\Chordata\Mammalia\Info.txt and
    ///   \Plantae\Magnoliophyta\Dicotyledon\Info.txt, then calling this method
    ///   with <paramref name="preserveDirHierarchy"/> = <c>false</c> will
    ///   result in an exception because of a duplicate entry name, while
    ///   calling this method with <paramref name="preserveDirHierarchy"/> =
    ///   <c>true</c> will result in the full direcory paths being included in
    ///   the entries added to the ZipFile.
    /// </param>
    /// <seealso cref="Ionic.Zip.ZipFile.AddSelectedFiles(String, String)" />
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
        /// <seealso cref="Ionic.Zip.ZipFile.AddSelectedFiles(String, String)" />
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
    /// <returns>
    ///   The <c>ZipEntry</c> corresponding to the File that was added or updated.
    /// </returns>
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
    /// <returns>
    ///   The <c>ZipEntry</c> corresponding to the File that was added or updated.
    /// </returns>
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
    /// <returns>
    /// The <c>ZipEntry</c> corresponding to the Directory that was added or updated.
    /// </returns>
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
    /// <returns>
    ///   The <c>ZipEntry</c> corresponding to the Directory that was added or updated.
    /// </returns>
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
    /// <param name="itemName">
    ///  the path to the file or directory to be added or updated.
    /// </param>
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
    /// <param name="itemName">
    ///   The path for the File or Directory to be added or updated.
    /// </param>
    /// <param name="directoryPathInArchive">
    ///   Specifies a directory path to use to override any path in the
    ///   <c>itemName</c>.  This path may, or may not, correspond to a real
    ///   directory in the current filesystem.  If the files within the zip are
    ///   later extracted, this is the path used for the extracted file.  Passing
    ///   <c>null</c> (<c>Nothing</c> in VB) will use the path on the
    ///   <c>itemName</c>, if any.  Passing the empty string ("") will insert the
    ///   item at the root path within the archive.
    /// </param>
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
    /// </code>
    /// <code lang="VB">
    /// Public Sub Run()
    ///   Dim Content As String = "This string will be the content of the Readme.txt file in the zip archive."
    ///   Using zip1 As ZipFile = New ZipFile
    ///     zip1.AddEntry("Readme.txt", Content)
    ///     zip1.AddFile("MyDocuments\Resume.doc", "files")
    ///     zip1.Comment = ("This zip file was created at " &amp; DateTime.Now.ToString("G"))
    ///     zip1.Save("Content.zip")
    ///   End Using
    /// End Sub
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
        /// <param name="entryName">
        ///   The name, including any path, which is shown in the zip file for the added
        ///   entry.
        /// </param>
        /// <param name="stream">
        ///   The input stream from which to grab content for the file
        /// </param>
        /// <returns>The <c>ZipEntry</c> added.</returns>
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
        /// Public Sub Run()
        ///     Using zip = New ZipFile
        ///         zip.AddEntry(zipEntryName, New WriteDelegate(AddressOf WriteEntry))
        ///         zip.Save(zipFileName)
        ///     End Using
        /// End Sub
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
        /// <param name="stream">The input stream from which to read file data.</param>
        /// <returns>The <c>ZipEntry</c> added.</returns>
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
    /// <param name="directoryName">The name of the directory to add.</param>
    /// <returns>The <c>ZipEntry</c> added.</returns>
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
    /// <returns>The <c>ZipEntry</c> added.</returns>
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
    /// <param name="directoryNameInArchive">
    ///   The name of the directory to create in the archive.
    /// </param>
    /// <returns>The <c>ZipEntry</c> added.</returns>
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