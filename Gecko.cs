namespace BotwTrainer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public enum Command
    {
        COMMAND_WRITE_8 = 0x01,

        COMMAND_WRITE_16 = 0x02,

        COMMAND_WRITE_32 = 0x03,

        COMMAND_READ_MEMORY = 0x04,

        COMMAND_READ_MEMORY_KERNEL = 0x05,

        COMMAND_VALIDATE_ADDRESS_RANGE = 0x06,

        COMMAND_MEMORY_DISASSEMBLE = 0x08,

        COMMAND_READ_MEMORY_COMPRESSED = 0x09,

        COMMAND_KERNEL_WRITE = 0x0B,

        COMMAND_KERNEL_READ = 0x0C,

        COMMAND_TAKE_SCREEN_SHOT = 0x0D,

        COMMAND_UPLOAD_MEMORY = 0x41,

        COMMAND_SERVER_STATUS = 0x50,

        COMMAND_GET_DATA_BUFFER_SIZE = 0x51,

        COMMAND_READ_FILE = 0x52,

        COMMAND_READ_DIRECTORY = 0x53,

        COMMAND_REPLACE_FILE = 0x54,

        COMMAND_GET_CODE_HANDLER_ADDRESS = 0x55,

        COMMAND_READ_THREADS = 0x56,

        COMMAND_ACCOUNT_IDENTIFIER = 0x57,

        COMMAND_WRITE_SCREEN = 0x58,

        COMMAND_FOLLOW_POINTER = 0x60,

        COMMAND_RPC = 0x70,

        COMMAND_GET_SYMBOL = 0x71,

        COMMAND_MEMORY_SEARCH = 0x73,

        COMMAND_SERVER_VERSION = 0x99,

        COMMAND_OS_VERSION = 0x9A,

        COMMAND_RUN_KERNEL_COPY_SERVICE = 0xCD
    }


    public class Gecko
    {
        private const uint MaximumMemoryChunkSize = 0x400;

        private readonly TcpConn tcpConn;

        public Gecko(TcpConn tcpConn)
        {
            this.tcpConn = tcpConn;
        }

        private void SendCommand(Command command)
        {
            uint bytesWritten = 0;
            this.tcpConn.Write(new[] { (byte)command }, 1, ref bytesWritten);
        }

        public int GetOsVersion()
        {
            this.SendCommand(Command.COMMAND_OS_VERSION);

            uint bytesRead = 0;
            var response = new byte[4];
            this.tcpConn.Read(response, 4, ref bytesRead);

            var os = ByteSwap.Swap(BitConverter.ToUInt32(response, 0));

            return (int)os;
        }

        public string GetServerVersion()
        {
            this.SendCommand(Command.COMMAND_SERVER_VERSION);

            uint bytesRead = 0;
            var response = new byte[4];
            this.tcpConn.Read(response, 4, ref bytesRead);

            var length = ByteSwap.Swap(BitConverter.ToUInt32(response, 0));

            response = new byte[length];
            this.tcpConn.Read(response, length, ref bytesRead);

            var server = Encoding.Default.GetString(response);

            return server;
        }

        public int GetServerStatus()
        {
            this.SendCommand(Command.COMMAND_SERVER_STATUS);

            uint bytesRead = 0;
            var response = new byte[1];
            this.tcpConn.Read(response, 1, ref bytesRead);

            var status = response[0];

            return (int)status;
        }

        public int GetInt(uint address)
        {
            var bytes = this.ReadBytes(address, 0x4);

            Array.Reverse(bytes);

            return !bytes.Any() ? 0 : BitConverter.ToInt32(bytes, 0);
        }

        public uint GetUInt(uint address)
        {
            var bytes = this.ReadBytes(address, 0x4);

            Array.Reverse(bytes);

            return !bytes.Any() ? 0 : BitConverter.ToUInt32(bytes, 0);
        }

        public string GetString(uint address)
        {
            var bytes = this.ReadBytes(address, 0x4);

            return !bytes.Any() ? string.Empty : BitConverter.ToString(bytes).Replace("-", string.Empty);
        }

        public byte[] ReadBytes(uint address, uint length)
        {
            try
            {
                this.RequestBytes(address, length);

                uint bytesRead = 0;
                var response = new byte[1];
                this.tcpConn.Read(response, 1, ref bytesRead);

                var ms = new MemoryStream();

                // all zeros
                if (response[0] == 0xB0)
                {
                    return ms.ToArray();
                }

                uint remainingBytesCount = length;
                while (remainingBytesCount > 0)
                {
                    uint chunkSize = remainingBytesCount;

                    // Don't read more bytes than the remote buffer can hold
                    if (chunkSize > MaximumMemoryChunkSize)
                    {
                        chunkSize = MaximumMemoryChunkSize;
                    }

                    var buffer = new byte[chunkSize];
                    bytesRead = 0;
                    this.tcpConn.Read(buffer, chunkSize, ref bytesRead);

                    ms.Write(buffer, 0, (int)chunkSize);

                    remainingBytesCount -= chunkSize;
                }

                return ms.ToArray();
            }
            catch (Exception)
            {
                throw new IOException();
            }
        }

        private void RequestBytes(uint address, uint length)
        {
            try
            {
                this.SendCommand(Command.COMMAND_READ_MEMORY);

                uint bytesRead = 0;
                var bytes = BitConverter.GetBytes(ByteSwap.Swap(address));
                var bytes2 = BitConverter.GetBytes(ByteSwap.Swap(address + length));
                this.tcpConn.Write(bytes, 4, ref bytesRead);
                this.tcpConn.Write(bytes2, 4, ref bytesRead);
            }
            catch (Exception)
            {
                throw new IOException();
            }
        }

        public void WriteInt(uint address, int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            this.WriteBytes(address, bytes);
        }

        public void WriteUInt(uint address, uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            this.WriteBytes(address, bytes);
        }

        public void WriteBytes(uint address, byte[] bytes)
        {
            var partitionedBytes = Partition(bytes, MaximumMemoryChunkSize);
            this.WritePartitionedBytes(address, partitionedBytes);
        }

        private void WritePartitionedBytes(uint address, IEnumerable<byte[]> byteChunks)
        {
            var length = (uint)byteChunks.Sum(chunk => chunk.Length);
            
            try
            {
                this.SendCommand(Command.COMMAND_UPLOAD_MEMORY);

                uint bytesRead = 0;
                var start = BitConverter.GetBytes(ByteSwap.Swap(address));
                var end = BitConverter.GetBytes(ByteSwap.Swap(address + length));

                this.tcpConn.Write(start, 4, ref bytesRead);
                this.tcpConn.Write(end, 4, ref bytesRead);

                foreach (var chunk in byteChunks)
                {
                    address = this.UploadBytes(address, chunk);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        
        private uint UploadBytes(uint address, byte[] bytes)
        {
            var length = bytes.Length;

            uint endAddress = address + (uint)bytes.Length;
            uint bytesRead = 0;
            this.tcpConn.Write(bytes, length, ref bytesRead);
            
            return endAddress;
        }
        
        private static IEnumerable<byte[]> Partition(byte[] bytes, uint chunkSize)
        {
            var byteArrayChunks = new List<byte[]>();
            uint startingIndex = 0;
            
            while (startingIndex < bytes.Length)
            {
                var end = Math.Min(bytes.Length, startingIndex + chunkSize);
                byteArrayChunks.Add(CopyOfRange(bytes, startingIndex, end));
                startingIndex += chunkSize;
            }
            
            return byteArrayChunks;
        }

        private static byte[] CopyOfRange(byte[] src, long start, long end)
        {
            var len = end - start;
            var dest = new byte[len];
            Array.Copy(src, start, dest, 0, len);
            return dest;
        }

        public static string ByteToHexBitFiddle(byte[] bytes)
        {
            var c = new char[bytes.Length * 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = bytes[i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }

            return new string(c);
        }

    }
}