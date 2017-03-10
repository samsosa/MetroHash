﻿using System;
using System.Runtime.CompilerServices;

namespace MetroHash
{
    /// <summary>
    /// Metro Hash 128
    /// </summary>
    public class MetroHash128
    {

        private byte[] _result;
        private byte[] _buffer;
        private ulong[] _firstTwoStates;
        private ulong _thirdState;
        private ulong _fourthState;
        private int _bytes;

        /// <summary>
        /// Constructor for incremental version, call Update and Finalize for full Hash
        /// </summary>
        /// <param name="seed">Seed</param>
        public MetroHash128(ulong seed)
        {
            _buffer = new byte[32];
            _result = new byte[16];
            _firstTwoStates = Unsafe.As<byte[], ulong[]>(ref _result);
            _thirdState = 0;
            _fourthState = 0;
            ref var firstState = ref _firstTwoStates[0];
            ref var secondState = ref _firstTwoStates[1];
            firstState = (seed - K0) * K3;
            secondState = (seed + K1) * K2;
            _thirdState = (seed + K0) * K2;
            _fourthState = (seed - K1) * K3;
        }

        private static void ValidateInput(byte[] input, int offset, int count)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (input.Length < offset + count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }
        /// <summary>
        /// Add data to hash
        /// </summary>
        /// <param name="input">data</param>
        /// <param name="offset">offset</param>
        /// <param name="count">count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(byte[] input, int offset, int count)
        {
            ValidateInput(input, offset, count);
            ref var firstState = ref _firstTwoStates[0];
            ref var secondState = ref _firstTwoStates[1];
            var end = offset + count;
            var bMod = _bytes % 32;
            if (bMod != 0)
            {
                int fill = 32 - bMod;
                if (fill > count)
                {
                    fill = count;
                }

                Buffer.BlockCopy(input, offset, _buffer, bMod, fill);
                offset += fill;
                _bytes += fill;

                if (_bytes % 32 != 0)
                {
                    return;
                }
                firstState += ToUlong(_buffer, 0) * K0;
                firstState = RotateRight(firstState, 29) + _thirdState;
                secondState += ToUlong(_buffer, 8) * K1;
                secondState = RotateRight(secondState, 29) + _fourthState;
                _thirdState += ToUlong(_buffer, 16) * K2;
                _thirdState = RotateRight(_thirdState, 29) + firstState;
                _fourthState += ToUlong(_buffer, 24) * K3;
                _fourthState = RotateRight(_fourthState, 29) + secondState;
            }
            _bytes += end - offset;
            BulkLoop(ref firstState, ref secondState, ref _thirdState, ref _fourthState, input, ref offset, end);

            if (offset < end)
            {
                Buffer.BlockCopy(input, offset, _buffer, 0, end - offset);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BulkLoop(ref ulong firstState, ref ulong secondState, ref ulong thirdState, ref ulong fourthState, byte[] buffer, ref int offset, int count)
        {
            while (offset <= count - 32)
            {
                firstState += ToUlong(buffer, offset) * K0;
                offset += 8;
                firstState = RotateRight(firstState, 29) + thirdState;
                secondState += ToUlong(buffer, offset) * K1;
                offset += 8;
                secondState = RotateRight(secondState, 29) + fourthState;
                thirdState += ToUlong(buffer, offset) * K2;
                offset += 8;
                thirdState = RotateRight(thirdState, 29) + firstState;
                fourthState += ToUlong(buffer, offset) * K3;
                offset += 8;
                fourthState = RotateRight(fourthState, 29) + secondState;
            }
        }

        private static void FinalizeHash(ref ulong firstState, ref ulong secondState, ref ulong thirdState, ref ulong fourthState, byte[] buffer, ref int offset, int count)
        {
            // finalize bulk loop, if used
            if (count >= 32)
            {
                thirdState ^= RotateRight((firstState + fourthState) * K0 + secondState, 21) * K1;
                fourthState ^= RotateRight((secondState + thirdState) * K1 + firstState, 21) * K0;
                firstState ^= RotateRight((firstState + thirdState) * K0 + fourthState, 21) * K1;
                secondState ^= RotateRight((secondState + fourthState) * K1 + thirdState, 21) * K0;
            }

            var end = offset + count % 32;

            if (end - offset >= 16)
            {
                firstState += ToUlong(buffer, offset) * K2;
                offset += 8;
                firstState = RotateRight(firstState, 33) * K3;
                secondState += ToUlong(buffer, offset) * K2;
                offset += 8;
                secondState = RotateRight(secondState, 33) * K3;
                firstState ^= RotateRight(firstState * K2 + secondState, 45) * K1;
                secondState ^= RotateRight(secondState * K3 + firstState, 45) * K0;
            }

            if (end - offset >= 8)
            {
                firstState += ToUlong(buffer, offset) * K2;
                offset += 8;
                firstState = RotateRight(firstState, 33) * K3;
                firstState ^= RotateRight(firstState * K2 + secondState, 27) * K1;
            }

            if (end - offset >= 4)
            {
                secondState += ToUint(buffer, offset) * K2;
                offset += 4;
                secondState = RotateRight(secondState, 33) * K3;
                secondState ^= RotateRight(secondState * K3 + firstState, 46) * K0;
            }

            if (end - offset >= 2)
            {
                firstState += ToUshort(buffer, offset) * K2;
                offset += 2;
                firstState = RotateRight(firstState, 33) * K3;
                firstState ^= RotateRight(firstState * K2 + secondState, 22) * K1;
            }

            if (end - offset >= 1)
            {
                secondState += ToByte(buffer, offset) * K2;
                secondState = RotateRight(secondState, 33) * K3;
                secondState ^= RotateRight(secondState * K3 + firstState, 58) * K0;
            }

            firstState += RotateRight(firstState * K0 + secondState, 13);
            secondState += RotateRight(secondState * K1 + firstState, 37);
            firstState += RotateRight(firstState * K2 + secondState, 13);
            secondState += RotateRight(secondState * K3 + firstState, 37);
        }

        /// <summary>
        /// Finalizes the hash and returns the hash
        /// </summary>
        /// <returns>Hash</returns>
        public byte[] Finalize()
        {
            int offset = 0;
            FinalizeHash(ref _firstTwoStates[0], ref _firstTwoStates[1], ref _thirdState, ref _fourthState, _buffer, ref offset, _bytes);
            _bytes = 0;
            return _result;
        }

        /// <summary>
        /// MetroHash 128 hash method
        /// Not cryptographically secure
        /// </summary>
        /// <param name="seed">Seed to initialize data</param>
        /// <param name="input">Data you want to hash</param>
        /// <param name="offset">Start of the data you want to hash</param>
        /// <param name="count">Length of the data you want to hash</param>
        /// <returns></returns>
        public static byte[] Hash(ulong seed, byte[] input, int offset, int count)
        {
            ValidateInput(input, offset, count);
            var result = new byte[16];
            var end = offset + count;
            ulong[] state = Unsafe.As<byte[], ulong[]>(ref result);
            ref var firstState = ref state[0];
            ref var secondState = ref state[1];
            ulong thirdState = 0;
            ulong fourthState = 0;
            firstState = (seed - K0) * K3;
            secondState = (seed + K1) * K2;
            if (count >= 32)
            {
                thirdState = (seed + K0) * K2;
                fourthState = (seed - K1) * K3;
            }
            BulkLoop(ref firstState, ref secondState, ref thirdState, ref fourthState, input, ref offset, end);
            FinalizeHash(ref firstState, ref secondState, ref thirdState, ref fourthState, input, ref offset, count);
            return result;
        }

        private const ulong K0 = 0xC83A91E1;
        private const ulong K1 = 0x8648DBDB;
        private const ulong K2 = 0x7BDEC03B;
        private const ulong K3 = 0x2F5870A5;


        /// <summary>
        ///     BitConverter methods are several times slower
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ToByte(byte[] data, int start)
        {
            return data[start];
        }

        /// <summary>
        ///     BitConverter methods are several times slower
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort ToUshort(byte[] data, int start)
        {
            return Unsafe.As<byte, ushort>(ref data[start]);
        }

        /// <summary>
        ///     BitConverter methods are several times slower
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ToUint(byte[] data, int start)
        {
            return Unsafe.As<byte, uint>(ref data[start]);
        }

        /// <summary>
        ///     BitConverter methods are several times slower
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ToUlong(byte[] data, int start)
        {
            return Unsafe.As<byte, ulong>(ref data[start]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RotateRight(ulong x, int r)
        {
            return (x >> r) | (x << (64 - r));
        }
    }
}