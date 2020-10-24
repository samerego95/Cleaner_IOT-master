using Cleaner_IOT;
using DS3221_IOT;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DatabaseManaging;
using Windows.System.Threading;
using System.Globalization;

// Il modello di elemento Pagina vuota è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x410

namespace Cleaner_IOT
{
    /// <summary>
    /// Pagina vuota che può essere usata autonomamente oppure per l'esplorazione all'interno di un frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //---------------------------------
        //Variabili
        //---------------------------------
        //Costruttore Timer
        private Windows.UI.Xaml.DispatcherTimer TimerInterrogazioneDispositivi =
            new Windows.UI.Xaml.DispatcherTimer();

        private int systemTimer = 0;

        //Costruttore RTC 3221
        public static DS3231 DS3231_RTC = new DS3231();
        public static DateTime dataOraSistema;

        //stringhe
        private string txtOraEdata = "";
        private string txtNomeProdotto = "";
        private string txtPesoIngresso = "";
        private string txtPesoUscita = "";
        private string txtImpurita = "";
        private string txtTara = "";
        private string txtBottonePulitura = "";
        private string txtCountdown = "";
        private string valPesoIngresso = "0,0 g";
        private string valPesoUscita = "0,0 g";
        private string valImpurita = "---";
        private string txtNumeroMisura = "Nr.";
        private string headerVentilazione;

        //colori
#if ARM
        private SolidColorBrush pennelloNero = new SolidColorBrush(Windows.UI.Colors.White);

#else   //ARM
        private SolidColorBrush pennelloNero = new SolidColorBrush(Windows.UI.Colors.Black);

#endif  //ARM
        private SolidColorBrush pennelloRosso = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush pennelloVerde = new SolidColorBrush(Windows.UI.Colors.Green);
        private Brush colorePesoIngresso;
        private Brush colorePesoUscita;

        //valori
        private static int countdownTimer;
        private static int tempoInizialeMisura;
        public static float pesoIngresso = 0;
        public static float pesoUscita = 0;
        private static int numeroProgressivoMisure = 0;
        private static int prodottoCorrente;
        public static int ProdottoCorrente { get { return prodottoCorrente; } }

        //interrogazione GPM
        private int rispostaAttesa = 0;
        private int prossimoComando = 0;

        //timer interrogazione RTC
        private int prossimaInterrogazioneRTC;

        //costruttore accesso dati
        public static database Database = new database();

        //Impostazioni prodotti
        public static database.ImpostazioniProdotto impostazioniProdotto
            = new database.ImpostazioniProdotto();

        private ObservableCollection<database.ImpostazioniProdotto> ImpostazioniProdotti
            = new ObservableCollection<database.ImpostazioniProdotto>();

        //cella log misura
        public static database.Misura Misura = new database.Misura();

        //stato macchina
        private int statoMacchina = 0;

        public enum STATO_MACCHINA
        {
            S_STANDBY,
            S_AVVIO_MISURA,
            S_MISURAZIONE,
            S_TERMINA_MISURA
        }

        //Id errore per box messaggi
        private enum ERRORI
        {
            ERRORE_COMUNICAZIONE,
            PRODOTTO_INSUFFICIENTE,
            PRODOTTO_ABBONDANTE,
            CASSETTO_USCITA_PIENO,
            MANCA_CASSETTO_USCITA
        }

        //---------------------------------
        //Funzioni
        //---------------------------------

        public MainPage()
        {
            this.InitializeComponent();
        }

