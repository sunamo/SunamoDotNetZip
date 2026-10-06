namespace Ionic.Zip.Deflate64;

    // This class can be used to read bits from an byte array quickly.
    // Normally we get bits from 'bitBuffer' field and bitsInBuffer stores
    // the number of bits available in 'BitBuffer'.
    // When we used up the bits in bitBuffer, we will try to get byte from
    // the byte array and copy the byte to appropiate position in bitBuffer.
    //
    // The byte array is not reused. We will go from 'start' to 'end'.
    // When we reach the end, most read operations will return -1,
    // which means we are running out of input.
    internal sealed class InputBuffer
    {
        private byte[] _buffer;           // byte array to store input
        private int _start;               // start poisition of the buffer
        private int _end;                 // end position of the buffer
        private uint _bitBuffer = 0;      // store the bits here, we can quickly shift in this buffer
        private int _bitsInBuffer = 0;    // number of bits available in bitBuffer
        public int AvailableBits => _bitsInBuffer;
        public int AvailableBytes => (_end - _start) + (_bitsInBuffer / 8);
        public bool EnsureBitsAvailable(int count)
        {
            Debug.Assert(0 < count && count <= 16, "count is invalid.");
            // manual inlining to improve perf
            if (_bitsInBuffer < count)
            {
                if (NeedsInput())
                {
                    return false;
                }
                Debug.Assert(_buffer != null);
                // insert a byte to bitbuffer
                _bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
                _bitsInBuffer += 8;
                if (_bitsInBuffer < count)
                {
                    if (NeedsInput())
                    {
                        return false;
                    }
                    // insert a byte to bitbuffer
                    _bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
                    _bitsInBuffer += 8;
                }
            }
            return true;
        }
        public uint TryLoad16Bits()
        {
            Debug.Assert(_buffer != null);
            if (_bitsInBuffer < 8)
            {
                if (_start < _end)
                {
                    _bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
                    _bitsInBuffer += 8;
                }
                if (_start < _end)
                {
                    _bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
                    _bitsInBuffer += 8;
                }
            }
            else if (_bitsInBuffer < 16)
            {
                if (_start < _end)
                {
                    _bitBuffer |= (uint)_buffer[_start++] << _bitsInBuffer;
                    _bitsInBuffer += 8;
                }
            }
            return _bitBuffer;
        }
        private uint GetBitMask(int count) => ((uint)1 << count) - 1;
        public int GetBits(int count)
        {
            Debug.Assert(0 < count && count <= 16, "count is invalid.");
            if (!EnsureBitsAvailable(count))
            {
                return -1;
            }
            int result = (int)(_bitBuffer & GetBitMask(count));
            _bitBuffer >>= count;
            _bitsInBuffer -= count;
            return result;
        }
        // Copies length bytes from input buffer to output buffer starting at output[offset].
        // You have to make sure, that the buffer is byte aligned. If not enough bytes are
        // available, copies fewer bytes.
        // Returns the number of bytes copied, 0 if no byte is available.
        public int CopyTo(byte[] output, int offset, int length)
        {
            Debug.Assert(output != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(length >= 0);
            Debug.Assert(offset <= output.Length - length);
            Debug.Assert((_bitsInBuffer % 8) == 0);
            // Copy the bytes in bitBuffer first.
            int bytesFromBitBuffer = 0;
            while (_bitsInBuffer > 0 && length > 0)
            {
                output[offset++] = (byte)_bitBuffer;
                _bitBuffer >>= 8;
                _bitsInBuffer -= 8;
                length--;
                bytesFromBitBuffer++;
            }
            if (length == 0)
            {
                return bytesFromBitBuffer;
            }
            int avail = _end - _start;
            if (length > avail)
            {
                length = avail;
            }
            Debug.Assert(_buffer != null);
            Array.Copy(_buffer, _start, output, offset, length);
            _start += length;
            return bytesFromBitBuffer + length;
        }
        // Return true is all input bytes are used.
        // This means the caller can call SetInput to add more input.
        public bool NeedsInput() => _start == _end;
        // Set the byte array to be processed.
        // All the bits remained in bitBuffer will be processed before the new bytes.
        // We don't clone the byte array here since it is expensive.
        // The caller should make sure after a buffer is passed in.
        // It will not be changed before calling this function again.
        public void SetInput(byte[] buffer, int offset, int length)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(length >= 0);
            Debug.Assert(offset <= buffer.Length - length);
            if (_start == _end)
            {
                _buffer = buffer;
                _start = offset;
                _end = offset + length;
            }
        }
        // Skip n bits in the buffer.
        public void SkipBits(int bitCount)
        {
            Debug.Assert(_bitsInBuffer >= bitCount, "No enough bits in the buffer, Did you call EnsureBitsAvailable?");
            _bitBuffer >>= bitCount;
            _bitsInBuffer -= bitCount;
        }
        // Skips to the next byte boundary.
        public void SkipToByteBoundary()
        {
            _bitBuffer >>= (_bitsInBuffer % 8);
            _bitsInBuffer = _bitsInBuffer - (_bitsInBuffer % 8);
        }
    }
