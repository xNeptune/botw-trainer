namespace BotwTrainer
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Security;
    using System.Threading;
    using System.Windows;

    public class TcpConn
    {
        private TcpClient client;

        private NetworkStream stream;

        public TcpConn(string host, int port)
        {
            this.Host = host;
            this.Port = port;
            this.client = null;
            this.stream = null;
        }

        private string Host { get; set; }

        private int Port { get; set; }

        public bool Connect()
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            this.client = new TcpClient { NoDelay = true };
            var waitHandle = new object() as WaitHandle;

            try
            {
                var asyncResult = this.client.BeginConnect(this.Host, this.Port, null, null);
                waitHandle = asyncResult.AsyncWaitHandle;
                if (!asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                {
                    this.client.Close();
                    return false;
                }

                this.client.EndConnect(asyncResult);
            }
            catch (ArgumentNullException argumentNullException)
            {
                MessageBox.Show(argumentNullException.Message);
            }
            catch (SocketException socketException)
            {
                MessageBox.Show(socketException.Message);
            }
            catch (ObjectDisposedException objectDisposedException)
            {
                MessageBox.Show(objectDisposedException.Message);
            }
            catch (SecurityException securityException)
            {
                MessageBox.Show(securityException.Message);
            }
            catch (ArgumentOutOfRangeException argumentOutOfRangeException)
            {
                MessageBox.Show(argumentOutOfRangeException.Message);
            }
            catch (InvalidOperationException invalidOperationException)
            {
                MessageBox.Show(invalidOperationException.Message);
            }
            catch (AbandonedMutexException abandonedMutexException)
            {
                MessageBox.Show(abandonedMutexException.Message);
            }
            catch (ArgumentException argumentException)
            {
                MessageBox.Show(argumentException.Message);
            }
            catch (OverflowException overflowException)
            {
                MessageBox.Show(overflowException.Message);
            }
            finally
            {
                if (waitHandle != null)
                {
                    waitHandle.Close();
                }
            }

            try
            {
                this.stream = this.client.GetStream();
                this.stream.ReadTimeout = 10000;
                this.stream.WriteTimeout = 10000;
            }
            catch (InvalidOperationException invalidOperationException)
            {
                MessageBox.Show(invalidOperationException.Message);
            }
            catch (ArgumentOutOfRangeException argumentOutOfRangeException)
            {
                MessageBox.Show(argumentOutOfRangeException.Message);
            }

            return true;
        }

        public void Close()
        {
            try
            {
                if (this.client != null)
                {
                    this.client.Close();
                    this.client.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Tcp Close");
            }
            finally
            {
                this.client = null;
            }
        }

        /// <exception cref="IOException">Connection closed.</exception>
        public void Read(byte[] buffer, uint nobytes, ref uint bytesRead)
        {
            try
            {
                var offset = 0;
                if (this.stream == null)
                {
                    throw new IOException("Not connected.", new NullReferenceException());
                }

                bytesRead = 0;
                while (nobytes > 0)
                {
                    var read = this.stream.Read(buffer, offset, (int)nobytes);
                    if (read >= 0)
                    {
                        bytesRead += (uint)read;
                        offset += read;
                        nobytes -= (uint)read;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (ArgumentOutOfRangeException argumentOutOfRangeException)
            {
                throw new IOException("Connection closed.", argumentOutOfRangeException);
            }
            catch (ArgumentNullException argumentNullException)
            {
                throw new IOException("Connection closed.", argumentNullException);
            }
            catch (IOException ioException)
            {
                throw new IOException("Connection closed.", ioException);
            }
            catch (ObjectDisposedException e)
            {
                throw new IOException("Connection closed.", e);
            }
        }

        /// <exception cref="IOException">Not connected.</exception>
        public void Write(byte[] buffer, int nobytes, ref uint bytesWritten)
        {
            try
            {
                if (this.stream == null)
                {
                    throw new IOException("Not connected.", new NullReferenceException());
                }

                this.stream.Write(buffer, 0, nobytes);
                if (nobytes >= 0)
                {
                    bytesWritten = (uint)nobytes;
                }
                else
                {
                    bytesWritten = 0;
                }

                this.stream.Flush();
            }
            catch (ArgumentNullException argumentNullException)
            {
                throw new IOException("Connection closed.", argumentNullException);
            }
            catch (ArgumentOutOfRangeException argumentOutOfRangeException)
            {
                throw new IOException("Connection closed.", argumentOutOfRangeException);
            }
            catch (ObjectDisposedException e)
            {
                throw new IOException("Connection closed.", e);
            }
        }
    }
}