        //Timer di sistema
        private void TimerInterrogazioneDispositivi_Tick(object sender, object e)
        {
            bool aggiorna = false;
            
            //incrementa timer di sistema
            systemTimer++;

            //Qui ogni xxxms
            //se seriale aperta, ed indice interrogazioverica se deve inviare messaggio
            if (App.ComPort.aperta)
            {
                try
                {
                    //---------------------------------
                    //Gestione lettura peso
                    //---------------------------------
                    string buffer = App.ComPort.bufferRicezione;

                    //verifica se c'è un comando valido nel buffer
                    if (buffer.Contains("\r"))
                    {
                        aggiorna = processaBuffer(buffer);

                        //clear buffer ricezione ed uscita
                        App.ComPort.ClearBufferRicezione();

                        //reset flag attesa risposta
                        rispostaAttesa = (int)App.COMANDO.NESSUNA;

                        //disabilita ring attesa
                        attesa.IsActive = false;
                        attesa.Visibility = Visibility.Collapsed;


                    }

                    //se nessuna risposta attesa, interroga
                    if (rispostaAttesa == (int)App.COMANDO.NESSUNA)
                    {
                        //controlla che non ci siano comandi in coda da eseguire
                        if (prossimoComando > 0)
                        {
                            processaProssimoComando();
                        }
                        //altrimenti gestisci alternanza lettura peso
                        else
                        {
                            //invia comando su porta seriale
                            App.ComPort.SendString("ATGW=3\r");

                            //flag attesa risposta
                            rispostaAttesa = (int)App.COMANDO.ENTRAMBI_I_PESI;

                        }
                    }
                    //altrimenti cancella comunque risposta attesa
                    else
                    {
                        rispostaAttesa = 0;
                        prossimoComando = 0;
                    }

                }
                catch (Exception ex)
                {
                    //throw new Exception("Data Error", ex);
                    return;
                }
            }

            //--------------------
            //aggiorna ora e data
            //--------------------
            //aggiorna ogni 5 secondi
            if (systemTimer > prossimaInterrogazioneRTC)
            {
                //se presente RTC 3221, usa principalmente quello
                if (DS3231_RTC.dataDisponibile)
                {
                    //legge data ora
                    dataOraSistema = DS3231_RTC.ReadTime();

                }
                //altrimenti prova ad usare tempo di internet
                else
                {
                    dataOraSistema = DateTime.Now;

                }

                //converte in formato Europeo
                string readDateTime = dataOraSistema.ToString("HH:mm dd/mm/yyyy");
                if (txtOraEdata != readDateTime)
                {
                    txtOraEdata = readDateTime;
                    aggiorna = true;
                }

                //prossima interrogazione entro 5 secondi
                prossimaInterrogazioneRTC = systemTimer + 5;

            }

            //se in misura, riattiva sempre bottone arresto
            switch (statoMacchina)
            {
                //stand by
                case (int)STATO_MACCHINA.S_STANDBY:
                    txtCountdown = impostazioniProdotto.tempoMisura.ToString();
                    break;

                //controllo in fase di avvio: verifica risposta macchina
                case (int)STATO_MACCHINA.S_AVVIO_MISURA:
                    //aggiorna countdown
                    if (countdownTimer > 0)
                        countdownTimer--;

                    txtCountdown = countdownTimer.ToString();

                    //se la macchina non risponde entro 2 secondi -> errore
                    if ((tempoInizialeMisura - countdownTimer) > 2)
                    {
                        //interrompi la misura e lancia messaggio errore
                        interrompiMisura();

#warning "avviare messaggio errore risposta qui"
                    }
                    break;

                //controllo fase di misurazione
                case (int)STATO_MACCHINA.S_MISURAZIONE:
                    //aggiorna countdown
                    countdownTimer--;
                    txtCountdown = countdownTimer.ToString();

                    //se tempo scaduto, termina misura
                    if (countdownTimer == 0)
                    {
                        terminaMisura();
                    }
                    //altrimenti continua
                    else
                    {
                        //bottone play diventa bottone stop, ed attivo
                        PlayIcon.Symbol = Symbol.Stop;
                        Play.IsEnabled = true;
                        AddTime.IsEnabled = true;
                        RemoveTime.IsEnabled = true;

                        //gestione barra di stato
                        barraProgressione.Visibility = Visibility.Visible;

                        //calcola tempo trascorso
                        int tempoTrascorso = impostazioniProdotto.tempoMisura - countdownTimer;
                        int valoreBarraStato = 
                            (tempoTrascorso * 100) / impostazioniProdotto.tempoMisura;

                        if (valoreBarraStato > 100)
                            valoreBarraStato = 100;

                        barraProgressione.Value = valoreBarraStato;

                        //testo bottone
                            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                        if (resourceLoader != null)
                        {
                            txtBottonePulitura = resourceLoader.GetString("ArrestaPulitura");
                        }

                        //controlla se velocità ventilatore è stata variata
                        if (SliderVentilazione.Value != impostazioniProdotto.potenzaVentilatore)
                        {
                            //se la velocità è stata variata la comunica al dispositivo slave
                            impostazioniProdotto.potenzaVentilatore = (int)SliderVentilazione.Value;

                            //carica prossimo comando UART: aggiorna ventilatore
                            prossimoComando = (int)App.COMANDO.AGGIORNA_VENTILATORE;

                        }
                    }

                    break;

                //terminazione della misura
                case (int)STATO_MACCHINA.S_TERMINA_MISURA:
                    //ripristina UI
                    ripristinaBottoni();
                    PlayIcon.Symbol = Symbol.Play;
                    barraProgressione.IsIndeterminate = false;
                    barraProgressione.Value = 0;
                    barraProgressione.Visibility = Visibility.Collapsed;

                    //calcola impurità
                    float differenza =
                        (Misura.pesoInizialeIngresso - pesoIngresso) - (pesoUscita - Misura.pesoInizialeUscita);

                    //formatta con un solo numero dopo la virgola
                    differenza = (float) Math.Round((double) differenza, 1);

                    float impurita = differenza * 100 / Misura.pesoInizialeIngresso;

                    //formatta con un solo numero dopo la virgola
                    impurita = (float)Math.Round((double)impurita, 1);

                    //controlla limiti
                    if (impurita < 0)
                        impurita = 0;
                    if (impurita > 100)
                        impurita = 100;

                    //salva impurità calcolata
                    valImpurita = string.Format("{0:F1} %", impurita);

                    //salva dati misura
                    Misura.ID = numeroProgressivoMisure;
                    Misura.dataOra = dataOraSistema;
                    Misura.prodotto = impostazioniProdotto.prodotto;
                    Misura.tempoMisura = impostazioniProdotto.tempoMisura;
                    Misura.potenzaVentilatore = impostazioniProdotto.potenzaVentilatore;
                    Misura.minimo = impostazioniProdotto.minimo;
                    Misura.massimo = impostazioniProdotto.massimo;
                    Misura.pesoIngresso = pesoIngresso;
                    Misura.pesoUscita = pesoUscita;
                    Misura.pesoPulito = differenza;
                    Misura.impurita = impurita;
                    Misura.note = "-";

                    //salva nel DB
                    Database.putLogMisura(Misura);

                    //incrementa numero misure
                    numeroProgressivoMisure++;
                    txtNumeroMisura = "Nr. " + numeroProgressivoMisure;

                    //salva numero misure in DB
                    Database.putProgressivoMisure(numeroProgressivoMisure);
                    //stato macchina = standby
                    statoMacchina = (int)STATO_MACCHINA.S_STANDBY;
                    break;

            }

            //verifica se deve aggiornare bindings
            if (aggiorna)
                this.Bindings.Update();

        }

