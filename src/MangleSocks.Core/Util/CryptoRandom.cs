using System;
using System.Security.Cryptography;

namespace MangleSocks.Core.Util
{
    // Based on https://gist.github.com/niik/1017834
    public class CryptoRandom : Random
    {
        public static readonly Random Instance = new CryptoRandom();

        readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();

        byte[] _buffer;
        int _bufferPosition;

        void InitBuffer()
        {
            if (this._buffer == null || this._buffer.Length != 4)
            {
                this._buffer = new byte[4];
            }

            this._rng.GetBytes(this._buffer);
            this._bufferPosition = 0;
        }

        /// <summary>
        /// Returns a nonnegative random number.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to zero and less than <see cref="F:System.Int32.MaxValue"/>.
        /// </returns>
        public override int Next()
        {
            // Mask away the sign bit so that we always return nonnegative integers
            return (int)this.GetRandomUInt32() & 0x7FFFFFFF;
        }

        /// <summary>
        /// Returns a nonnegative random number less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue"/> must be greater than or equal to zero.</param>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to zero, and less than <paramref name="maxValue"/>; that is, the range of return values ordinarily includes zero but not <paramref name="maxValue"/>. However, if <paramref name="maxValue"/> equals zero, <paramref name="maxValue"/> is returned.
        /// </returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="maxValue"/> is less than zero.
        /// </exception>
        public override int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException("maxValue");
            }

            return this.Next(0, maxValue);
        }

        /// <summary>
        /// Returns a random number within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>.</param>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>; that is, the range of return values includes <paramref name="minValue"/> but not <paramref name="maxValue"/>. If <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="minValue"/> is returned.
        /// </returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="minValue"/> is greater than <paramref name="maxValue"/>.
        /// </exception>
        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException("minValue");
            }

            if (minValue == maxValue)
            {
                return minValue;
            }

            long diff = maxValue - minValue;

            while (true)
            {
                uint rand = this.GetRandomUInt32();

                long max = 1 + (long)uint.MaxValue;
                long remainder = max % diff;

                if (rand < max - remainder)
                {
                    return (int)(minValue + (rand % diff));
                }
            }
        }

        /// <summary>
        /// Returns a random number between 0.0 and 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        public override double NextDouble()
        {
            return this.GetRandomUInt32() / (1.0 + uint.MaxValue);
        }

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">An array of bytes to contain random numbers.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="buffer"/> is null.
        /// </exception>
        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            lock (this._rng)
            {
                this._rng.GetBytes(buffer);
            }
        }

        /// <summary>
        /// Gets one random unsigned 32bit integer in a thread safe manner.
        /// </summary>
        uint GetRandomUInt32()
        {
            lock (this)
            {
                this.EnsureRandomBuffer(4);

                uint rand = BitConverter.ToUInt32(this._buffer, this._bufferPosition);

                this._bufferPosition += 4;

                return rand;
            }
        }

        /// <summary>
        /// Ensures that we have enough bytes in the random buffer.
        /// </summary>
        /// <param name="requiredBytes">The number of required bytes.</param>
        void EnsureRandomBuffer(int requiredBytes)
        {
            if (this._buffer == null)
            {
                this.InitBuffer();
            }

            if (requiredBytes > this._buffer.Length)
            {
                throw new ArgumentOutOfRangeException("requiredBytes", "cannot be greater than random buffer");
            }

            if ((this._buffer.Length - this._bufferPosition) < requiredBytes)
            {
                this.InitBuffer();
            }
        }
    }
}