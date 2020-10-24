using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Cleaner_IOT
{
    /// <summary>
    /// Fornisci un comportamento specifico dell'applicazione in supplemento alla classe Application predefinita.
    /// </summary>
    sealed partial class App : Application
    {
        //variabili globali
        public static string DB_PATH =
            Path.Combine(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "Cleaner.db"));    //DataBase Name   
                                                                                                                   //costruttore porta seriale
        public static SerialPortNameSpace.Serial_UWP ComPort = new SerialPortNameSpace.Serial_UWP();

        public enum COMANDO
        {
            NESSUNA,
            PESO_INGRESSO,
            PESO_USCITA,
            ENTRAMBI_I_PESI,
            TARA_INGRESSO,
            TARA_USCITA,
            AVVIA_MISURA,
            INTERROMPI_MISURA,
            AGGIORNA_VENTILATORE
        }

        public enum INDIRIZZO_PESA
        {
            NESSUNA,
            PESO_INGRESSO,
            PESO_USCITA,
        }

        public const string CMD_GCL_REPLY_PESO = "GW";                                        //risposta peso
        public const string CMD_GCL_REPLY_TARA = "TARE";                                    //risposta tara
        public const string CMD_GCL_DO_TARA_INGRESSO = "ATTARE=1\r";                        //comando esegui tara offset
        public const string CMD_GCL_DO_TARA_USCITA = "ATTARE=2\r";                          //comando esegui tara offset
        public const string CMD_GCL_DO_CALIBRAZIONE_OK = "ATCAL>OK";                         //comando risposta calibrazione pesa andato a buon fine
        public const string CMD_GCL_DO_CALIBRAZIONE_ERR = "ATCAL>ERROR";                    //comando risposta calibrazione pesa fallito
        public const string CMD_GCL_DO_MISURA = "ATSTARTM=1;";                              //comando avvio misura
        public const string CMD_GCL_REPLY_MISURA = "ATSTARTM";                              //risposta a comando misura
        public const string CMD_GCL_UPDATE_FAN = "ATFAN=";                                  //comando aggiorna velocità ventilatore
        public const string CMD_STOP = "ATSTOP\r";                                          //comando stop
        public const string CMD__REPLY_STOP = "ATSTOP";                                     //risposta comando stop
        public const string CMD_GCL_REPLY_OK = "OK";                                        //risposta affermativa
        /// <summary>
        /// Inizializza l'oggetto Application singleton. Si tratta della prima riga del codice creato
        /// creato e, come tale, corrisponde all'equivalente logico di main() o WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            verificaDatabase();
        }

        async private void verificaDatabase()
        {
            //Se database non esiste nella cartella dati, lo copia dal pacchetto installazione
            Windows.Storage.StorageFolder destinationFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile DBfileSource;
            StorageFile DBfileDestination;

            //verifica se esiste il DB nella cartella data APP, altrimenti lo copia
            try
            {
                DBfileDestination = await destinationFolder.GetFileAsync("Cleaner.db");
            }
            catch
            {
                //ottieni percorso Assest folder
                StorageFolder appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFolder assetsFolder = await appInstalledFolder.GetFolderAsync("Assets");

                //copia database
                DBfileSource = await assetsFolder.GetFileAsync("Cleaner.db");
                DBfileDestination = await DBfileSource.CopyAsync(destinationFolder, "Cleaner.db", NameCollisionOption.FailIfExists);
            }

        }

        /// <summary>
        /// Richiamato quando l'applicazione viene avviata normalmente dall'utente finale. All'avvio dell'applicazione
        /// verranno usati altri punti di ingresso per aprire un file specifico.
        /// </summary>
        /// <param name="e">Dettagli sulla richiesta e sul processo di avvio.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Non ripetere l'inizializzazione dell'applicazione se la finestra già dispone di contenuto,
            // assicurarsi solo che la finestra sia attiva
            if (rootFrame == null)
            {
                // Creare un frame che agisca da contesto di navigazione e passare alla prima pagina
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: caricare lo stato dall'applicazione sospesa in precedenza
                }

                // Posizionare il frame nella finestra corrente
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // Quando lo stack di esplorazione non viene ripristinato, passare alla prima pagina
                    // configurando la nuova pagina per passare le informazioni richieste come parametro di
                    // navigazione
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Assicurarsi che la finestra corrente sia attiva
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Chiamato quando la navigazione a una determinata pagina ha esito negativo
        /// </summary>
        /// <param name="sender">Frame la cui navigazione non è riuscita</param>
        /// <param name="e">Dettagli sull'errore di navigazione.</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Richiamato quando l'esecuzione dell'applicazione viene sospesa. Lo stato dell'applicazione viene salvato
        /// senza che sia noto se l'applicazione verrà terminata o ripresa con il contenuto
        /// della memoria ancora integro.
        /// </summary>
        /// <param name="sender">Origine della richiesta di sospensione.</param>
        /// <param name="e">Dettagli relativi alla richiesta di sospensione.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: salvare lo stato dell'applicazione e arrestare eventuali attività eseguite in background
            deferral.Complete();
        }
    }
}