        //-----------------------------------------------------------
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {

            //abilita chaching pagina, per non perdere le informazioni quando si va su altre pagine
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

            //legge prodotti e intestazione
            ImpostazioniProdotti = Database.getImpostazioniProdotti();
            string intestazione = Database.getIntestazione();

            //seleziona ragione sociale da intestazione e aggiorna header
            string[] splitta = intestazione.Split("\r");
            splitta[0].Trim();
            if (splitta[0] != "")
                HeaderMacchina.Text = splitta[0] + " - Grain Cleaner";

            //ottiene prodotto corrente
            Windows.Storage.ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string pc = localSettings.Values["ProdottoCorrente"] as string;

            //Verifica correttezza prodotto corrente e se sono presenti prodotti
            if (ImpostazioniProdotti.Count() != 0)
            {
                if (pc == null)
                    prodottoCorrente = 0;
                else
                {
                    prodottoCorrente = Convert.ToInt32(pc);

                    //verifica errori
                    if (prodottoCorrente < 0
                        || prodottoCorrente > ImpostazioniProdotti.Count())
                    {
                        prodottoCorrente = 0;
                    }
                }

                //carica valori
                impostazioniProdotto = ImpostazioniProdotti.ElementAt(prodottoCorrente);
            }
            //altrimenti imposta un valore di default
            else
            {
                impostazioniProdotto.prodotto = "-";
                impostazioniProdotto.potenzaVentilatore = 50;
                impostazioniProdotto.tempoMisura = 120;
                impostazioniProdotto.minimo = 300;
                impostazioniProdotto.massimo = 800;
            }

            //--------------------
            //prepara per binding
            //-------------------
            //carica nome prodotto
            txtNomeProdotto = impostazioniProdotto.prodotto;
            txtCountdown = impostazioniProdotto.tempoMisura.ToString();

            //carica potenza ventilatore e ne controlla i limiti
            int vent = impostazioniProdotto.potenzaVentilatore;
            if (vent > 100)
                vent = 50;

            SliderVentilazione.Value = vent;

            //ricarica dati database
            //ricarica progressivo misure
            numeroProgressivoMisure = Database.getProgressivoMisure();
            txtNumeroMisura = "Nr. " + numeroProgressivoMisure;

            //colore peso ingresso
            colorePesoIngresso = pennelloNero;
            colorePesoUscita = pennelloNero;

            //icona play
            PlayIcon.Symbol = Symbol.Play;

            //barra progressione non visibile
            barraProgressione.Visibility = Visibility.Collapsed;

            //ripristina funzionalità bloccabili
            ripristinaBottoni();

            //ricarica stringhe
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                string txt = "( " + impostazioniProdotto.minimo + " - " + impostazioniProdotto.massimo + " )";
                txtPesoIngresso = resourceLoader.GetString("pesoIngresso") + txt;
                txtPesoUscita = resourceLoader.GetString("pesoUscita");
                txtImpurita = resourceLoader.GetString("Impurita");
                txtTara = resourceLoader.GetString("Tara");
                txtBottonePulitura = resourceLoader.GetString("AvviaPulitura");
                headerVentilazione = resourceLoader.GetString("headerSlideVentilazione");
            }

