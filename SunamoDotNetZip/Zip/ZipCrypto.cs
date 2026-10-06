namespace Ionic.Zip;

// ZipCrypto.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2008, 2009, 2011 Dino Chiesa
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
// Time-stamp: <2011-July-28 06:30:59>
//
// ------------------------------------------------------------------
//
// This module provides the implementation for "traditional" Zip encryption.
//
// Created Tue Apr 15 17:39:56 2008
//
// ------------------------------------------------------------------
    internal class ZipCrypto
    {
        private ZipCrypto() { }
        public static ZipCrypto ForWrite(string password)
        {
            ZipCrypto crypto = new();
            if (password == null)
                throw new BadPasswordException("This entry requires a password.");
            crypto.InitCipher(password);
            return crypto;
        }
        public static ZipCrypto ForRead(string password, ZipEntry entry)
        {
            System.IO.Stream stream = entry._archiveStream;
            entry._WeakEncryptionHeader = new byte[12];
            byte[] encryptionHeader = entry._WeakEncryptionHeader;
            ZipCrypto crypto = new();
            if (password == null)
                throw new BadPasswordException("This entry requires a password.");
            crypto.InitCipher(password);
            ZipEntry.ReadWeakEncryptionHeader(stream, encryptionHeader);
            // Decrypt the header.  This has a side effect of "further initializing the
            // encryption keys" in the traditional zip encryption.
            byte[] DecryptedHeader = crypto.DecryptMessage(encryptionHeader, encryptionHeader.Length);
            // CRC check
            // According to the pkzip spec, the final byte in the decrypted header
            // is the highest-order byte in the CRC. We check it here.
            if (DecryptedHeader[11] != (byte)((entry._Crc32 >> 24) & 0xff))
            {
                // In the case that bit 3 of the general purpose bit flag is set to
                // indicate the presence of an 'Extended File Header' or a 'data
                // descriptor' (signature 0x08074b50), the last byte of the decrypted
                // header is sometimes compared with the high-order byte of the
                // lastmodified time, rather than the high-order byte of the CRC, to
                // verify the password.
                //
                // This is not documented in the PKWare Appnote.txt.  It was
                // discovered this by analysis of the Crypt.c source file in the
                // InfoZip library http://www.info-zip.org/pub/infozip/
                //
                // The reason for this is that the CRC for a file cannot be known
                // until the entire contents of the file have been streamed. This
                // means a tool would have to read the file content TWICE in its
                // entirety in order to perform PKZIP encryption - once to compute
                // the CRC, and again to actually encrypt.
                //
                // This is so important for performance that using the timeblob as
                // the verification should be the standard practice for DotNetZip
                // when using PKZIP encryption. This implies that bit 3 must be
                // set. The downside is that some tools still cannot cope with ZIP
                // files that use bit 3.  Therefore, DotNetZip DOES NOT force bit 3
                // when PKZIP encryption is in use, and instead, reads the stream
                // twice.
                //
                if ((entry._BitField & 0x0008) != 0x0008)
                {
                    throw new BadPasswordException("The password did not match.");
                }
                else if (DecryptedHeader[11] != (byte)((entry._TimeBlob >> 8) & 0xff))
                {
                    throw new BadPasswordException("The password did not match.");
                }
                // We have a good password.
            }
            else
            {
                // A-OK
            }
            return crypto;
        }
        private byte MagicByte
        {
            get
            {
                UInt16 t = (UInt16)((UInt16)(_Keys[2] & 0xFFFF) | 2);
                return (byte)((t * (t ^ 1)) >> 8);
            }
        }
        // Decrypting:
        // From AppNote.txt:
        // loop for i from 0 to 11
        //     C := buffer(i) ^ decrypt_byte()
        //     update_keys(C)
        //     buffer(i) := C
        // end loop
        public byte[] DecryptMessage(byte[] cipherText, int length)
        {
        if (cipherText == null) throw new ArgumentNullException(nameof(cipherText));
        if (length > cipherText.Length)
                throw new ArgumentOutOfRangeException(nameof(length),
                                                      "Bad length during Decryption: the length parameter must be smaller than or equal to the size of the destination array.");
            byte[] plainText = new byte[length];
            for (int i = 0; i < length; i++)
            {
                byte C = (byte)(cipherText[i] ^ MagicByte);
                UpdateKeys(C);
                plainText[i] = C;
            }
            return plainText;
        }
        public byte[] EncryptMessage(byte[] plainText, int length)
        {
        if (plainText == null) throw new ArgumentNullException(nameof(plainText));
        if (length > plainText.Length)
                throw new ArgumentOutOfRangeException(nameof(length),
                                                      "Bad length during Encryption: The length parameter must be smaller than or equal to the size of the destination array.");
            byte[] cipherText = new byte[length];
            for (int i = 0; i < length; i++)
            {
                byte C = plainText[i];
                cipherText[i] = (byte)(plainText[i] ^ MagicByte);
                UpdateKeys(C);
            }
            return cipherText;
        }
        public void InitCipher(string passphrase)
        {
            byte[] p = SharedUtilities.StringToByteArray(passphrase);
            for (int i = 0; i < passphrase.Length; i++)
                UpdateKeys(p[i]);
        }
        private void UpdateKeys(byte byteValue)
        {
            _Keys[0] = (UInt32)crc32.ComputeCrc32((int)_Keys[0], byteValue);
            _Keys[1] = _Keys[1] + (byte)_Keys[0];
            _Keys[1] = _Keys[1] * 0x08088405 + 1;
            _Keys[2] = (UInt32)crc32.ComputeCrc32((int)_Keys[2], (byte)(_Keys[1] >> 24));
        }
        //public byte[] KeyHeader
        //{
        //    get
        //    {
        //        byte[] result = new byte[12];
        //        result[0] = (byte)(_Keys[0] & 0xff);
        //        result[1] = (byte)((_Keys[0] >> 8) & 0xff);
        //        result[2] = (byte)((_Keys[0] >> 16) & 0xff);
        //        result[3] = (byte)((_Keys[0] >> 24) & 0xff);
        //        result[4] = (byte)(_Keys[1] & 0xff);
        //        result[5] = (byte)((_Keys[1] >> 8) & 0xff);
        //        result[6] = (byte)((_Keys[1] >> 16) & 0xff);
        //        result[7] = (byte)((_Keys[1] >> 24) & 0xff);
        //        result[8] = (byte)(_Keys[2] & 0xff);
        //        result[9] = (byte)((_Keys[2] >> 8) & 0xff);
        //        result[10] = (byte)((_Keys[2] >> 16) & 0xff);
        //        result[11] = (byte)((_Keys[2] >> 24) & 0xff);
        //        return result;
        //    }
        //}
        // private fields for the crypto stuff:
        private readonly UInt32[] _Keys = [0x12345678, 0x23456789, 0x34567890];
        private readonly Ionic.Zlib.CRC32 crc32 = new();
    }
    internal enum CryptoMode
    {
        Encrypt,
        Decrypt
    }
    internal class ZipCipherStream : System.IO.Stream
    {
        private readonly ZipCrypto _cipher;
        private readonly System.IO.Stream _s;
        private readonly CryptoMode _mode;
        public ZipCipherStream(System.IO.Stream stream, ZipCrypto cipher, CryptoMode mode)
        {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        _cipher = cipher;
            _s = stream;
            _mode = mode;
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_mode == CryptoMode.Encrypt)
                throw new NotSupportedException("This stream does not encrypt via Read()");
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        byte[] db = new byte[count];
            int bytesRead = _s.Read(db, 0, count);
            byte[] decrypted = _cipher.DecryptMessage(db, bytesRead);
            for (int i = 0; i < bytesRead; i++)
            {
                buffer[offset + i] = decrypted[i];
            }
            return bytesRead;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_mode == CryptoMode.Decrypt)
                throw new NotSupportedException("This stream does not Decrypt via Write()");
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        // workitem 7696
        if (count == 0) return;
        byte[] plaintext;
        if (offset != 0)
            {
                plaintext = new byte[count];
                for (int i = 0; i < count; i++)
                {
                    plaintext[i] = buffer[offset + i];
                }
            }
            else plaintext = buffer;
            byte[] encrypted = _cipher.EncryptMessage(plaintext, count);
            _s.Write(encrypted, 0, encrypted.Length);
        }
        public override bool CanRead
        {
            get { return (_mode == CryptoMode.Decrypt); }
        }
        public override bool CanSeek
        {
            get { return false; }
        }
        public override bool CanWrite
        {
            get { return (_mode == CryptoMode.Encrypt); }
        }
        public override void Flush()
        {
            //throw new NotSupportedException();
        }
        public override long Length
        {
            get { throw new NotSupportedException(); }
        }
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
    public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}