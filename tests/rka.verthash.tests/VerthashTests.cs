using System;
using Xunit;

namespace RKA.Verthash.Tests
{
    public class VerthashTests
    {
        public VerthashTests()
        {

        }

        [Fact]
        public void VerifyPow()
        {
            // Setup
            // VTC block height #1745675 534f974f901a0e7d3aadb81640e49cbfd5f685daf173cec62fd0292f56b1fcc4
            string headerHex = "000000204208d166958445f554ce270223a10c5ef6bf14aa645fbf60b6ca449946a43672ef74dcadc8b346c11dd8dd14c88aa2b3c14431e17024dc296e41f91cc993fb87f0d24762f1d0031c8f348e00";

            // Act
            string result = string.Empty;
            using (Verthashdat vhdat = Verthashdat.GetInstance("C:\\Projects\\Verthash"))
            {
                vhdat.LoadInRam();
                Verthash vh = new Verthash(vhdat);

                var powHash = vh.PowHash(headerHex.ToBytes());
                result = powHash.ToHex();
            }

            // Assert

            Assert.Equal("750b4bbe353b049f42a0c37d58ef92175826b4d98580b3943d1b3e0100000000", result);
        }
    }


}