            //lista porte in ordine di preferenza
            string[] porte = { "RS485", "FT230X", "VID_0403", "PID_6001" };

            //inizializza porta
            if (!App.ComPort.aperta)
            {
                var result = App.ComPort.Initialise(porte, 9600);

            }

            //inizializza RTC 3221
            if (!DS3231_RTC.initComplete)
            {
                //inizializza DS3221
                await DS3231_RTC.InitializeAsync();

                //legge data ora
                dataOraSistema = DS3231_RTC.ReadTime();

                //converte in formato Europeo
                txtOraEdata = dataOraSistema.ToString("HH:mm dd/mm/yyyy");

                //prossima interrogazione entro 5 secondi
                prossimaInterrogazioneRTC = systemTimer + 5;

            }

            //se la data non è valida, tenta la data di sistema
            if (!DS3231_RTC.dataDisponibile)
            {
                dataOraSistema = DateTime.Now;

                //converte in formato Europeo
                try
                {
                    txtOraEdata = dataOraSistema.ToString("HH:mm dd/mm/yyyy");
                }
                catch (Exception err)
                {
                    Console.WriteLine("Exception: " + err.Message);
                }

            }

            //Inizializza timer
            TimerInterrogazioneDispositivi.Interval = TimeSpan.FromMilliseconds(1000);
            TimerInterrogazioneDispositivi.Tick += TimerInterrogazioneDispositivi_Tick;
            TimerInterrogazioneDispositivi.Start();

