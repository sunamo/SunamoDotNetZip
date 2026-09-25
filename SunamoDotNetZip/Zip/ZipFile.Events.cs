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
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
        ///
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