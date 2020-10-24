using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Il modello di elemento Pagina vuota è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234238

namespace Cleaner_IOT
{
    /// <summary>
    /// Pagina vuota che può essere usata autonomamente oppure per l'esplorazione all'interno di un frame.
    /// </summary>
    public sealed partial class ImpostazioniAvanzate : Page
    {
        //Costruttore Timer timeout
        private Windows.UI.Xaml.DispatcherTimer TimerTimeout =
            new Windows.UI.Xaml.DispatcherTimer();

        //variabile timout
        private int timeout;

        //Stringhe
        private string txtHeader;
        private string headerCalibrazioneIngresso;
        private string headerCalibrazioneUscita;

        //Variabili generiche
        int pesaInCalibrazione = (int)App.INDIRIZZO_PESA.NESSUNA;

        public ImpostazioniAvanzate()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //recupera testi lingue
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                txtHeader = resourceLoader.GetString("ImpostazioniAvanzate");
                headerCalibrazioneIngresso = resourceLoader.GetString("CalibrazionePesoIngresso");
                headerCalibrazioneUscita = resourceLoader.GetString("CalibrazionePesoUscita");

            }

            //Inizializza timer timeout
            timeout = 0;
            TimerTimeout.Interval = TimeSpan.FromMilliseconds(1000);
            TimerTimeout.Tick += TimerTimeout_Tick;
            TimerTimeout.Start();

            //imposta pesi iniziali calibrazione
            pesoRealeIngresso.Text = MainPage.pesoIngresso.ToString();
            pesoRealeUscita.Text = MainPage.pesoUscita.ToString();

            //riabilita i bottoni
            ButtonCalIn.IsEnabled = true;
            ButtonCalOut.IsEnabled = true;

