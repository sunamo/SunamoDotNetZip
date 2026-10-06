namespace Ionic.Zip;

// ZipFile.Events.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2008, 2009, 2011 Dino Chiesa .
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
// Time-stamp: <2011-July-09 08:42:35>
//
// ------------------------------------------------------------------
//
// This module defines the methods for issuing events from the ZipFile class.
//
// ------------------------------------------------------------------
//
    public partial class ZipFile
    {
        private string ArchiveNameForEvent
        {
            get
            {
                return (_name != null) ? _name : "(stream)";
            }
        }
        #region Save
        /// <summary>
        ///   An event handler invoked when a Save() starts, before and after each
        ///   entry has been written to the archive, when a Save() completes, and
        ///   during other Save events.
        /// </summary>
        ///
        /// <remarks>
        /// <para>
        ///   Depending on the particular event, different properties on the <see
        ///   cref="SaveProgressEventArgs"/> parameter are set.  The following
        ///   table summarizes the available EventTypes and the conditions under
        ///   which this event handler is invoked with a
        ///   <c>SaveProgressEventArgs</c> with the given EventType.
        /// </para>
        ///
        /// <list type="table">
        /// <listheader>
        /// <term>value of EntryType</term>
        /// <description>Meaning and conditions</description>
        /// </listheader>
        ///
        /// <item>
        /// <term>ZipProgressEventType.Saving_Started</term>
        /// <description>Fired when ZipFile.Save() begins.
        /// </description>
        /// </item>
        ///
        /// <item>
        /// <term>ZipProgressEventType.Saving_BeforeSaveEntry</term>
        /// <description>
        ///   Fired within ZipFile.Save(), just before writing data for each
        ///   particular entry.
        /// </description>
        /// </item>
        ///
        /// <item>
        /// <term>ZipProgressEventType.Saving_AfterSaveEntry</term>
        /// <description>
        ///   Fired within ZipFile.Save(), just after having finished writing data
        ///   for each particular entry.
        /// </description>
        /// </item>
        ///
        /// <item>
        /// <term>ZipProgressEventType.Saving_Completed</term>
        /// <description>Fired when ZipFile.Save() has completed.
        /// </description>
        /// </item>
        ///
        /// <item>
        /// <term>ZipProgressEventType.Saving_AfterSaveTempArchive</term>
        /// <description>
        ///   Fired after the temporary file has been created.  This happens only
        ///   when saving to a disk file.  This event will not be invoked when
        ///   saving to a stream.
        /// </description>
        /// </item>
        ///
        /// <item>
        /// <term>ZipProgressEventType.Saving_BeforeRenameTempArchive</term>
        /// <description>
        ///   Fired just before renaming the temporary file to the permanent
        ///   location.  This happens only when saving to a disk file.  This event
        ///   will not be invoked when saving to a stream.
        /// </description>
        /// </item>
        ///
        /// <item>
        /// <term>ZipProgressEventType.Saving_AfterRenameTempArchive</term>
        /// <description>
        ///   Fired just after renaming the temporary file to the permanent
        ///   location.  This happens only when saving to a disk file.  This event
        ///   will not be invoked when saving to a stream.
        /// </description>
        /// </item>
        ///
        /// <item>
        /// <term>ZipProgressEventType.Saving_AfterCompileSelfExtractor</term>
        /// <description>
        ///   Fired after a self-extracting archive has finished compiling.  This
        ///   EventType is used only within SaveSelfExtractor().
        /// </description>
        /// </item>
        ///
        /// <item>
        /// <term>ZipProgressEventType.Saving_BytesRead</term>
        /// <description>
        ///   Set during the save of a particular entry, to update progress of the
        ///   Save().  When this EventType is set, the BytesTransferred is the
        ///   number of bytes that have been read from the source stream.  The
        ///   TotalBytesToTransfer is the number of bytes in the uncompressed
        ///   file.
        /// </description>
        /// </item>
        ///
        /// </list>
        /// </remarks>
        ///
        /// <example>
        ///
        ///    This example uses an anonymous method to handle the
        ///    SaveProgress event, by updating a progress bar.
        ///
        /// <code lang="C#">
        /// progressBar1.Value = 0;
        /// progressBar1.Max = listbox1.Items.Count;
        /// using (ZipFile zip = new ZipFile())
        /// {
        ///    // listbox1 contains a list of filenames
        ///    zip.AddFiles(listbox1.Items);
        ///
        ///    // do the progress bar:
        ///    zip.SaveProgress += (sender, element) => {
        ///       if (element.EventType == ZipProgressEventType.Saving_BeforeWriteEntry) {
        ///          progressBar1.PerformStep();
        ///       }
        ///    };
        ///
        ///    zip.Save(fs);
        /// }
        /// </code>
        /// </example>
        ///
        /// <example>
        ///   This example uses a named method as the
        ///   <c>SaveProgress</c> event handler, to update the user, in a
        ///   console-based application.
        ///
        /// <code lang="C#">
        /// static bool justHadByteUpdate= false;
        /// public static void SaveProgress(object sender, SaveProgressEventArgs element)
        /// {
        ///     if (element.EventType == ZipProgressEventType.Saving_Started)
        ///         Console.WriteLine("Saving: {0}", element.ArchiveName);
        ///
        ///     else if (element.EventType == ZipProgressEventType.Saving_Completed)
        ///     {
        ///         justHadByteUpdate= false;
        ///         Console.WriteLine();
        ///         Console.WriteLine("Done: {0}", element.ArchiveName);
        ///     }
        ///
        ///     else if (element.EventType == ZipProgressEventType.Saving_BeforeWriteEntry)
        ///     {
        ///         if (justHadByteUpdate)
        ///             Console.WriteLine();
        ///         Console.WriteLine("  Writing: {0} ({1}/{2})",
        ///                           element.CurrentEntry.FileName, element.EntriesSaved, element.EntriesTotal);
        ///         justHadByteUpdate= false;
        ///     }
        ///
        ///     else if (element.EventType == ZipProgressEventType.Saving_EntryBytesRead)
        ///     {
        ///         if (justHadByteUpdate)
        ///             Console.SetCursorPosition(0, Console.CursorTop);
        ///          Console.Write("     {0}/{1} ({2:N0}%)", element.BytesTransferred, element.TotalBytesToTransfer,
        ///                       element.BytesTransferred / (0.01 * element.TotalBytesToTransfer ));
        ///         justHadByteUpdate= true;
        ///     }
        /// }
        ///
        /// public static ZipUp(string targetZip, string directory)
        /// {
        ///   using (var zip = new ZipFile()) {
        ///     zip.SaveProgress += SaveProgress;
        ///     zip.AddDirectory(directory);
        ///     zip.Save(targetZip);
        ///   }
        /// }
        ///
        /// </code>
        ///
        /// <code lang="VB">
        /// Public Sub ZipUp(ByVal targetZip As String, ByVal directory As String)
        ///     Using zip As ZipFile = New ZipFile
        ///         AddHandler zip.SaveProgress, AddressOf MySaveProgress
        ///         zip.AddDirectory(directory)
        ///         zip.Save(targetZip)
        ///     End Using
        /// End Sub
        ///
        /// Private Shared justHadByteUpdate As Boolean = False
        ///
        /// Public Shared Sub MySaveProgress(ByVal sender As Object, ByVal element As SaveProgressEventArgs)
        ///     If (element.EventType Is ZipProgressEventType.Saving_Started) Then
        ///         Console.WriteLine("Saving: {0}", element.ArchiveName)
        ///
        ///     ElseIf (element.EventType Is ZipProgressEventType.Saving_Completed) Then
        ///         justHadByteUpdate = False
        ///         Console.WriteLine
        ///         Console.WriteLine("Done: {0}", element.ArchiveName)
        ///
        ///     ElseIf (element.EventType Is ZipProgressEventType.Saving_BeforeWriteEntry) Then
        ///         If justHadByteUpdate Then
        ///             Console.WriteLine
        ///         End If
        ///         Console.WriteLine("  Writing: {0} ({1}/{2})", element.CurrentEntry.FileName, element.EntriesSaved, element.EntriesTotal)
        ///         justHadByteUpdate = False
        ///
        ///     ElseIf (element.EventType Is ZipProgressEventType.Saving_EntryBytesRead) Then
        ///         If justHadByteUpdate Then
        ///             Console.SetCursorPosition(0, Console.CursorTop)
        ///         End If
        ///         Console.Write("     {0}/{1} ({2:N0}%)", element.BytesTransferred, _
        ///                       element.TotalBytesToTransfer, _
        ///                       (CDbl(element.BytesTransferred) / (0.01 * element.TotalBytesToTransfer)))
        ///         justHadByteUpdate = True
        ///     End If
        /// End Sub
        /// </code>
        /// </example>
        ///
        /// <example>
        ///
        /// This is a more complete example of using the SaveProgress
        /// events in a Windows Forms application, with a
        /// Thread object.
        ///
        /// <code lang="C#">
        /// delegate void SaveEntryProgress(SaveProgressEventArgs element);
        /// delegate void ButtonClick(object sender, EventArgs element);
        ///
        /// public class WorkerOptions
        /// {
        ///     public string ZipName;
        ///     public string Folder;
        ///     public string Encoding;
        ///     public string Comment;
        ///     public int ZipFlavor;
        ///     public Zip64Option Zip64;
        /// }
        ///
        /// private int _progress2MaxFactor;
        /// private bool _saveCanceled;
        /// private long _totalBytesBeforeCompress;
        /// private long _totalBytesAfterCompress;
        /// private Thread _workerThread;
        ///
        ///
        /// private void btnZipup_Click(object sender, EventArgs element)
        /// {
        ///     KickoffZipup();
        /// }
        ///
        /// private void btnCancel_Click(object sender, EventArgs element)
        /// {
        ///     if (this.lblStatus.InvokeRequired)
        ///     {
        ///         this.lblStatus.Invoke(new ButtonClick(this.btnCancel_Click), new object[] { sender, element });
        ///     }
        ///     else
        ///     {
        ///         _saveCanceled = true;
        ///         lblStatus.Text = "Canceled...";
        ///         ResetState();
        ///     }
        /// }
        ///
        /// private void KickoffZipup()
        /// {
        ///     _folderName = tbDirName.Text;
        ///
        ///     if (_folderName == null || _folderName == "") return;
        ///     if (this.tbZipName.Text == null || this.tbZipName.Text == "") return;
        ///
        ///     // check for existence of the zip file:
        ///     if (System.IO.File.Exists(this.tbZipName.Text))
        ///     {
        ///         var dlgResult = MessageBox.Show(String.Format("The file you have specified ({0}) already exists." +
        ///                                                       "  Do you want to overwrite this file?", this.tbZipName.Text),
        ///                                         "Confirmation is Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        ///         if (dlgResult != DialogResult.Yes) return;
        ///         System.IO.File.Delete(this.tbZipName.Text);
        ///     }
        ///
        ///      _saveCanceled = false;
        ///     _nFilesCompleted = 0;
        ///     _totalBytesAfterCompress = 0;
        ///     _totalBytesBeforeCompress = 0;
        ///     this.btnOk.Enabled = false;
        ///     this.btnOk.Text = "Zipping...";
        ///     this.btnCancel.Enabled = true;
        ///     lblStatus.Text = "Zipping...";
        ///
        ///     var options = new WorkerOptions
        ///     {
        ///         ZipName = this.tbZipName.Text,
        ///         Folder = _folderName,
        ///         Encoding = "ibm437"
        ///     };
        ///
        ///     if (this.comboBox1.SelectedIndex != 0)
        ///     {
        ///         options.Encoding = this.comboBox1.SelectedItem.ToString();
        ///     }
        ///
        ///     if (this.radioFlavorSfxCmd.Checked)
        ///         options.ZipFlavor = 2;
        ///     else if (this.radioFlavorSfxGui.Checked)
        ///         options.ZipFlavor = 1;
        ///     else options.ZipFlavor = 0;
        ///
        ///     if (this.radioZip64AsNecessary.Checked)
        ///         options.Zip64 = Zip64Option.AsNecessary;
        ///     else if (this.radioZip64Always.Checked)
        ///         options.Zip64 = Zip64Option.Always;
        ///     else options.Zip64 = Zip64Option.Never;
        ///
        ///     options.Comment = String.Format("Encoding:{0} || Flavor:{1} || ZIP64:{2}\r\nCreated at {3} || {4}\r\n",
        ///                 options.Encoding,
        ///                 FlavorToString(options.ZipFlavor),
        ///                 options.Zip64.ToString(),
        ///                 System.DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss"),
        ///                 this.Text);
        ///
        ///     if (this.tbComment.Text != TB_COMMENT_NOTE)
        ///         options.Comment += this.tbComment.Text;
        ///
        ///     _workerThread = new Thread(this.DoSave);
        ///     _workerThread.Name = "Zip Saver thread";
        ///     _workerThread.Start(options);
        ///     this.Cursor = Cursors.WaitCursor;
        ///  }
        ///
        ///
        /// private void DoSave(Object p)
        /// {
        ///     WorkerOptions options = p as WorkerOptions;
        ///     try
        ///     {
        ///         using (var zip1 = new ZipFile())
        ///         {
        ///             zip1.ProvisionalAlternateEncoding = System.Text.Encoding.GetEncoding(options.Encoding);
        ///             zip1.Comment = options.Comment;
        ///             zip1.AddDirectory(options.Folder);
        ///             _entriesToZip = zip1.EntryFileNames.Count;
        ///             SetProgressBars();
        ///             zip1.SaveProgress += this.zip1_SaveProgress;
        ///
        ///             zip1.UseZip64WhenSaving = options.Zip64;
        ///
        ///             if (options.ZipFlavor == 1)
        ///                 zip1.SaveSelfExtractor(options.ZipName, SelfExtractorFlavor.WinFormsApplication);
        ///             else if (options.ZipFlavor == 2)
        ///                 zip1.SaveSelfExtractor(options.ZipName, SelfExtractorFlavor.ConsoleApplication);
        ///             else
        ///                 zip1.Save(options.ZipName);
        ///         }
        ///     }
        ///     catch (System.Exception exc1)
        ///     {
        ///         MessageBox.Show(String.Format("Exception while zipping: {0}", exc1.Message));
        ///         btnCancel_Click(null, null);
        ///     }
        /// }
        ///
        ///
        ///
        /// void zip1_SaveProgress(object sender, SaveProgressEventArgs element)
        /// {
        ///     switch (element.EventType)
        ///     {
        ///         case ZipProgressEventType.Saving_AfterWriteEntry:
        ///             StepArchiveProgress(element);
        ///             break;
        ///         case ZipProgressEventType.Saving_EntryBytesRead:
        ///             StepEntryProgress(element);
        ///             break;
        ///         case ZipProgressEventType.Saving_Completed:
        ///             SaveCompleted();
        ///             break;
        ///         case ZipProgressEventType.Saving_AfterSaveTempArchive:
        ///             // this event only occurs when saving an SFX file
        ///             TempArchiveSaved();
        ///             break;
        ///     }
        ///     if (_saveCanceled)
        ///         element.Cancel = true;
        /// }
        ///
        ///
        ///
        /// private void StepArchiveProgress(SaveProgressEventArgs element)
        /// {
        ///     if (this.progressBar1.InvokeRequired)
        ///     {
        ///         this.progressBar1.Invoke(new SaveEntryProgress(this.StepArchiveProgress), new object[] { element });
        ///     }
        ///     else
        ///     {
        ///         if (!_saveCanceled)
        ///         {
        ///             _nFilesCompleted++;
        ///             this.progressBar1.PerformStep();
        ///             _totalBytesAfterCompress += element.CurrentEntry.CompressedSize;
        ///             _totalBytesBeforeCompress += element.CurrentEntry.UncompressedSize;
        ///
        ///             // reset the progress bar for the entry:
        ///             this.progressBar2.Value = this.progressBar2.Maximum = 1;
        ///
        ///             this.Update();
        ///         }
        ///     }
        /// }
        ///
        ///
        /// private void StepEntryProgress(SaveProgressEventArgs element)
        /// {
        ///     if (this.progressBar2.InvokeRequired)
        ///     {
        ///         this.progressBar2.Invoke(new SaveEntryProgress(this.StepEntryProgress), new object[] { element });
        ///     }
        ///     else
        ///     {
        ///         if (!_saveCanceled)
        ///         {
        ///             if (this.progressBar2.Maximum == 1)
        ///             {
        ///                 // reset
        ///                 Int64 max = element.TotalBytesToTransfer;
        ///                 _progress2MaxFactor = 0;
        ///                 while (max > System.Int32.MaxValue)
        ///                 {
        ///                     max /= 2;
        ///                     _progress2MaxFactor++;
        ///                 }
        ///                 this.progressBar2.Maximum = (int)max;
        ///                 lblStatus.Text = String.Format("{0} of {1} files...({2})",
        ///                     _nFilesCompleted + 1, _entriesToZip, element.CurrentEntry.FileName);
        ///             }
        ///
        ///              int xferred = element.BytesTransferred >> _progress2MaxFactor;
        ///
        ///              this.progressBar2.Value = (xferred >= this.progressBar2.Maximum)
        ///                 ? this.progressBar2.Maximum
        ///                 : xferred;
        ///
        ///              this.Update();
        ///         }
        ///     }
        /// }
        ///
        /// private void SaveCompleted()
        /// {
        ///     if (this.lblStatus.InvokeRequired)
        ///     {
        ///         this.lblStatus.Invoke(new MethodInvoker(this.SaveCompleted));
        ///     }
        ///     else
        ///     {
        ///         lblStatus.Text = String.Format("Done, Compressed {0} files, {1:N0}% of original.",
        ///             _nFilesCompleted, (100.00 * _totalBytesAfterCompress) / _totalBytesBeforeCompress);
        ///          ResetState();
        ///     }
        /// }
        ///
        /// private void ResetState()
        /// {
        ///     this.btnCancel.Enabled = false;
        ///     this.btnOk.Enabled = true;
        ///     this.btnOk.Text = "Zip it!";
        ///     this.progressBar1.Value = 0;
        ///     this.progressBar2.Value = 0;
        ///     this.Cursor = Cursors.Default;
        ///     if (!_workerThread.IsAlive)
        ///         _workerThread.Join();
        /// }
        /// </code>
        ///
        /// </example>
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.ReadProgress"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddProgress"/>
        /// <seealso cref="Ionic.Zip.ZipFile.ExtractProgress"/>
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        public event EventHandler<SaveProgressEventArgs> SaveProgress;
        internal bool OnSaveBlock(ZipEntry entry, Int64 bytesXferred, Int64 totalBytesToXfer)
        {
            EventHandler<SaveProgressEventArgs> sp = SaveProgress;
            if (sp != null)
            {
                var element = SaveProgressEventArgs.ByteUpdate(ArchiveNameForEvent, entry,
                                                         bytesXferred, totalBytesToXfer);
                sp(this, element);
                if (element.Cancel)
                    _saveOperationCanceled = true;
            }
            return _saveOperationCanceled;
        }
        private void OnSaveEntry(int current, ZipEntry entry, bool before)
        {
            EventHandler<SaveProgressEventArgs> sp = SaveProgress;
            if (sp != null)
            {
                var element = new SaveProgressEventArgs(ArchiveNameForEvent, before, _entries.Count, current, entry);
                sp(this, element);
                if (element.Cancel)
                    _saveOperationCanceled = true;
            }
        }
        private void OnSaveEvent(ZipProgressEventType eventFlavor)
        {
            EventHandler<SaveProgressEventArgs> sp = SaveProgress;
            if (sp != null)
            {
                var element = new SaveProgressEventArgs(ArchiveNameForEvent, eventFlavor);
                sp(this, element);
                if (element.Cancel)
                    _saveOperationCanceled = true;
            }
        }
        private void OnSaveStarted()
        {
            EventHandler<SaveProgressEventArgs> sp = SaveProgress;
            if (sp != null)
            {
                var element = SaveProgressEventArgs.Started(ArchiveNameForEvent);
                sp(this, element);
                if (element.Cancel)
                    _saveOperationCanceled = true;
            }
        }
        private void OnSaveCompleted()
        {
            EventHandler<SaveProgressEventArgs> sp = SaveProgress;
            if (sp != null)
            {
                var element = SaveProgressEventArgs.Completed(ArchiveNameForEvent);
                sp(this, element);
            }
        }
        #endregion
        #region Read
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.SaveProgress"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddProgress"/>
        /// <seealso cref="Ionic.Zip.ZipFile.ExtractProgress"/>
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        public event EventHandler<ReadProgressEventArgs> ReadProgress;
        private void OnReadStarted()
        {
            EventHandler<ReadProgressEventArgs> rp = ReadProgress;
            if (rp != null)
            {
                    var element = ReadProgressEventArgs.Started(ArchiveNameForEvent);
                    rp(this, element);
            }
        }
        private void OnReadCompleted()
        {
            EventHandler<ReadProgressEventArgs> rp = ReadProgress;
            if (rp != null)
            {
                    var element = ReadProgressEventArgs.Completed(ArchiveNameForEvent);
                    rp(this, element);
            }
        }
        internal void OnReadBytes(ZipEntry entry)
        {
            EventHandler<ReadProgressEventArgs> rp = ReadProgress;
            if (rp != null)
            {
                    var element = ReadProgressEventArgs.ByteUpdate(ArchiveNameForEvent,
                                        entry,
                                        ReadStream.Position,
                                        LengthOfReadStream);
                    rp(this, element);
            }
        }
        internal void OnReadEntry(bool before, ZipEntry entry)
        {
            EventHandler<ReadProgressEventArgs> rp = ReadProgress;
            if (rp != null)
            {
                ReadProgressEventArgs element = (before)
                    ? ReadProgressEventArgs.Before(ArchiveNameForEvent, _entries.Count)
                    : ReadProgressEventArgs.After(ArchiveNameForEvent, entry, _entries.Count);
                rp(this, element);
            }
        }
        private Int64 _lengthOfReadStream = -99;
        private Int64 LengthOfReadStream
        {
            get
            {
                if (_lengthOfReadStream == -99)
                {
                    _lengthOfReadStream = (_ReadStreamIsOurs)
                        ? SharedUtilities.GetFileLength(_name)
                        : -1L;
                }
                return _lengthOfReadStream;
            }
        }
        #endregion
        #region Extract
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.SaveProgress"/>
        /// <seealso cref="Ionic.Zip.ZipFile.ReadProgress"/>
        /// <seealso cref="Ionic.Zip.ZipFile.AddProgress"/>
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        public event EventHandler<ExtractProgressEventArgs> ExtractProgress;
        private void OnExtractEntry(int current, bool before, ZipEntry currentEntry, string path)
        {
            EventHandler<ExtractProgressEventArgs> ep = ExtractProgress;
            if (ep != null)
            {
                var element = new ExtractProgressEventArgs(ArchiveNameForEvent, before, _entries.Count, current, currentEntry, path);
                ep(this, element);
                if (element.Cancel)
                    _extractOperationCanceled = true;
            }
        }
        // Can be called from within ZipEntry._ExtractOne.
        internal bool OnExtractBlock(ZipEntry entry, Int64 bytesWritten, Int64 totalBytesToWrite)
        {
            EventHandler<ExtractProgressEventArgs> ep = ExtractProgress;
            if (ep != null)
            {
                var element = ExtractProgressEventArgs.ByteUpdate(ArchiveNameForEvent, entry,
                                                            bytesWritten, totalBytesToWrite);
                ep(this, element);
                if (element.Cancel)
                    _extractOperationCanceled = true;
            }
            return _extractOperationCanceled;
        }
        // Can be called from within ZipEntry.InternalExtract.
        internal bool OnSingleEntryExtract(ZipEntry entry, string path, bool before)
        {
            EventHandler<ExtractProgressEventArgs> ep = ExtractProgress;
            if (ep != null)
            {
                var element = (before)
                    ? ExtractProgressEventArgs.BeforeExtractEntry(ArchiveNameForEvent, entry, path)
                    : ExtractProgressEventArgs.AfterExtractEntry(ArchiveNameForEvent, entry, path);
                ep(this, element);
                if (element.Cancel)
                    _extractOperationCanceled = true;
            }
            return _extractOperationCanceled;
        }
        internal bool OnExtractExisting(ZipEntry entry, string path)
        {
            EventHandler<ExtractProgressEventArgs> ep = ExtractProgress;
            if (ep != null)
            {
                var element = ExtractProgressEventArgs.ExtractExisting(ArchiveNameForEvent, entry, path);
                ep(this, element);
                if (element.Cancel)
                    _extractOperationCanceled = true;
            }
            return _extractOperationCanceled;
        }
        private void OnExtractAllCompleted(string path)
        {
            EventHandler<ExtractProgressEventArgs> ep = ExtractProgress;
            if (ep != null)
            {
                var element = ExtractProgressEventArgs.ExtractAllCompleted(ArchiveNameForEvent,
                                                                     path );
                ep(this, element);
            }
        }
        private void OnExtractAllStarted(string path)
        {
            EventHandler<ExtractProgressEventArgs> ep = ExtractProgress;
            if (ep != null)
            {
                var element = ExtractProgressEventArgs.ExtractAllStarted(ArchiveNameForEvent,
                                                                   path );
                ep(this, element);
            }
        }
        #endregion
        #region Add
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.SaveProgress"/>
        /// <seealso cref="Ionic.Zip.ZipFile.ReadProgress"/>
        /// <seealso cref="Ionic.Zip.ZipFile.ExtractProgress"/>
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        public event EventHandler<AddProgressEventArgs> AddProgress;
        private void OnAddStarted()
        {
            EventHandler<AddProgressEventArgs> ap = AddProgress;
            if (ap != null)
            {
                var element = AddProgressEventArgs.Started(ArchiveNameForEvent);
                ap(this, element);
                if (element.Cancel) // workitem 13371
                    _addOperationCanceled = true;
            }
        }
        private void OnAddCompleted()
        {
            EventHandler<AddProgressEventArgs> ap = AddProgress;
            if (ap != null)
            {
                var element = AddProgressEventArgs.Completed(ArchiveNameForEvent);
                ap(this, element);
            }
        }
        internal void AfterAddEntry(ZipEntry entry)
        {
            EventHandler<AddProgressEventArgs> ap = AddProgress;
            if (ap != null)
            {
                var element = AddProgressEventArgs.AfterEntry(ArchiveNameForEvent, entry, _entries.Count);
                ap(this, element);
                if (element.Cancel) // workitem 13371
                    _addOperationCanceled = true;
            }
        }
        #endregion
        #region Error
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        /// <seealso cref="Ionic.Zip.ZipFile.ZipErrorAction"/>
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        public event EventHandler<ZipErrorEventArgs> ZipError;
        internal bool OnZipErrorSaving(ZipEntry entry, Exception exc)
        {
            if (ZipError != null)
            {
                lock (LOCK)
                {
                    var element = ZipErrorEventArgs.Saving(this.Name, entry, exc);
                    ZipError(this, element);
                    if (element.Cancel)
                        _saveOperationCanceled = true;
                }
            }
            return _saveOperationCanceled;
        }
        #endregion
    }