namespace BotwTrainer
{
    using System;
    using System.IO;
    using System.Net.Sockets;

    class tcpconn
    {
        TcpClient client;
        NetworkStream stream;

        public string Host { get; private set; }
        public int Port { get; private set; }

        public tcpconn(string host, int port)
        {
            this.Host = host;
            this.Port = port;
            this.client = null;
            this.stream = null;
        }

        public void Connect()
        {
            try
            {
                this.Close();
            }
            catch (Exception) { }
            this.client = new TcpClient();
            this.client.NoDelay = true;
            IAsyncResult ar = this.client.BeginConnect(this.Host, this.Port, null, null);
            System.Threading.WaitHandle wh = ar.AsyncWaitHandle;
            try
            {
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                {
                    this.client.Close();
                    throw new IOException("Connection timoeut.", new TimeoutException());
                }

                this.client.EndConnect(ar);
            }
            finally
            {
                wh.Close();
            } 
            this.stream = this.client.GetStream();
            this.stream.ReadTimeout = 10000;
            this.stream.WriteTimeout = 10000;
        }

        public void Close()
        {
            try
            {
                if (this.client == null)
                {
                    throw new IOException("Not connected.", new NullReferenceException());
                }
                this.client.Close();

            }
            catch (Exception) { }
            finally
            {
                this.client = null;
            }
        }

        public void Purge()
        {
            if (this.stream == null)
            {
                throw new IOException("Not connected.", new NullReferenceException());
            }
            this.stream.Flush();
        }

        public void Read(Byte[] buffer, UInt32 nobytes, ref UInt32 bytes_read)
        {
            try
            {
                int offset = 0;
                if (this.stream == null)
                {
                    throw new IOException("Not connected.", new NullReferenceException());
                }
                bytes_read = 0;
                while (nobytes > 0)
                {
                    int read = this.stream.Read(buffer, offset, (int)nobytes);
                    if (read >= 0)
                    {
                        bytes_read += (uint)read;
                        offset += read;
                        nobytes -= (uint)read;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (ObjectDisposedException e)
            {
                throw new IOException("Connection closed.", e);
            }
        }

        public void Write(Byte[] buffer, Int32 nobytes, ref UInt32 bytes_written)
        {
            try
            {
                if (this.stream == null)
                {
                    throw new IOException("Not connected.", new NullReferenceException());
                }
                this.stream.Write(buffer, 0, nobytes);
                if (nobytes >= 0)
                    bytes_written = (uint)nobytes;
                else
                    bytes_written = 0;
                this.stream.Flush();
            }
            catch (ObjectDisposedException e)
            {
                throw new IOException("Connection closed.", e);
            }
        }
    }
}
