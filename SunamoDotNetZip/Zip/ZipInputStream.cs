namespace Ionic.Zip;

// ZipInputStream.cs
//
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2010 Dino Chiesa.
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
// Time-stamp: <2011-July-31 14:48:30>
//
// ------------------------------------------------------------------
//
// This module defines the ZipInputStream class, which is a stream metaphor for
// reading zip files.  This class does not depend on Ionic.Zip.ZipFile, but rather
// stands alongside it as an alternative "container" for ZipEntry, when reading zips.
//
// It adds one interesting method to the normal "stream" interface: GetNextEntry.
//
// ------------------------------------------------------------------
//
    public class ZipInputStream : Stream
    {
        public ZipInputStream(Stream stream)  : this (stream, false) { }
        public ZipInputStream(String fileName)
        {
            Stream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read );
            _Init(stream, false, fileName);
        }
        public ZipInputStream(Stream stream, bool leaveOpen)
        {
            _Init(stream, leaveOpen, null);
        }
        private void _Init(Stream stream, bool leaveOpen, string name)
        {
            _inputStream = stream;
            if (!_inputStream.CanRead)
                throw new ZipException("The stream must be readable.");
            _container= new ZipContainer(this);
            _provisionalAlternateEncoding = System.Text.Encoding.GetEncoding("IBM437");
            _leaveUnderlyingStreamOpen = leaveOpen;
            _findRequired= true;
            _name = name ?? "(stream)";
        }
    public override String ToString() => String.Format("ZipInputStream::{0}(leaveOpen({1})))", _name, _leaveUnderlyingStreamOpen);
    public System.Text.Encoding ProvisionalAlternateEncoding
        {
            get
            {
                return _provisionalAlternateEncoding;
            }
            set
            {
                _provisionalAlternateEncoding = value;
            }
        }
        public int CodecBufferSize
        {
            get;
            set;
        }
        public String Password
        {
            set
            {
                if (_closed)
                {
                    _exceptionPending = true;
                    throw new System.InvalidOperationException("The stream has been closed.");
                }
                _Password = value;
            }
        }
        private void SetupStream()
        {
            // Seek to the correct posn in the file, and open a
            // stream that can be read.
            _crcStream= _currentEntry.InternalOpenReader(_Password);
            _LeftToRead = _crcStream.Length;
            _needSetup = false;
        }
        internal Stream ReadStream
        {
            get
            {
                return _inputStream;
            }
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_closed)
            {
                _exceptionPending = true;
                throw new System.InvalidOperationException("The stream has been closed.");
            }
            if (_needSetup)
                SetupStream();
            if (_LeftToRead == 0) return 0;
            int len = (_LeftToRead > count) ? count : (int)_LeftToRead;
            int bytesRead = _crcStream.Read(buffer, offset, len);
            _LeftToRead -= bytesRead;
            if (_LeftToRead == 0)
            {
                int CrcResult = _crcStream.Crc;
                _currentEntry.VerifyCrcAfterExtract(CrcResult, _currentEntry.Encryption, _currentEntry._Crc32, _currentEntry.ArchiveStream, _currentEntry.UncompressedSize);
                _inputStream.Seek(_endOfEntry, SeekOrigin.Begin);
            }
            return bytesRead;
        }
        public ZipEntry GetNextEntry()
        {
            if (_findRequired)
            {
                // find the next signature
                long bytesSkipped = SharedUtilities.FindSignature(_inputStream, ZipConstants.ZipEntrySignature);
                if (bytesSkipped == -1) return null;
                // back up 4 bytes: ReadEntry assumes the file pointer is positioned before the entry signature
                _inputStream.Seek(-4, SeekOrigin.Current);
            }
            // workitem 10923
            else if (_firstEntry)
            {
                // we've already read one entry.
                // Seek to the end of it.
                _inputStream.Seek(_endOfEntry, SeekOrigin.Begin);
            }
            _currentEntry = ZipEntry.ReadEntry(_container, !_firstEntry);
            // ReadEntry leaves the file position after all the entry
            // data and the optional bit-3 data descriptpr.  This is
            // where the next entry would normally start.
            _endOfEntry = _inputStream.Position;
            _firstEntry = true;
            _needSetup = true;
            _findRequired= false;
            return _currentEntry;
        }
        protected override void Dispose(bool disposing)
        {
            if (_closed) return;
            if (disposing) // not called from finalizer
            {
                // When ZipInputStream is used within a using clause, and an
                // exception is thrown, Close() is invoked.  But we don't want to
                // try to write anything in that case.  Eventually the exception
                // will be propagated to the application.
                if (_exceptionPending) return;
                if (!_leaveUnderlyingStreamOpen)
                {
                    _inputStream.Dispose();
                }
            }
            _closed= true;
        }
        public override bool CanRead  { get { return true; }}
        public override bool CanSeek  { get { return _inputStream.CanSeek; } }
        public override bool CanWrite { get { return false; } }
        public override long Length   { get { return _inputStream.Length; }}
        public override long Position
        {
            get { return _inputStream.Position;}
            set { Seek(value, SeekOrigin.Begin); }
        }
    public override void Flush() => throw new NotSupportedException("Flush");
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("Write");
    public override long Seek(long offset, SeekOrigin origin)
        {
            _findRequired= true;
            return _inputStream.Seek(offset, origin);
        }
    public override void SetLength(long value) => throw new NotSupportedException();
    private Stream _inputStream;
        private System.Text.Encoding _provisionalAlternateEncoding;
        private ZipEntry _currentEntry;
        private bool _firstEntry;
        private bool _needSetup;
        private ZipContainer _container;
        private Ionic.Zlib.CrcCalculatorStream _crcStream;
        private Int64 _LeftToRead;
        internal String _Password;
        private Int64 _endOfEntry;
        private string _name;
        private bool _leaveUnderlyingStreamOpen;
        private bool _closed;
        private bool _findRequired;
        private bool _exceptionPending;
    }
