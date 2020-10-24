using System;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Il modello di elemento Pagina vuota è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234238

namespace Cleaner_IOT
{
    /// <summary>
    /// Pagina vuota che può essere usata autonomamente oppure per l'esplorazione all'interno di un frame.
    /// </summary>
    public sealed partial class ImpostazioniGenerali : Page
    {
        //---------------------------------
        //Variabili
        //---------------------------------
        //Costruttore Timer
        private Windows.UI.Xaml.DispatcherTimer TimerTimeout =
            new Windows.UI.Xaml.DispatcherTimer();

        //variabile timout
        private int timeout;

        //valori in binding

        //Stringhe
        private string txtHeader;
        private string headerIntestazione;
        private string testoIntestazione;
        private string copiaIntestazione;
        private string testoOra;
        private string testoData;

        //Flag
        private bool oraDataCambiati = false;

        //Password
        public const string PASSWORD = "9999";

        //---------------------------------
        //Funzioni
        //---------------------------------
        public ImpostazioniGenerali()
        {
            this.InitializeComponent();
        }

        //******** TIMER Timeout      ******
        private void TimerTimeout_Tick(object sender, object e)
        {
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
        //Gestione eventi UI
        //-----------------------------------------------------------
        private void Esci_Click(object sender, RoutedEventArgs e)
        {
            //Salva impostazioni ed eventualmente le inizializza
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            //aggiorna impostazioni indietro
            testoIntestazione = Intestazione.Text;

            //se cambiato, salva scontrino
            if (copiaIntestazione != testoIntestazione)
            {
                MainPage.Database.putIntestazione(testoIntestazione);
            }

            //se cambiata data, la salva
            if (oraDataCambiati)
            {
                DateTime dataOra = new DateTime(datePick.Date.Year,
                                                datePick.Date.Month,
                                                datePick.Date.Day,
                                                timePick.Time.Hours,
                                                timePick.Time.Minutes,
                                                timePick.Time.Seconds);

                MainPage.ImpostaDataOra(dataOra);

            }

            //stop timer timeout
            TimerTimeout.Stop();

            //torna al form principale
            this.Frame.GoBack();

        }

        async private void Help_Click(object sender, RoutedEventArgs e)
        {
            string titolo = " ";
            string messaggio = " ";

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                titolo = resourceLoader.GetString("InformazioniSistema");
                messaggio = resourceLoader.GetString("Versione");
            }

            //resetta timeout
            timeout = 0;

            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(messaggio, titolo);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            messageDialog.Commands.Add(new UICommand("Ok"));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 0;

            // Show the message dialog
            await messageDialog.ShowAsync();

            //azzera contatore timeout
            timeout = 0;
        }

        private void Page_Loading(FrameworkElement sender, object args)
        {
            //ring attesa
            attesa.Visibility = Visibility.Visible;
            attesa.IsActive = true;

            //gestione data ora
            timePick.Time = MainPage.dataOraSistema.TimeOfDay;
            datePick.Date = MainPage.dataOraSistema.Date;
            datePick.MinYear = DateTimeOffset.Parse("2000");

            //reset flag modifica data ora
            oraDataCambiati = false;

            //time e date picker disponibili solo se RTC presente
            if (MainPage.DS3231_RTC.dataDisponibile)
            {
                timePick.IsEnabled = true;
                datePick.IsEnabled = true;
            }
            //altrimenti non disponibili
            else
            {
                timePick.IsEnabled = false;
                datePick.IsEnabled = false;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //carica testi da risorse
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                txtHeader = resourceLoader.GetString("ImpostazioniGenerali");
                Button.Content = resourceLoader.GetString("ImpostazioniAvanzate");

                headerIntestazione = resourceLoader.GetString("Scontrino");
                testoOra = resourceLoader.GetString("Ora");
                testoData = resourceLoader.GetString("Data");

            }

            //legge impostazioni ed eventualmente le inizializza
            //intestazione
            testoIntestazione = MainPage.Database.getIntestazione();
            copiaIntestazione = testoIntestazione;

            //Inizializza timer timeout
            timeout = 0;
            TimerTimeout.Interval = TimeSpan.FromMilliseconds(1000);
            TimerTimeout.Tick += TimerTimeout_Tick;
            TimerTimeout.Start();

            //disabilita bottone impostazioni impianto
            Button.IsEnabled = false;

            this.Bindings.Update();

            attesa.IsActive = false;
            attesa.Visibility = Visibility.Collapsed;


        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //blocca timeout
            TimerTimeout.Stop();

            //mostra pagina impostazioni
            this.Frame.Navigate(typeof(ImpostazioniAvanzate));

            //azzera contatore timeout
            timeout = 0;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            string pass;

            //leggi password
            pass = passwordBox.Password.ToString();

            //se password corretta, abilita tasto impostazioni impianto, altrimenti no
            if (pass == PASSWORD)
                Button.IsEnabled = true;
            else
                Button.IsEnabled = false;

            //resetta timeout
            timeout = 0;

            //azzera contatore timeout
            timeout = 0;
        }

        private void Password_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            //se premuto enter e password corretta, vai ad impostazioni
            if (e.Key == VirtualKey.Enter
                && passwordBox.Password.ToString() == PASSWORD)
            {
                Button_Click(this, null);
            }

            //azzera contatore timeout
            timeout = 0;
        }

        private void timePick_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            oraDataCambiati = true;
        }

        private void datePick_DateChanged(object sender, DatePickerValueChangedEventArgs e)
        {
            oraDataCambiati = true;
        }
    }
}
