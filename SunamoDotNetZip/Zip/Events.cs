namespace Ionic.Zip;

// Events.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2006, 2007, 2008, 2009 Dino Chiesa and Microsoft Corporation.
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
// Time-stamp: <2011-August-06 12:26:24>
//
// ------------------------------------------------------------------
//
// This module defines events used by the ZipFile class.
//
//
    public delegate void WriteDelegate(string entryName, System.IO.Stream stream);
    public delegate System.IO.Stream OpenDelegate(string entryName);
    public delegate void CloseDelegate(string entryName, System.IO.Stream stream);
    public delegate CompressionLevel SetCompressionCallback(string localFileName, string fileNameInArchive);
    public enum ZipProgressEventType
    {
        Adding_Started,
        Adding_AfterAddEntry,
        Adding_Completed,
        Reading_Started,
        Reading_BeforeReadEntry,
        Reading_AfterReadEntry,
        Reading_Completed,
        Reading_ArchiveBytesRead,
        Saving_Started,
        Saving_BeforeWriteEntry,
        Saving_AfterWriteEntry,
        Saving_Completed,
        Saving_AfterSaveTempArchive,
        Saving_BeforeRenameTempArchive,
        Saving_AfterRenameTempArchive,
        Saving_AfterCompileSelfExtractor,
        Saving_EntryBytesRead,
        Extracting_BeforeExtractEntry,
        Extracting_AfterExtractEntry,
        Extracting_ExtractEntryWouldOverwrite,
        Extracting_EntryBytesWritten,
        Extracting_BeforeExtractAll,
        Extracting_AfterExtractAll,
        Error_Saving,
    }
    public class ZipProgressEventArgs : EventArgs
    {
        private int _entriesTotal;
        private bool _cancel;
        private ZipEntry _latestEntry;
        private ZipProgressEventType _flavor;
        private String _archiveName;
        private Int64 _bytesTransferred;
        private Int64 _totalBytesToTransfer;
        internal ZipProgressEventArgs() { }
        internal ZipProgressEventArgs(string archiveName, ZipProgressEventType flavor)
        {
            this._archiveName = archiveName;
            this._flavor = flavor;
        }
        public int EntriesTotal
        {
            get { return _entriesTotal; }
            set { _entriesTotal = value; }
        }
        public ZipEntry CurrentEntry
        {
            get { return _latestEntry; }
            set { _latestEntry = value; }
        }
        public bool Cancel
        {
            get { return _cancel; }
            set { _cancel = _cancel || value; }
        }
        public ZipProgressEventType EventType
        {
            get { return _flavor; }
            set { _flavor = value; }
        }
        public String ArchiveName
        {
            get { return _archiveName; }
            set { _archiveName = value; }
        }
        public Int64 BytesTransferred
        {
            get { return _bytesTransferred; }
            set { _bytesTransferred = value; }
        }
        public Int64 TotalBytesToTransfer
        {
            get { return _totalBytesToTransfer; }
            set { _totalBytesToTransfer = value; }
        }
    }
    public class ReadProgressEventArgs : ZipProgressEventArgs
    {
        internal ReadProgressEventArgs() { }
        private ReadProgressEventArgs(string archiveName, ZipProgressEventType flavor)
            : base(archiveName, flavor)
        { }
        internal static ReadProgressEventArgs Before(string archiveName, int entriesTotal)
        {
        var xValue = new ReadProgressEventArgs(archiveName, ZipProgressEventType.Reading_BeforeReadEntry)
        {
            EntriesTotal = entriesTotal
        };
        return xValue;
        }
        internal static ReadProgressEventArgs After(string archiveName, ZipEntry entry, int entriesTotal)
        {
        var xValue = new ReadProgressEventArgs(archiveName, ZipProgressEventType.Reading_AfterReadEntry)
        {
            EntriesTotal = entriesTotal,
            CurrentEntry = entry
        };
        return xValue;
        }
        internal static ReadProgressEventArgs Started(string archiveName)
        {
            var xValue = new ReadProgressEventArgs(archiveName, ZipProgressEventType.Reading_Started);
            return xValue;
        }
        internal static ReadProgressEventArgs ByteUpdate(string archiveName, ZipEntry entry, Int64 bytesXferred, Int64 totalBytes)
        {
        var xValue = new ReadProgressEventArgs(archiveName, ZipProgressEventType.Reading_ArchiveBytesRead)
        {
            CurrentEntry = entry,
            BytesTransferred = bytesXferred,
            TotalBytesToTransfer = totalBytes
        };
        return xValue;
        }
        internal static ReadProgressEventArgs Completed(string archiveName)
        {
            var xValue = new ReadProgressEventArgs(archiveName, ZipProgressEventType.Reading_Completed);
            return xValue;
        }
    }
    public class AddProgressEventArgs : ZipProgressEventArgs
    {
        internal AddProgressEventArgs() { }
        private AddProgressEventArgs(string archiveName, ZipProgressEventType flavor)
            : base(archiveName, flavor)
        { }
        internal static AddProgressEventArgs AfterEntry(string archiveName, ZipEntry entry, int entriesTotal)
        {
        var xValue = new AddProgressEventArgs(archiveName, ZipProgressEventType.Adding_AfterAddEntry)
        {
            EntriesTotal = entriesTotal,
            CurrentEntry = entry
        };
        return xValue;
        }
        internal static AddProgressEventArgs Started(string archiveName)
        {
            var xValue = new AddProgressEventArgs(archiveName, ZipProgressEventType.Adding_Started);
            return xValue;
        }
        internal static AddProgressEventArgs Completed(string archiveName)
        {
            var xValue = new AddProgressEventArgs(archiveName, ZipProgressEventType.Adding_Completed);
            return xValue;
        }
    }
    public class SaveProgressEventArgs : ZipProgressEventArgs
    {
        private readonly int _entriesSaved;
        internal SaveProgressEventArgs(string archiveName, bool before, int entriesTotal, int entriesSaved, ZipEntry entry)
            : base(archiveName, (before) ? ZipProgressEventType.Saving_BeforeWriteEntry : ZipProgressEventType.Saving_AfterWriteEntry)
        {
            this.EntriesTotal = entriesTotal;
            this.CurrentEntry = entry;
            this._entriesSaved = entriesSaved;
        }
        internal SaveProgressEventArgs() { }
        internal SaveProgressEventArgs(string archiveName, ZipProgressEventType flavor)
            : base(archiveName, flavor)
        { }
        internal static SaveProgressEventArgs ByteUpdate(string archiveName, ZipEntry entry, Int64 bytesXferred, Int64 totalBytes)
        {
        var xValue = new SaveProgressEventArgs(archiveName, ZipProgressEventType.Saving_EntryBytesRead)
        {
            ArchiveName = archiveName,
            CurrentEntry = entry,
            BytesTransferred = bytesXferred,
            TotalBytesToTransfer = totalBytes
        };
        return xValue;
        }
        internal static SaveProgressEventArgs Started(string archiveName)
        {
            var xValue = new SaveProgressEventArgs(archiveName, ZipProgressEventType.Saving_Started);
            return xValue;
        }
        internal static SaveProgressEventArgs Completed(string archiveName)
        {
            var xValue = new SaveProgressEventArgs(archiveName, ZipProgressEventType.Saving_Completed);
            return xValue;
        }
        public int EntriesSaved
        {
            get { return _entriesSaved; }
        }
    }
    public class ExtractProgressEventArgs : ZipProgressEventArgs
    {
        private readonly int _entriesExtracted;
        private string _target;
        internal ExtractProgressEventArgs(string archiveName, bool before, int entriesTotal, int entriesExtracted, ZipEntry entry, string extractLocation)
            : base(archiveName, (before) ? ZipProgressEventType.Extracting_BeforeExtractEntry : ZipProgressEventType.Extracting_AfterExtractEntry)
        {
            this.EntriesTotal = entriesTotal;
            this.CurrentEntry = entry;
            this._entriesExtracted = entriesExtracted;
            this._target = extractLocation;
        }
        internal ExtractProgressEventArgs(string archiveName, ZipProgressEventType flavor)
            : base(archiveName, flavor)
        { }
        internal ExtractProgressEventArgs()
        { }
        internal static ExtractProgressEventArgs BeforeExtractEntry(string archiveName, ZipEntry entry, string extractLocation)
        {
            var xValue = new ExtractProgressEventArgs
                {
                    ArchiveName = archiveName,
                    EventType = ZipProgressEventType.Extracting_BeforeExtractEntry,
                    CurrentEntry = entry,
                    _target = extractLocation,
                };
            return xValue;
        }
        internal static ExtractProgressEventArgs ExtractExisting(string archiveName, ZipEntry entry, string extractLocation)
        {
            var xValue = new ExtractProgressEventArgs
                {
                    ArchiveName = archiveName,
                    EventType = ZipProgressEventType.Extracting_ExtractEntryWouldOverwrite,
                    CurrentEntry = entry,
                    _target = extractLocation,
                };
            return xValue;
        }
        internal static ExtractProgressEventArgs AfterExtractEntry(string archiveName, ZipEntry entry, string extractLocation)
        {
            var xValue = new ExtractProgressEventArgs
                {
                    ArchiveName = archiveName,
                    EventType = ZipProgressEventType.Extracting_AfterExtractEntry,
                    CurrentEntry = entry,
                    _target = extractLocation,
                };
            return xValue;
        }
        internal static ExtractProgressEventArgs ExtractAllStarted(string archiveName, string extractLocation)
        {
        var xValue = new ExtractProgressEventArgs(archiveName, ZipProgressEventType.Extracting_BeforeExtractAll)
        {
            _target = extractLocation
        };
        return xValue;
        }
        internal static ExtractProgressEventArgs ExtractAllCompleted(string archiveName, string extractLocation)
        {
        var xValue = new ExtractProgressEventArgs(archiveName, ZipProgressEventType.Extracting_AfterExtractAll)
        {
            _target = extractLocation
        };
        return xValue;
        }
        internal static ExtractProgressEventArgs ByteUpdate(string archiveName, ZipEntry entry, Int64 bytesWritten, Int64 totalBytes)
        {
        var xValue = new ExtractProgressEventArgs(archiveName, ZipProgressEventType.Extracting_EntryBytesWritten)
        {
            ArchiveName = archiveName,
            CurrentEntry = entry,
            BytesTransferred = bytesWritten,
            TotalBytesToTransfer = totalBytes
        };
        return xValue;
        }
        public int EntriesExtracted
        {
            get { return _entriesExtracted; }
        }
        public String ExtractLocation
        {
            get { return _target; }
        }
    }
    public class ZipErrorEventArgs : ZipProgressEventArgs
    {
        private Exception _exc;
        private ZipErrorEventArgs() { }
        internal static ZipErrorEventArgs Saving(string archiveName, ZipEntry entry, Exception exception)
        {
            var xValue = new ZipErrorEventArgs
                {
                    EventType = ZipProgressEventType.Error_Saving,
                    ArchiveName = archiveName,
                    CurrentEntry = entry,
                    _exc = exception
                };
            return xValue;
        }
        public Exception @Exception
        {
            get { return _exc; }
        }
        public String FileName
        {
            get { return CurrentEntry.LocalFileName; }
        }
    }