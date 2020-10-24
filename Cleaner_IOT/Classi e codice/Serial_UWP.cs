using System;
using System.Linq;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace SerialPortNameSpace
{
    public class Serial_UWP
    {
        private SerialDevice UartPort;
        private DataReader DataReaderObject = null;
        private DataWriter DataWriterObject;
        private CancellationTokenSource ReadCancellationTokenSource;

        //flag che dice se la porta è stata aperta
        public Boolean aperta;

        //buffer di ricezione
        public volatile string bufferRicezione;


        //********************************
        //********************************
        //********** INITIALISE **********
        //********************************
        //********************************
        public async Task Initialise(string[] portNames, uint BaudRate)     //NOTE - THIS IS AN ASYNC METHOD!
        {
            if (aperta == true)
                return;

            //aperta = false;
            ClearBufferRicezione();

            try
            {

                //cerca porte disponibili nel systema
                string aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);

                foreach (string porta in portNames)
                {

                    //compara con predefinite
                    foreach (DeviceInformation element in dis)
                    {
                        //se trova una delle porte COM predefinite, la inizializza
                        if (element.Name.Contains(porta)
                            || element.Id.Contains(porta))
                        {
                            //configura porta e relative impostazioni
                            UartPort = await SerialDevice.FromIdAsync(element.Id);

                            if (UartPort == null)
                            {
                                string s = SerialDevice.GetDeviceSelectorFromUsbVidPid(0x0403, 0x6015);
                                UartPort = await SerialDevice.FromIdAsync(s);
                            }

                            UartPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);    //mS before a time-out occurs when a write operation does not finish (default=InfiniteTimeout).
                            UartPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);     //mS before a time-out occurs when a read operation does not finish (default=InfiniteTimeout).
                            UartPort.BaudRate = BaudRate;
                            UartPort.Parity = SerialParity.None;
                            UartPort.StopBits = SerialStopBitCount.One;
                            UartPort.DataBits = 8;

                            //configura lettore in background
                            DataReaderObject = new DataReader(UartPort.InputStream);
                            DataReaderObject.InputStreamOptions = InputStreamOptions.Partial;
                            DataWriterObject = new DataWriter(UartPort.OutputStream);

                            //avvia ricezione
                            StartReceive();

                            //flag porta aperta
                            aperta = true;

                            return;

                        }
                    }
                }

                //se la porta non è stata trovata, apre UART fisica Raspberry
                //configura porta e relative impostazioni
                UartPort = await SerialDevice.FromIdAsync(dis.First().Id);
                UartPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);    //mS before a time-out occurs when a write operation does not finish (default=InfiniteTimeout).
                UartPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);     //mS before a time-out occurs when a read operation does not finish (default=InfiniteTimeout).
                UartPort.BaudRate = BaudRate;
                UartPort.Parity = SerialParity.None;
                UartPort.StopBits = SerialStopBitCount.One;
                UartPort.DataBits = 8;

                //configura lettore in background
                DataReaderObject = new DataReader(UartPort.InputStream);
                DataReaderObject.InputStreamOptions = InputStreamOptions.Partial;
                DataWriterObject = new DataWriter(UartPort.OutputStream);

                //avvia ricezione
                StartReceive();

                //flag porta aperta
                aperta = true;

            }
            catch (Exception ex)
            {
                throw new Exception("Uart Initialise Error", ex);
            }
        }

        //***********************************
        //***********************************
        //********** RECEIVE BYTES **********
        //***********************************
        //***********************************
        //This is all a bit complex....but necessary if you want receive to happen asynchrously and for your app to be notified instead of your code having to poll it (this code is basically polling it to create a receive event)

        //ASYNC METHOD TO CREATE THE LISTEN LOOP
        public async void StartReceive()
        {

            ReadCancellationTokenSource = new CancellationTokenSource();

            while (true)
            {
                await Listen();
                if ((ReadCancellationTokenSource.Token.IsCancellationRequested) || (UartPort == null))
                    break;
            }
        }

        //LISTEN FOR NEXT RECEIVE
        private async Task Listen()
        {
            const int NUMBER_OF_BYTES_TO_RECEIVE = 1;           //<<<<<SET THE NUMBER OF BYTES YOU WANT TO WAIT FOR

            //Task<UInt32> loadAsyncTask;
            byte[] ReceiveData;
            UInt32 bytesRead;

            try
            {
                if (UartPort != null)
                {
                    while (true)
                    {
                        //###### WINDOWS IoT MEMORY LEAK BUG 2017-03 - USING CancellationToken WITH LoadAsync() CAUSES A BAD MEMORY LEAK.  WORKAROUND IS
                        //TO BUILD RELEASE WITHOUT USING THE .NET NATIVE TOOLCHAIN OR TO NOT USE A CancellationToken IN THE CALL #####
                        //bytesRead = await DataReaderObject.LoadAsync(NUMBER_OF_BYTES_TO_RECEIVE).AsTask(ReadCancellationTokenSource.Token);	//Wait until buffer is full
                        bytesRead = await DataReaderObject.LoadAsync(NUMBER_OF_BYTES_TO_RECEIVE).AsTask();  //Wait until buffer is full

                        if ((ReadCancellationTokenSource.Token.IsCancellationRequested) || (UartPort == null))
                            break;

                        if (bytesRead > 0)
                        {
                            char dataString;
                            ReceiveData = new byte[NUMBER_OF_BYTES_TO_RECEIVE];
                            DataReaderObject.ReadBytes(ReceiveData);

                            foreach (byte Data in ReceiveData)
                            {
                                //-------------------------------
                                //-------------------------------
                                //----- RECEIVED NEXT BYTE ------
                                //-------------------------------
                                //-------------------------------

                                dataString = System.Convert.ToChar(Data);

                                //lo mette nel buffer
                                bufferRicezione = bufferRicezione + dataString.ToString();


                            } //foreach (byte Data in ReceiveData)

                        }

                    }
                }
            }
            catch (Exception)
            {
                //We will get here often if the USB serial cable is removed so reset ready for a new connection (otherwise a never ending error occurs)
                if (ReadCancellationTokenSource != null)
                    ReadCancellationTokenSource.Cancel();

                //System.Diagnostics.Debug.WriteLine("UART ReadAsync Exception: {0}", e.Message);
            }
        }


        //********************************
        //********************************
        //********** SEND BYTES **********
        //********************************
        //********************************
        public async void SendBytes(byte[] TxData)
        {
            try
            {
                //Send data to UART
                DataWriterObject.WriteBytes(TxData);
                await DataWriterObject.StoreAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Uart Tx Error", ex);
            }
        }

        //********************************
        //********************************
        //********** SEND STRING *********
        //********************************
        //********************************
        public async void SendString(String TxString)
        {
            try
            {
                //Send data to UART
                DataWriterObject.WriteString(TxString);
                await DataWriterObject.StoreAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Uart Tx Error", ex);
            }
        }

        //Clear buffer di ricezione
        public void ClearBufferRicezione()
        {
            bufferRicezione = "";
        }

    }
}
