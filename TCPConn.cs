using System;
using System.IO;
using System.Net.Sockets;
using System.Security;
using System.Threading;
using System.Windows;

namespace BotwTrainer
{
    public class TcpConn
    {
        private TcpClient _client;

        private NetworkStream _stream;

        public TcpConn(string host, int port)
        {
            Host = host;
            Port = port;
            _client = null;
            _stream = null;
        }

        private string Host { get; }

        private int Port { get; }

        public bool Connect()
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            _client = new TcpClient {NoDelay = true};
            WaitHandle waitHandle = new object() as WaitHandle;

            try
            {
                var asyncResult = _client.BeginConnect(Host, Port, null, null);
                waitHandle = asyncResult.AsyncWaitHandle;
                if (!asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), false))
                {
                    _client.Close();
                    return false;
                }

                _client.EndConnect(asyncResult);
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
                _stream = _client.GetStream();
                _stream.ReadTimeout = 10000;
                _stream.WriteTimeout = 10000;
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
                if (_client != null)
                {
                    _client.Close();
                    _client.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Tcp Close");
            }
            finally
            {
                _client = null;
            }
        }

        /// <exception cref="IOException">Connection closed.</exception>
        public void Read(byte[] buffer, uint nobytes, ref uint bytesRead)
        {
            try
            {
                var offset = 0;
                if (_stream == null)
                    throw new IOException("Not connected.", new NullReferenceException());

                bytesRead = 0;
                while (nobytes > 0)
                {
                    var read = _stream.Read(buffer, offset, (int) nobytes);
                    if (read >= 0)
                    {
                        bytesRead += (uint) read;
                        offset += read;
                        nobytes -= (uint) read;
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
                if (_stream == null)
                    throw new IOException("Not connected.", new NullReferenceException());
                _stream.Write(buffer, 0, nobytes);
                if (nobytes >= 0)
                    bytesWritten = (uint) nobytes;
                else
                    bytesWritten = 0;

                _stream.Flush();
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