            //disabilita ring attesa
            attesa.IsActive = false;
            attesa.Visibility = Visibility.Collapsed;

        }

        private void Page_Loading(FrameworkElement sender, object args)
        {
            //abilita ring attesa
            attesa.Visibility = Visibility.Visible;
            attesa.IsActive = true;


        }

        //******** TIMER Timeout      ******
        private void TimerTimeout_Tick(object sender, object e)
        {
            //lettura buffer di ricezione
            string buffer = App.ComPort.bufferRicezione;

            //verifica se c'è un comando valido nel buffer
            if (buffer.Contains("\r"))
            {
                //verifica se calibrazione andata a buon fine
                if (buffer.Contains(App.CMD_GCL_DO_CALIBRAZIONE_OK))
                {
                    //mostra popup calibrazione andata a buon fine
                    mostraPopupCalibrazione(true);

                    //reset comando pesa attesa
                    pesaInCalibrazione = (int)App.INDIRIZZO_PESA.NESSUNA;
                }

                //verifica se calibrazione fallita
                if (buffer.Contains(App.CMD_GCL_DO_CALIBRAZIONE_ERR))
                {
                    //mostra popup calibrazione sbagliata
                    mostraPopupCalibrazione(false);

                    //reset comando pesa attesa
                    pesaInCalibrazione = (int)App.INDIRIZZO_PESA.NESSUNA;
                }

                //clear buffer ricezione ed uscita
                App.ComPort.ClearBufferRicezione();

            }

            //se sta aspettando risposta calibrazione e passati 3 secondi, da errore
            if (pesaInCalibrazione != (int)App.INDIRIZZO_PESA.NESSUNA
                && timeout > 3)
            {
                //mostra popup calibrazione sbagliata
                mostraPopupCalibrazione(false);

                //reset comando pesa attesa
                pesaInCalibrazione = (int)App.INDIRIZZO_PESA.NESSUNA;
            }

            //se scaduto timeout, esce senza salvare alcun valore
            if (++timeout > 60)
            {
                //stop timer timeout
                TimerTimeout.Stop();

                //torna al form principale
                this.Frame.GoBack();

            }

        }

        //-----------------------------------------------------------
        //Gestione eventi
        //-----------------------------------------------------------
        private void Esci_Click(object sender, RoutedEventArgs e)
        {
            ////Salva impostazioni ed eventualmente le inizializza
            //ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            //localSettings.Values["NomeImpianto"] =
            //    NomeImpianto.Text;

            //stop timer timeout
            TimerTimeout.Stop();

            //torna al form principale
            this.Frame.GoBack();

        }

        async private void Button_Cal_In(object sender, RoutedEventArgs e)
        {
            string titolo = " ";
            string messaggio = " ";
            string si = "Yes";
            string no = "Not";

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                titolo = resourceLoader.GetString("CalibrazionePesoIngresso");
                messaggio = resourceLoader.GetString("SicuroCalibraIngresso")
                    + " " + pesoRealeIngresso.Text + "g";
                si = resourceLoader.GetString("Si");
                no = resourceLoader.GetString("No");
            }

            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(messaggio, titolo);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            messageDialog.Commands.Add(new UICommand(
                si,
                new UICommandInvokedHandler(this.calPesoIngressoHandler)));
            messageDialog.Commands.Add(new UICommand(
                no,
                new UICommandInvokedHandler(this.calPesoIngressoHandler)));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 0;

            //disabilita bottoni
            ButtonCalIn.IsEnabled = false;
            ButtonCalOut.IsEnabled = false;

            // Show the message dialog
            await messageDialog.ShowAsync();

            //reset timeout
            timeout = 0;

        }

        async private void Button_Cal_Out(object sender, RoutedEventArgs e)
        {
            string titolo = " ";
            string messaggio = " ";
            string si = "Yes";
            string no = "Not";

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                titolo = resourceLoader.GetString("CalibrazionePesoUscita");
                messaggio = resourceLoader.GetString("SicuroCalibraUscita")
                    + " " + pesoRealeUscita.Text + "g";
                si = resourceLoader.GetString("Si");
                no = resourceLoader.GetString("No");
            }

            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(messaggio, titolo);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            messageDialog.Commands.Add(new UICommand(
                si,
                new UICommandInvokedHandler(this.calPesoUscitaHandler)));
            messageDialog.Commands.Add(new UICommand(
                no,
                new UICommandInvokedHandler(this.calPesoUscitaHandler)));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 0;

            //disabilita bottoni
            ButtonCalIn.IsEnabled = false;
            ButtonCalOut.IsEnabled = false;

            // Show the message dialog
            await messageDialog.ShowAsync();

            //reset timeout
            timeout = 0;

        }

        async private void mostraPopupCalibrazione(bool isAndataAbuonFine)
        {
            string titolo = " ";
            string messaggio = " ";
            string ok = "Cancel";

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                //a seconda della pesa che si stava calibrando, carica messaggio giusto
                if (pesaInCalibrazione == (int)App.INDIRIZZO_PESA.PESO_INGRESSO)
                    titolo = resourceLoader.GetString("CalibrazionePesoIngresso");
                else
                    titolo = resourceLoader.GetString("CalibrazionePesoUscita");

                //andata a buon fine o meno
                if (isAndataAbuonFine)
                    messaggio = resourceLoader.GetString("CalibrazioneOk");
                else
                    messaggio = resourceLoader.GetString("CalibrazioneFallita");

                ok = resourceLoader.GetString("Cancel");

            }

            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(messaggio, titolo);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            messageDialog.Commands.Add(new UICommand(ok, null));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 0;

            //disabilita bottone
            ButtonCalOut.IsTapEnabled = false;

            // Show the message dialog
            await messageDialog.ShowAsync();

            //riabilita i bottoni
            ButtonCalIn.IsEnabled = true;
            ButtonCalOut.IsEnabled = true;

        }

        //------------------------------------
        //box asincroni
        //------------------------------------
        private void calPesoIngressoHandler(IUICommand command)
        {
            string si = "Yes";

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                si = resourceLoader.GetString("Si");
            }

            //se ricevuto comando si, spegni applicazione
            if (command.Label == si)
            {
                //calibra qui pesa
                //invia comando su porta seriale
                App.ComPort.SendString("ATCAL=" + pesoRealeIngresso.Text + ";1\r");

                //seleziona pesa in calibrazione (serve per alert errore/successo)
                pesaInCalibrazione = (int)App.INDIRIZZO_PESA.PESO_INGRESSO;

            }

        }

        private void calPesoUscitaHandler(IUICommand command)
        {
            string si = "Yes";

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                si = resourceLoader.GetString("Si");
            }

            //se ricevuto comando si, spegni applicazione
            if (command.Label == si)
            {
                //calibra qui pesa
                //invia comando su porta seriale
                App.ComPort.SendString("ATCAL=" + pesoRealeUscita.Text + ";2\r");

                //seleziona pesa in calibrazione (serve per alert errore/successo)
                pesaInCalibrazione = (int)App.INDIRIZZO_PESA.PESO_USCITA;

            }

        }

        private void reloadTimeout(object sender, TextChangedEventArgs e)
        {
            //reset timeout
            timeout = 0;

        }

    }
}
