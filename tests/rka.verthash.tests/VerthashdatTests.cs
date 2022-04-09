using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RKA.Verthash.Tests
{
    public class VerthashdatTests
    {
        [Fact]
        public void FileNotFound()
        {
            // Setup

            // Act
            Action action;
            using (Verthashdat vhdat = Verthashdat.GetInstance(@"c:\fakepath"))
            {
                action = () => vhdat.LoadInRam();
            }

            // Assert
            var exception = Assert.Throws<FileNotFoundException>(action);
            Assert.Equal("Verthash datafile not found.", exception.Message);
        }
    }
}