            ////disabilita ring attesa
            //attesa.IsActive = false;
            //attesa.Visibility = Visibility.Collapsed;
        }

        private async void Page_Loading(FrameworkElement sender, object args)
        {
            //abilita ring attesa
            attesa.Visibility = Visibility.Visible;
            attesa.IsActive = true;
        }

        //-----------------------------------------------------------
        //ripristina bottoni
        public void ripristinaBottoni()
        {
            bottoneTaraUscita.IsEnabled = true;
            bottoneTaraIngresso.IsEnabled = true;
            bottoneSelezioneProdotto.IsEnabled = true;
            BarraPulsanti.IsEnabled = true;
            Play.IsEnabled = true;
            AddTime.IsEnabled = true;
            RemoveTime.IsEnabled = true;
        }

        //-----------------------------------------------------------
        //disabilita bottoni
        private void disabilitaBottoni()
        {
            bottoneTaraUscita.IsEnabled = false;
            bottoneTaraIngresso.IsEnabled = false;
            bottoneSelezioneProdotto.IsEnabled = false;
            BarraPulsanti.IsEnabled = false;
            Play.IsEnabled = false;
            AddTime.IsEnabled = false;
            RemoveTime.IsEnabled = false;
        }

        //------------------------------------------------------------
        //ripristina etichette che vengono cancellate duranta tara o misura
        private void ripristinaEtichette()
        {
            //ricarica stringhe
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                string txt = "( " + impostazioniProdotto.minimo + " - " + impostazioniProdotto.massimo + " )";
                txtPesoIngresso = resourceLoader.GetString("pesoIngresso") + txt;
                txtPesoUscita = resourceLoader.GetString("pesoUscita");
                txtImpurita = resourceLoader.GetString("Impurita");
                txtTara = resourceLoader.GetString("Tara");
                txtBottonePulitura = resourceLoader.GetString("AvviaPulitura");
                headerVentilazione = resourceLoader.GetString("headerSlideVentilazione");
            }

        }


        //-----------------------------------------------------------
        //funzione per interrompere servizi legati alla misura
        private void interrompiMisura()
        {
            //rende visibile stato macchina
            barraProgressione.Visibility = Visibility.Collapsed;
            barraProgressione.IsIndeterminate = false;
            barraProgressione.Value = 0;

            //riabilita cose che aveva bloccato
            ripristinaBottoni();

            //carica prossimo comando UART
            prossimoComando = (int)App.COMANDO.INTERROMPI_MISURA;

        }

        //-----------------------------------------------------------
        //funzione che avvia fase terminale della misura. 3 secondi di ritardo
        private void terminaMisura()
        {
            //disabilita UI durante calcolo impurità
            interrompiMisura();
            disabilitaBottoni();

            //barra di stato in modo indeterminato
            barraProgressione.IsIndeterminate = true;

            //Crea timer di 3 secondi per stabilizzazione misura;
            ThreadPoolTimer DelayTimer = ThreadPoolTimer.CreateTimer(
                (source) =>
                {
                    //set stato machina
                    statoMacchina = (int)STATO_MACCHINA.S_TERMINA_MISURA;

                }, TimeSpan.FromSeconds(3));


        }

        //-----------------------------------------------------------
        //Servizi seriale
        //-----------------------------------------------------------
        //processa il prossimo comando da inviare
        void processaProssimoComando()
        {
            //verifica quale comando deve inviare
            switch (prossimoComando)
            {
                //invia comando esecuzione tara ingresso
                case (int)App.COMANDO.TARA_INGRESSO:
                    App.ComPort.SendString(App.CMD_GCL_DO_TARA_INGRESSO);
                    rispostaAttesa = prossimoComando;

                    break;

                //invia comando esecuzione tara uscita
                case (int)App.COMANDO.TARA_USCITA:
                    App.ComPort.SendString(App.CMD_GCL_DO_TARA_USCITA);
                    rispostaAttesa = prossimoComando;

                    break;

                //invia comando esecuzione pulitura
                case (int)App.COMANDO.AVVIA_MISURA:
                    string comandoUART = impostazioniProdotto.tempoMisura.ToString()
                        + ";"
                        + impostazioniProdotto.potenzaVentilatore.ToString()
                        + "\r";

                    App.ComPort.SendString(App.CMD_GCL_DO_MISURA + comandoUART);
                    rispostaAttesa = prossimoComando;

                    break;

                //invia comando stop pulitura
                case (int)App.COMANDO.INTERROMPI_MISURA:
                    App.ComPort.SendString(App.CMD_STOP);
                    rispostaAttesa = prossimoComando;

                    break;

                //invia comando aggiornamento ventilatore
                case (int)App.COMANDO.AGGIORNA_VENTILATORE:
                    App.ComPort.SendString(App.CMD_GCL_UPDATE_FAN + 
                        impostazioniProdotto.potenzaVentilatore.ToString() +
                        "\r");

                    rispostaAttesa = prossimoComando;

                    break;

                default:
                    prossimoComando = (int)App.COMANDO.NESSUNA;
                    break;
            }

            //cancella prossimo comando
            prossimoComando = 0;

        }

        //decodifica risposta buffer
        bool processaBuffer(string buffer)
        {
            //verifica che comando ricevuto
            if (buffer.Contains(App.CMD_GCL_REPLY_PESO))
            {
                //verifica se si stava aspettando risposta per l'ingresso o per l'uscita
                switch (rispostaAttesa)
                {
                    //Peso ingresso
                    case (int)App.COMANDO.PESO_INGRESSO:
                        //legge valore in formato stringa, e converte in float
                        valPesoIngresso = getValoreLetto();
                        valPesoIngresso = valPesoIngresso.Replace('.', ',');
                        pesoIngresso = Convert.ToSingle(valPesoIngresso);
                        valPesoIngresso += " g";

                        //processa colore peso ingresso: Se 0 colore = nero
                        if (pesoIngresso == 0)
                        {
                            colorePesoIngresso = pennelloNero;
                        }
                        //altrimenti, se entro range peso pulitura prodotto, è verde
                        if (pesoIngresso >= impostazioniProdotto.minimo
                            && pesoIngresso <= impostazioniProdotto.massimo)
                        {
                            colorePesoIngresso = pennelloVerde;
                        }
                        //altrimenti è rossa
                        else
                            colorePesoIngresso = pennelloRosso;


                        break;

                    //Peso Uscita
                    case (int)App.COMANDO.PESO_USCITA:
                        //legge valore in formato stringa, e converte in float
                        valPesoUscita = getValoreLetto();
                        valPesoUscita = valPesoUscita.Replace('.', ',');
                        pesoUscita = Convert.ToSingle(valPesoUscita);
                        valPesoUscita += " g";

                        break;

                    //entrambi i pesi
                    case (int)App.COMANDO.ENTRAMBI_I_PESI:
                        //legge valori letti in formato stringa, poi li converte in float
                        getValoriLetti();

                        valPesoIngresso = valPesoIngresso.Replace('.', ',');
                        pesoIngresso = Convert.ToSingle(valPesoIngresso);
                        valPesoUscita = valPesoUscita.Replace('.', ',');
                        pesoUscita = Convert.ToSingle(valPesoUscita);

                        //processa colore peso ingresso: Se 0 colore = nero
                        if (pesoIngresso == 0)
                        {
                            colorePesoIngresso = pennelloNero;
                        }
                        //altrimenti, se entro range peso pulitura prodotto, è verde
                        else if (pesoIngresso >= impostazioniProdotto.minimo
                            && pesoIngresso <= impostazioniProdotto.massimo)
                        {
                            colorePesoIngresso = pennelloVerde;
                        }
                        //altrimenti è rossa
                        else
                            colorePesoIngresso = pennelloRosso;

                        //aggiunge "g" dopo per evitare errori conversione
                        valPesoIngresso += " g";
                        valPesoUscita += " g";

                        //colore peso uscita
                        if (pesoUscita < 0)
                            colorePesoUscita = pennelloRosso;
                        else
                            colorePesoUscita = pennelloNero;

                        break;

                }

                return (true);
            }

            //verifica se stava aspettando risposta esecuzione tara
            if (buffer.Contains(App.CMD_GCL_REPLY_TARA))
            { 
                //verifica se si stava aspettando risposta per l'ingresso o per l'uscita
                switch (rispostaAttesa)
                {
                    //Tara ingresso
                    case (int)App.COMANDO.TARA_INGRESSO:

                        break;

                    //Tara uscita
                    case (int)App.COMANDO.TARA_USCITA:

                        break;
                }

                //ripristina etichette
                ripristinaEtichette();

                //ripristina funzionalità bloccate
                ripristinaBottoni();

                return (true);
            }

            //verifica se stava aspettando risposta avvio misura
            if (buffer.Contains(App.CMD_GCL_REPLY_MISURA))
            {
                //verifica se andata a buon fine oppure no
                if (buffer.Contains(App.CMD_GCL_REPLY_OK))
                {
                    //con risposta positiva setta stato macchina in misurazione
                    statoMacchina = (int)STATO_MACCHINA.S_MISURAZIONE;

                }
                //se avvio misura non andato a buon fine, arresta misura e torna in standby
                else
                {
                    //ripristina in stanby
                    statoMacchina = (int)STATO_MACCHINA.S_STANDBY;
                    ripristinaBottoni();

                }
            }

            //verifica se stava aspettando risposta a stop misura
            if (buffer.Contains(App.CMD_STOP))
            {
                //ATTENZIONE qui bisognerà mettere esecuzione misura
                //ripristina in stanby
                statoMacchina = (int)STATO_MACCHINA.S_STANDBY;
                ripristinaBottoni();

            }

            return (false);
        }

        //-----------------------------------------------------------
        //legge il peso dal buffer e lo formatta
        public string getValoreLetto()
        {
            string buffer;
            int pos;

            //Leggere Buffer di ricezione ed eventualmente lo azzera
            buffer = App.ComPort.bufferRicezione;
            App.ComPort.ClearBufferRicezione();

            //pulisce il buffer dai caratteri terminatori
            buffer = buffer.Trim();

            //legge dato
            pos = posizioneDatoRicevuto(buffer);
            return (buffer.Substring(pos));
        }

        //-----------------------------------------------------------
        //legge entrambi i pesi dal buffer e li formatta
        void getValoriLetti()
        {
            string buffer;
            int pos;

            //Leggere Buffer di ricezione ed eventualmente lo azzera
            buffer = App.ComPort.bufferRicezione;
            App.ComPort.ClearBufferRicezione();

            //pulisce il buffer dai caratteri terminatori
            buffer = buffer.Trim();

            //legge dati
            pos = posizioneDatoRicevuto(buffer);
            buffer = buffer.Substring(pos);

            //split dati
            string[] split = buffer.Split(";", StringSplitOptions.None);

            valPesoIngresso = split[0].Trim();
            valPesoUscita = split[1].Trim();


        }

        //-----------------------------------------------------------
        //trova posizione dato ricevuto (dopo >)
        int posizioneDatoRicevuto(string buffer)
        {
            return (buffer.IndexOf('>') + 1);
        }

        //-----------------------------------------------------------
        public static void ImpostaProdottoCorrente(int prod)
        {
            //salva nella variabile locale
            prodottoCorrente = prod;

            //salva nelle impostazioni non volatili
            //Salva impostazioni ed eventualmente le inizializza
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            localSettings.Values["ProdottoCorrente"] =
                prodottoCorrente.ToString();
        }

        //-----------------------------------------------------------
        //funzione usata per fare comparire box popup errori. Bisogna passare l'ID del errore (enumerazione ERRORI)
        public static async System.Threading.Tasks.Task messaggioAsync(int errorID)
        {
            string titolo = "";
            string messaggio = "";
            string ok = "OK";

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

            if (resourceLoader != null)
            {
                //carica stringhe a seconda dell'ID
                switch (errorID)
                {
                    //prodotto nella pesa insufficiente
                    case (int)ERRORI.PRODOTTO_INSUFFICIENTE:
                        titolo = resourceLoader.GetString("ProdottoInsufficiente");
                        messaggio = resourceLoader.GetString("TestoProdottoInsufficiente");
                        messaggio += " "
                            + impostazioniProdotto.minimo.ToString()
                            + " - "
                            + impostazioniProdotto.massimo.ToString()
                            + "g";

                        break;

                    //prodotto nella pesa troppo abbondante
                    case (int)ERRORI.PRODOTTO_ABBONDANTE:
                        titolo = resourceLoader.GetString("ProdottoEccedenza");
                        messaggio = resourceLoader.GetString("TestoProdottoEccedenza");
                        messaggio += " "
                            + impostazioniProdotto.minimo.ToString()
                            + " - "
                            + impostazioniProdotto.massimo.ToString()
                            + "g";

                        break;

                    //Errore casseto pieno su avvio misura
                    case (int)ERRORI.CASSETTO_USCITA_PIENO:
                        titolo = resourceLoader.GetString("CassettoPieno");
                        messaggio = resourceLoader.GetString("SvuotareIlCassetto");

                        break;

                    //Errore assenza cassetto
                    case (int)ERRORI.MANCA_CASSETTO_USCITA:
                        titolo = resourceLoader.GetString("MancaCassettoUscita");
                        messaggio = resourceLoader.GetString("RimettereIlCassetto");

                        break;

                    //errore sconosciuto
                    default:
                        titolo = "ERROR";
                        messaggio = "UNKNOW ERROR";
                        break;

                }
            }

            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(messaggio, titolo);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            messageDialog.Commands.Add(new UICommand(ok));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 0;

            // Show the message dialog
            await messageDialog.ShowAsync();

        }

        //-------------------------------------------------------------
        //regolazione orologio
        //-------------------------------------------------------------
        public static void ImpostaDataOra(DateTime dataOra)
        {
            //imposta ora e data solo se disponibile modulo dataora
            if(DS3231_RTC.dataDisponibile)
            {
                DS3231_RTC.SetTime(dataOra);
            }

        }

        //--------------------------------------------------------------
        //Handlers
        //--------------------------------------------------------------
        private void CommandInvokedHandler(IUICommand command)
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
                //ferma timer interrogazione
                TimerInterrogazioneDispositivi.Stop();

                //#if !DEBUG
                //spegni apparecchiatura
                ShutdownManager.BeginShutdown(ShutdownKind.Shutdown, TimeSpan.FromSeconds(2));

                //#endif

                //chiudi l'applicazione.
                Application.Current.Exit();
            }
        }

        //-----------------------------------------------------------
        //Gestione eventi
        //-----------------------------------------------------------
        async private void Esci_Click(object sender, RoutedEventArgs e)
        {
            string titolo = " ";
            string messaggio = " ";
            string si = "Yes";
            string no = "Not";

            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (resourceLoader != null)
            {
                titolo = resourceLoader.GetString("ArrestoSistema");
                messaggio = resourceLoader.GetString("SicuroSpegnere");
                si = resourceLoader.GetString("Si");
                no = resourceLoader.GetString("No");
            }

            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(messaggio, titolo);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            messageDialog.Commands.Add(new UICommand(
                si,
                new UICommandInvokedHandler(this.CommandInvokedHandler)));
            messageDialog.Commands.Add(new UICommand(
                no,
                new UICommandInvokedHandler(this.CommandInvokedHandler)));

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 0;

            // Show the message dialog
            await messageDialog.ShowAsync();


        }

        private void Impostazioni_Click(object sender, RoutedEventArgs e)
        {
            //ferma timer
            TimerInterrogazioneDispositivi.Stop();

            //mostra pagina impostazioni
            this.Frame.Navigate(typeof(ImpostazioniGenerali));

        }


        private async void avvioMisura(object sender, RoutedEventArgs e)
        {
            //processa in base a stato macchina:
            switch (statoMacchina)
            {
                case (int) STATO_MACCHINA.S_STANDBY:
                    //verifica se quantità prodotto disponibile è sufficiente:
                    if (pesoIngresso < impostazioniProdotto.minimo)
                    {
                        await messaggioAsync((int) ERRORI.PRODOTTO_INSUFFICIENTE);

                        break;
                    }

                    //verifica se quantità prodotto disponibile è troppa:
                    if (pesoIngresso > impostazioniProdotto.massimo)
                    {
                        await messaggioAsync((int)ERRORI.PRODOTTO_ABBONDANTE);

                        break;
                    }

                    //verifica se quantità prodotto disponibile è troppa:
                    if (pesoUscita > 20)
                    {
                        await messaggioAsync((int)ERRORI.CASSETTO_USCITA_PIENO);

                        break;
                    }

                    //verifica se cassetto assente
                    if (pesoUscita < -100)
                    {
                        await messaggioAsync((int)ERRORI.MANCA_CASSETTO_USCITA);

                        break;
                    }

                    //quando misura attiva, non è possibile selezionare i prodotti, ne usare la barra bottoni in alto, ne fare tara
                    disabilitaBottoni();

                    //Azzera valore impurità
                    valImpurita = "---";

                    //rende visibile stato macchina
                    barraProgressione.IsIndeterminate = true;
                    barraProgressione.Visibility = Visibility.Visible;

                    //salva i valori di partenza
                    Misura.pesoInizialeIngresso = pesoIngresso;
                    Misura.pesoInizialeUscita = pesoUscita;
                    tempoInizialeMisura = impostazioniProdotto.tempoMisura;
                    countdownTimer = impostazioniProdotto.tempoMisura;

                    //carica prossimo comando UART
                    prossimoComando = (int)App.COMANDO.AVVIA_MISURA;

                    //avvia misura
                    statoMacchina++;

                    break;

                //case (int)STATO_MACCHINA.S_AVVIO_MISURA:
                case (int)STATO_MACCHINA.S_MISURAZIONE:
                    terminaMisura();

                    break;

                //definizione impurità
                case (int)STATO_MACCHINA.S_TERMINA_MISURA:
                    //calcolo impurita

                    break;

            }
        }

        private void CambiaProdotto(object sender, RoutedEventArgs e)
        {
            //ferma timer
            TimerInterrogazioneDispositivi.Stop();

            //mostra pagina impostazioni
            this.Frame.Navigate(typeof(presetProdotti));

        }

        private void TaraIngressoClick(object sender, RoutedEventArgs e)
        {
            //prepara comando da inviare
            prossimoComando = (int)App.COMANDO.TARA_INGRESSO;

            //mette -- su display
            txtPesoIngresso = "---";

            //inibisce bottone tara ed avvio
            disabilitaBottoni();

        }

        private void TaraUscitaClick(object sender, RoutedEventArgs e)
        {
            prossimoComando = (int)App.COMANDO.TARA_USCITA;

            //mette -- su display
            txtPesoUscita = "---";

            //inibisce bottone tara
            disabilitaBottoni();
        
        }

        private void RemoveTimeClick(object sender, RoutedEventArgs e)
        {
            //decrementa tempo misura
            if (impostazioniProdotto.tempoMisura > 10)
            {
                impostazioniProdotto.tempoMisura -= 10;

                //se in misura aumenta countdown di 10 secondi
                if (statoMacchina == (int)STATO_MACCHINA.S_MISURAZIONE)
                {
                    countdownTimer -= 10;
                    txtCountdown = countdownTimer.ToString();
                }
                //altrimenti incrementa di 10 secondi il tempo di misura
                else
                {
                    txtCountdown = impostazioniProdotto.tempoMisura.ToString();
                }
            }

            //aggiorna display
            this.Bindings.Update();
        }

        private void AddTimeClick(object sender, RoutedEventArgs e)
        {
            impostazioniProdotto.tempoMisura += 10;

            //se in misura decrementa countdown
            if (statoMacchina == (int)STATO_MACCHINA.S_MISURAZIONE)
            {
                countdownTimer += 10;
                txtCountdown = countdownTimer.ToString();
            }
            //altrimenti decrementa di 10 secondi il tempo di misura
            else
            {
                txtCountdown = impostazioniProdotto.tempoMisura.ToString();
            }

            //aggiorna display
            this.Bindings.Update();
        }

        private void bottoneLogMisure_Click(object sender, RoutedEventArgs e)
        {
            //ferma timer
            TimerInterrogazioneDispositivi.Stop();

            //mostra pagina impostazioni
            this.Frame.Navigate(typeof(LogMisure));

        }
    }
}
