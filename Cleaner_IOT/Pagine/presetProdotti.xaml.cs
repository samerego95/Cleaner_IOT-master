//using System;
//using System.Collections.Generic;
//using System.IO;
using DatabaseManaging;
using Microsoft.Toolkit.Uwp.UI.Controls;
//using Windows.UI.Xaml.Controls.Primitives;
//using Windows.UI.Xaml.Data;
//using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
//using Windows.UI.Xaml.Navigation;
//using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
//using System.Runtime.InteropServices.WindowsRuntime;
//using Windows.Foundation;
//using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using System;
//using Sentinel_IOT.Controlli;
//using Windows.UI.ViewManagement;
//using System.Diagnostics;
//using Microsoft.Toolkit.Uwp.UI.Controls;

// Il modello di elemento Pagina vuota è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234238

namespace Cleaner_IOT
{
    /// <summary>
    /// Pagina vuota che può essere usata autonomamente oppure per l'esplorazione all'interno di un frame.
    /// </summary>
    public sealed partial class presetProdotti : Page
    {
        //Costruttore Timer timeout
        private Windows.UI.Xaml.DispatcherTimer TimerTimeout =
            new Windows.UI.Xaml.DispatcherTimer();

        //stringhe
        private string txtHeader = "";

        //variabile timout
        private int timeout;

        //prodotto temporaneamente selezionato
        private int prodottoTemporaneamenteSelezionato;

        //flag per ricordare se dati cambiati
        private bool datiModificati;
        private bool caricamento;
        private bool gotFocusOnce = true;

        //costruttore accesso dati
        private database CleanerDB = new database();

        //variabile locale per bindin
        private ObservableCollection<database.ImpostazioniProdotto> imp
            = new ObservableCollection<database.ImpostazioniProdotto>();

        public presetProdotti()
        {
            this.InitializeComponent();

        }

        private void ImpostazioniImpianto_loaded(object sender, RoutedEventArgs e)
        {
            //inizializza flags
            datiModificati = false;

            //variabile messa per sistemare nomi header una sola volta
            caricamento = true;

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                //carica nome pagina
                txtHeader = resourceLoader.GetString("SceltaProdotti");

            }

            //Inizializza timer timeout
            timeout = 0;
            TimerTimeout.Interval = TimeSpan.FromMilliseconds(1000);
            TimerTimeout.Tick += TimerTimeout_Tick;
            TimerTimeout.Start();

            //aggiorna
            this.Bindings.Update();

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


        private void Add_Click(object sender, RoutedEventArgs e)
        {
            database.ImpostazioniProdotto nuovaImpostazione =
                new database.ImpostazioniProdotto();

            //crea nuova impostazione vuota
            if (imp.Count == 0)
                nuovaImpostazione.ID = 1;
            else
                nuovaImpostazione.ID = imp.Count + 1;

            nuovaImpostazione.prodotto = "Prodotto";
            nuovaImpostazione.tempoMisura = 60;
            nuovaImpostazione.potenzaVentilatore = 50;
            nuovaImpostazione.minimo = 300;
            nuovaImpostazione.massimo = 800;

            //aggiunge nuova impostazione appena creata
            imp.Add(nuovaImpostazione);

            //aggiorna flag modifica prodotti
            datiModificati = true;

            //resetta timeout
            timeout = 0;

        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            int linea;

            //ottiene numero di linea selezionata
            linea = GrigliaDati.SelectedIndex;

            //cancella la linea nella collezione dati
            imp.RemoveAt(linea);

            //aggiorna flag modifica prodotti
            datiModificati = true;

            //resetta timeout
            timeout = 0;

        }

        private void Esci_Click(object sender, RoutedEventArgs e)
        {
            //stop timer timeout
            TimerTimeout.Stop();

            //------- Questa parte va tolta se si reintroduce "Salva"
            //se ci sono state modifiche, salva le nuove impostazioni nel DB
            if (datiModificati)
            {
                //salva nuovo impianto
                CleanerDB.putImpostazioniProdotti(imp);

            }
            //----------

            //torna al form precedente
            this.Frame.GoBack();
        }

        //set prodotto corrente
        private void Accetta_Click(object sender, RoutedEventArgs e)
        {
            //salva il valore selezionato 
            MainPage.ImpostaProdottoCorrente(prodottoTemporaneamenteSelezionato);

            //infine esce
            Esci_Click(sender, e);
        }

        private void Cella_Modificata(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridCellEditEndedEventArgs e)
        {
            //aggiorna flag modifica prodotti
            datiModificati = true;

            //resetta timeout
            timeout = 0;

        }

        private void Cella_Selezionata(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridBeginningEditEventArgs e)
        {
            //imposta valore temporaneo selezione
            if (prodottoTemporaneamenteSelezionato >= 0
                && prodottoTemporaneamenteSelezionato <= imp.Count())
            {
                prodottoTemporaneamenteSelezionato = e.Row.GetIndex();
            }

            //resetta timeout
            timeout = 0;

        }

        private void Loading(FrameworkElement sender, object args)
        {
            //abilita ring attesa
            attesa.Visibility = Visibility.Visible;
            attesa.IsActive = true;

            //inizializza flags
            datiModificati = false;

        }

        private void GrigliaDati_LoadRow(object sender, DataGridRowEventArgs e)
        {
            //pennello colore verde
            SolidColorBrush greenBrush = new SolidColorBrush(Windows.UI.Colors.Green);

            //pittura di verde lo sfondo del prodotto selezionato
            if (e.Row.GetIndex() == MainPage.ProdottoCorrente)
            {
                e.Row.Background = greenBrush;
            }

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null
                && caricamento)
            {
                //carica header colonne
                GrigliaDati.Columns[0].Visibility = Visibility.Collapsed;
                GrigliaDati.Columns[1].Header = resourceLoader.GetString("Prodotto");
                GrigliaDati.Columns[2].Header = resourceLoader.GetString("TempoMisura");
                GrigliaDati.Columns[3].Header = resourceLoader.GetString("PotenzaVentilatore");
                GrigliaDati.Columns[4].Header = resourceLoader.GetString("Minimo");
                GrigliaDati.Columns[5].Header = resourceLoader.GetString("Massimo");

                //disabilita ring attesa
                attesa.IsActive = false;
                attesa.Visibility = Visibility.Collapsed;

                //variabile messa per sistemare nomi header una sola volta
                caricamento = false;
            }
        }

        private void GrigliaDatiCambiataSelezione(object sender, System.EventArgs e)
        {
            //imposta valore temporaneo selezione
            prodottoTemporaneamenteSelezionato = GrigliaDati.SelectedIndex;

            //resetta timeout
            timeout = 0;

        }

        private void Grid_GotFocus(object sender, RoutedEventArgs e)
        {
            if (gotFocusOnce)
            {
                //legge dati impianto
                imp = CleanerDB.getImpostazioniProdotti();

                //questo flag serve per aggiornare solo una volta le etichette (Headers) della tabella
                caricamento = true;

                this.Bindings.Update();

                gotFocusOnce = false;
            }
        }
    }
}
