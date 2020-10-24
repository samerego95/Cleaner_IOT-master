using DatabaseManaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    public sealed partial class LogMisure : Page
    {
        //Costruttore Timer timeout
        private Windows.UI.Xaml.DispatcherTimer TimerTimeout =
            new Windows.UI.Xaml.DispatcherTimer();

        //costruttore accesso dati
        private database CleanerDB = new database();

        //variabile locale per bindin
        private ObservableCollection<database.Misura> mis
            = new ObservableCollection<database.Misura>();

        //stringhe
        private string txtHeader = "";
        private string notaIniziale = "";

        //variabile timout
        private int timeout;

        //flag per ricordare se dati cambiati
        //private bool datiModificati;
        private bool caricamento;
        private bool gotFocusOnce = true;

        public LogMisure()
        {
            this.InitializeComponent();
        }

        private void Page_Loading(FrameworkElement sender, object args)
        {
            //abilita ring attesa
            attesa.Visibility = Visibility.Visible;
            attesa.IsActive = true;

            mis = null;

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //inizializza flags
            //datiModificati = false;

            //rinomina colonne dopo binding
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                //carica nome pagina
                txtHeader = resourceLoader.GetString("StoricoMisure");

            }

            //Inizializza timer timeout
            timeout = 0;
            TimerTimeout.Interval = TimeSpan.FromMilliseconds(1000);
            TimerTimeout.Tick += TimerTimeout_Tick;
            TimerTimeout.Start();


        }

        //----------------------------------
        //nasconde le colonne non strettamente necessarie
        private void nascondiColonne()
        {
            for (int l = 3; l <= 11; l++)
            {
                GrigliaDati.Columns[l].Visibility = Visibility.Collapsed;
            }

            rinominaHeader();

            this.Bindings.Update();

        }

        //Mostra colonne non necessariamente necessarie
        private void mostraColonne()
        {
            for (int l = 3; l <= 11; l++)
            {
                GrigliaDati.Columns[l].Visibility = Visibility.Visible;
            }

            rinominaHeader();
        }

        private void rinominaHeader()
        {
            //rinomina colonne dopo binding
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                //carica header colonne
                GrigliaDati.Columns[0].Header = resourceLoader.GetString("NumeroMisuraSigla");
                GrigliaDati.Columns[1].Header = resourceLoader.GetString("Data");
                GrigliaDati.Columns[2].Header = resourceLoader.GetString("Prodotto");
                GrigliaDati.Columns[3].Header = resourceLoader.GetString("TempoMisura");
                GrigliaDati.Columns[4].Header = resourceLoader.GetString("PotenzaVentilatore");
                GrigliaDati.Columns[5].Header = resourceLoader.GetString("Minimo");
                GrigliaDati.Columns[6].Header = resourceLoader.GetString("Massimo");
                GrigliaDati.Columns[7].Header = resourceLoader.GetString("PesoInizialeIngresso");
                GrigliaDati.Columns[8].Header = resourceLoader.GetString("PesoInizialeUscita");
                GrigliaDati.Columns[9].Header = resourceLoader.GetString("PesoIngresso");
                GrigliaDati.Columns[10].Header = resourceLoader.GetString("PesoUscita");
                GrigliaDati.Columns[11].Header = resourceLoader.GetString("PesoPulito");
                GrigliaDati.Columns[12].Header = resourceLoader.GetString("Impurita");
                if (GrigliaDati.Columns.Count() == 14)
                    GrigliaDati.Columns[13].Header = resourceLoader.GetString("Note");

            }

            ////rende editabile solo l'ultima colonna
            //for (int l = 0; l < 13; l++)
            //{
            //    GrigliaDati.Columns[l].IsReadOnly = true;
            //}

            //GrigliaDati.Columns[13].IsReadOnly = false;

        }

        //**********************************
        //**********************************
        //******** TIMER Timeout      ******
        //**********************************
        //**********************************
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

        //------------------------------------
        //box asincroni
        //------------------------------------
        private void cancellaHandler(IUICommand command)
        {
            string si = "Yes";

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                si = resourceLoader.GetString("Si");
            }

            //se ricevuto comando si, cancella il log
            if (command.Label == si)
            {
                CleanerDB.cancellaLogMisura();

                //cancella variabile locale
                mis.Clear();

                //aggiorna layout
                this.Bindings.Update();
            }

        }

        //----------------------------------------
        // Eventi
        //----------------------------------------
        //Tasto cancella log premuto
        private async void Clear_ClickAsync(object sender, RoutedEventArgs e)
        {
            string titolo = " ";
            string messaggio = " ";
            string si = "Yes";
            string no = "Not";

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                titolo = resourceLoader.GetString("CancellaLogMisureHeader");
                messaggio = resourceLoader.GetString("CancellaLogMisureTesto");
                si = resourceLoader.GetString("Si");
                no = resourceLoader.GetString("No");
            }

            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(messaggio, titolo);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            messageDialog.Commands.Add(new UICommand(
                si,
                new UICommandInvokedHandler(this.cancellaHandler)));
            messageDialog.Commands.Add(new UICommand(
                no,
                new UICommandInvokedHandler(this.cancellaHandler)));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 0;

            // Show the message dialog
            await messageDialog.ShowAsync();

            //reset timeout
            timeout = 0;

        }

        private void Esci_Click(object sender, RoutedEventArgs e)
        {
            //stop timer timeout
            TimerTimeout.Stop();

            //torna al form precedente
            this.Frame.GoBack();
        }

        private void ToggleView_Click(object sender, RoutedEventArgs e)
        {
            //se non visibili, rendile visibili
            if (GrigliaDati.Columns[3].Visibility == Visibility.Collapsed)
                mostraColonne();
            //altrimenti nascondile
            else
                nascondiColonne();

        }

        private void Grid_GotFocus(object sender, RoutedEventArgs e)
        {
            if (gotFocusOnce)
            {
                //legge dati impianto
                mis = CleanerDB.getLogMisure();

                this.Bindings.Update();

                //questo flag serve per aggiornare solo una volta le etichette (Headers) della tabella
                caricamento = true;
                gotFocusOnce = false;

            }

        }

        private void GrigliaDati_LoadingRow(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridRowEventArgs e)
        {
            //viene chiamato quando la griglia è aggiornata
            if (caricamento)
            {
                rinominaHeader();
                nascondiColonne();

                //disabilita ring attesa
                attesa.IsActive = false;
                attesa.Visibility = Visibility.Collapsed;

                //questo flag serve per aggiornare solo una volta le etichette (Headers) della tabella
                caricamento = false;
            }
            
        }

        private void GrigliaDati_BeginningEdit(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridBeginningEditEventArgs e)
        {
            //ottiene numero di linea selezionata
            int linea = GrigliaDati.SelectedIndex;

            //salva la nota iniziale per compararla a fine modifica
            notaIniziale = mis.ElementAt(linea).note;

            //resetta timeout
            timeout = 0;
        }

        private void GrigliaDati_CellEditEnded(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridCellEditEndedEventArgs e)
        {
            //ottiene numero di linea selezionata
            int linea = GrigliaDati.SelectedIndex;

            //se la cella è cambiata, aggiorna DB
            if (notaIniziale != mis.ElementAt(linea).note)
            {
                CleanerDB.updateNoteLogMisura(mis.ElementAt(linea).ID, mis.ElementAt(linea).note);
            }

            //resetta timeout
            timeout = 0;
        }
    }

}
