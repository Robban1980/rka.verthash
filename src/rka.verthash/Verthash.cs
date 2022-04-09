namespace RKA.Verthash;

public class Verthash
{
    private static readonly int _headerSize = 80;
    private static readonly uint _hashSizeOut = 32;
    private static readonly uint _p0Size = 64;
    private static readonly uint _NIterations = 8; // iterations
    private static readonly uint _nSubset = _p0Size * _NIterations;
    private static readonly uint _nRotations = 32; // Rotations
    private static readonly uint _nIndexes = 4096;
    private static readonly uint _byteAlignment = 16;

    // FNV constants
    private static readonly uint _prime = 16777619;
    private Verthashdat _verthashdat;

    SHA3.Net.Sha3 _sha3_256;
    SHA3.Net.Sha3 _sha3_512;

    public Verthash(Verthashdat verthashdat)
    {
        _verthashdat = verthashdat;
        _sha3_256 = SHA3.Net.Sha3.Sha3256();
        _sha3_512 = SHA3.Net.Sha3.Sha3512();
    }

    /// <summary>
    /// Calculate Hash of input.
    /// </summary>
    /// <param name="input">byte array of block header</param>
    /// <returns>Verthash POW hash.</returns>
    public byte[] PowHash(byte[] input)
    {
        byte[] input_header = new byte[_headerSize];
        Buffer.BlockCopy(input, 0, input_header, 0, _headerSize);
        byte[] p1 = new byte[_hashSizeOut];

        // 1. Generation of inital 32-byte array from 80-byte block header by using sha3-256 hash function
        _sha3_256.Initialize();
        // sha3hash should be 32 bytes
        var tmpSha3 = _sha3_256.ComputeHash(input_header);

        // Copy the specified number of bytes from source to target.
        Buffer.BlockCopy(tmpSha3, 0, p1, 0, tmpSha3.Length);

        byte[] p0 = new byte[_nSubset];

        // 2. Generation of a 512-byte array (128 4-byte pointers) by using 8 iterations of sha3-512, based on block header with incremented first byte (parallel task on GPU).
        for (int i = 0; i < _NIterations; i++)
        {
            input_header[0] += 1;
            _sha3_512.Initialize();
            var digest64 = _sha3_512.ComputeHash(input_header);
            Buffer.BlockCopy(digest64, 0, p0, (int)(i * _p0Size), (int)_p0Size);
        }

        uint[] seekIndexes = new uint[_nIndexes];

        // convert p0 bytes to uint's
        uint[] p0Index = new uint[p0.Length / 4];
        for (int i = 0; i < p0.Length; i += 4)
        {
            p0Index[i / 4] = BitConverter.ToUInt32(p0, i);
        }

        // Step 3: Expansion of pointer array by factor 32 (4096 4-byte pointers) using bit-wise rotation.
        uint size = (_nSubset / 4);
        for (uint x = 0; x < _nRotations; x++)
        {
            for (uint i = 0; i < _nSubset / 4; i++)
            {
                seekIndexes[(x * size) + i] = p0Index[i];
            }

            for (int y = 0; y < p0Index.Length; y++)
            {
                p0Index[y] = (p0Index[y] << 1) | (1 & (p0Index[y] >> 31));
            }
        }

        // Step 4: Initalization of accumulator to avoid deprecated fnv - 0 usage.
        uint valueAccumulator = 0x811c9dc5;

        // Step 5: Logical enumeration of 16 - byte chunks inside 1GB + verthash.dat file.
        uint mdiv = ((((uint)_verthashdat.FileSize) - _hashSizeOut) / _byteAlignment) + 1;

        for (uint i = 0; i < _nIndexes; i++)
        {
            // Get offset to start reading from
            uint offset = (fnv1a(seekIndexes[i], valueAccumulator) % mdiv) * _byteAlignment;

            for (uint i2 = 0; i2 < _hashSizeOut / 4; i2++)
            {
                using (var va = _verthashdat.VerthashFile.CreateViewAccessor(offset + i2 * 4, 4, System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read))
                {
                    // Read UInt32 from verthash file.
                    uint value = va.ReadUInt32(0);
                    
                    byte[] tmp = new byte[4];
                    Buffer.BlockCopy(p1, (int)i2 * 4, tmp, 0, 4);
                    var tmpUint = BitConverter.ToUInt32(tmp);

                    var res = fnv1a(tmpUint, value);
                    tmp = BitConverter.GetBytes(res);
                    Buffer.BlockCopy(tmp, 0, p1, (int)i2 * 4, 4);

                    // Update value accumulator
                    valueAccumulator = fnv1a(valueAccumulator, (uint)value);
                }
            }
        }

        return p1;
    }

    private uint fnv1a(uint a, uint b)
    {
        return (a ^ b) * _prime;
    }

}
