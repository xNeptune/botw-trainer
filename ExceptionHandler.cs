namespace BotwTrainer
{
    public class ExceptionHandler
    {
        private readonly MainWindow mainWindow;

        public ExceptionHandler(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public void HandleException(ETCPGeckoException exc)
        {
            this.HandleExceptionInternally(exc);
        }

        private void HandleExceptionInternally(ETCPGeckoException exc)
        {
            // If the result has no (, then it we failed, so be loud
            ETCPErrorCode error = exc.ErrorCode;
            string msg = string.Empty;
            switch (error)
            {
                case ETCPErrorCode.CheatStreamSizeInvalid:
                    msg = "Cheat stream size is invalid!";
                    break;
                case ETCPErrorCode.FTDICommandSendError:
                    msg = "Error sending a command to the TCP Gecko!";
                    break;
                case ETCPErrorCode.FTDIInvalidReply:
                    msg = "Received an invalid reply from the TCP Gecko!";
                    break;
                case ETCPErrorCode.FTDIPurgeRxError:
                    msg = "Error occured while purging receive data buffer!";
                    break;
                case ETCPErrorCode.FTDIPurgeTxError:
                    msg = "Error occured while purging transfer data buffer!";
                    break;
                case ETCPErrorCode.FTDIQueryError:
                    msg = "Error querying TCP Gecko data!";
                    break;
                case ETCPErrorCode.FTDIReadDataError:
                    msg = "Error reading TCP Gecko data!";
                    break;
                case ETCPErrorCode.FTDIResetError:
                    msg = "Error resetting the TCP Gecko connection!";
                    break;
                case ETCPErrorCode.FTDITimeoutSetError:
                    msg = "Error setting send/receive timeouts!";
                    break;
                case ETCPErrorCode.FTDITransferSetError:
                    msg = "Error setting transfer buffer sizes!";
                    break;
                case ETCPErrorCode.noFTDIDevicesFound:
                    msg = "No FTDI devices found! Please make sure your TCP Gecko is connected!";
                    break;
                case ETCPErrorCode.noTCPGeckoFound:
                    msg = "No TCP Gecko device found! Please make sure your TCP Gecko is connected!";
                    break;
                case ETCPErrorCode.REGStreamSizeInvalid:
                    msg = "Register stream data invalid!";
                    break;
                case ETCPErrorCode.TooManyRetries:
                    msg = "Too many retries while attempting to transfer data!";
                    break;
                default:
                    msg = "An unknown error occured";
                    break;
            }

            this.mainWindow.LogError(exc, msg);
        }
    }
}
