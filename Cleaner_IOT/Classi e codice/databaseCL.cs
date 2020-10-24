//using System.Data;
//using System.Text;
//using System.Threading.Tasks;
//using Windows.Storage;
using Microsoft.Data.Sqlite;
using System;
//using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
//using System.Threading;

namespace DatabaseManaging
{
    public class database
    {
        //classe che indica come è fatta l'impostazione di ogni singolo sensore
        public class ImpostazioniProdotto : INotifyPropertyChanged
        {
            public int ID { get; set; }
            public string prodotto { get; set; }
            public int tempoMisura { get; set; }
            public int potenzaVentilatore { get; set; }
            public float minimo { get; set; }
            public float massimo { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

        }

        //classe misura
        public class Misura : INotifyPropertyChanged
        {
            public int ID { get; set; }
            public DateTime dataOra { get; set; }
            public string prodotto { get; set; }
            public int tempoMisura { get; set; }
            public int potenzaVentilatore { get; set; }
            public float minimo { get; set; }
            public float massimo { get; set; }

            public float pesoInizialeIngresso { get; set; }
            public float pesoInizialeUscita { get; set; }
            public float pesoIngresso { get; set; }
            public float pesoUscita { get; set; }
            public float pesoPulito { get; set; }
            public float impurita { get; set; }
            public string note { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

        }

        //collezione di tutte le impostazioni sensore (definisce l'intero impianto)
        public ObservableCollection<ImpostazioniProdotto> ImpostazioniProdotti
            = new ObservableCollection<ImpostazioniProdotto>();

        //ottiene le impostazioni sensori dal DB e le mette in una collezione
        public ObservableCollection<ImpostazioniProdotto> getImpostazioniProdotti()
        {
            var impostazioni
            = new ObservableCollection<ImpostazioniProdotto>();

            using (SqliteConnection db =
                new SqliteConnection("Filename=" + Cleaner_IOT.App.DB_PATH))
            {
                try
                {
                    db.Open();

                    SqliteCommand selectCommand = new SqliteCommand
                        ("SELECT * FROM ImpostazioniProdotti;", db);

                    SqliteDataReader query = selectCommand.ExecuteReader();

                    impostazioni.Clear();

                    while (query.Read())
                    {
                        //crea variabile Impostazione Impianto
                        var tabI = new ImpostazioniProdotto();

                        //legge campi
                        tabI.ID = query.GetInt32(0);
                        tabI.prodotto = query.GetString(1);
                        tabI.tempoMisura = query.GetInt32(2);
                        tabI.potenzaVentilatore = query.GetInt32(3);
                        tabI.minimo = query.GetInt32(4);
                        tabI.massimo = query.GetInt32(5);

                        //Aggiungi riga
                        impostazioni.Add(tabI);

                    }

                    db.Close();

                    //salva quanto letto nella variabile globale
                    ImpostazioniProdotti = impostazioni;

                    return impostazioni;
                }
                catch (Exception eSql)
                {
                    Console.WriteLine("Exception: " + eSql.Message);
                }

                return null;
            }
        }

        public bool putImpostazioniProdotti(ObservableCollection<ImpostazioniProdotto> imp)
        {
            int id;

            using (SqliteConnection db =
                new SqliteConnection("Filename=" + Cleaner_IOT.App.DB_PATH))
            {
                try
                {
                    var command = db.CreateCommand();

                    //apre database
                    db.Open();

                    //cancella tabella per poi riscriverla successivamente
                    using (command)
                    {
                        try
                        {
                            //invia comando
                            string comando = @"DELETE FROM ImpostazioniProdotti";
                            command.CommandTimeout = 30;
                            command.CommandText = comando;
                            command.ExecuteNonQuery();

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }

                    }


                    //controlla in loop quali linee hanno bisogno di essere aggiornate nel DB
                    for (id = 0; id < imp.Count; id++)
                    {

                        using (command)
                        {
                            //crea stringa aggiornamento impostazione
                            string colonne = @"(prodotto, tempoMisura, potenzaVentilatore, minimo, massimo)";

                            string valori =
                                @" VALUES " +
                                string.Format(@"('{0}', {1}, {2}, {3}, {4});",
                                imp.ElementAt(id).prodotto,
                                imp.ElementAt(id).tempoMisura,
                                imp.ElementAt(id).potenzaVentilatore,
                                imp.ElementAt(id).minimo,
                                imp.ElementAt(id).massimo);

                            //inizio transazione
                            var transaction = db.BeginTransaction();

                            try
                            {


                                //invia comando
                                string comando = @"INSERT INTO ImpostazioniProdotti " + colonne + valori;

                                //aggiorna row su DB
                                command.Transaction = transaction;
                                command.CommandTimeout = 30;
                                command.CommandText = comando;
                                command.ExecuteNonQuery();

                                transaction.Commit();

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                transaction.Rollback();
                            }
                            finally
                            {
                                transaction.Dispose();
                            }
                        }

                    }

                    db.Close();

                    //aggiorna variabile pubblica
                    ImpostazioniProdotti = imp;

                    return true;
                }
                catch (Exception eSql)
                {
                    Console.WriteLine("Exception: " + eSql.Message);
                }
            }
            return false;
        }

        //ottieni ultimo progressivo misure
        public int getProgressivoMisure()
        {
            int ret = 0;

            using (SqliteConnection db =
                new SqliteConnection("Filename=" + Cleaner_IOT.App.DB_PATH))
            {
                try
                {
                    //apre database
                    db.Open();

                    SqliteCommand selectCommand = new SqliteCommand
                        ("SELECT * FROM ProgressivoMisure;", db);

                    //read query
                    SqliteDataReader qr = selectCommand.ExecuteReader();
                    qr.Read();

                    ret = qr.GetInt32(0);

                    db.Close();


                }
                catch (Exception eSql)
                {
                    Console.WriteLine("Exception: " + eSql.Message);
                }
            }

            return ret;

        }

        //aggiorna numero misure
        public bool putProgressivoMisure(int value)
        {
            using (SqliteConnection db =
                new SqliteConnection("Filename=" + Cleaner_IOT.App.DB_PATH))
            {
                try
                {
                    var command = db.CreateCommand();

                    //apre database
                    db.Open();

                    //-----------------------------
                    //---cread query di update
                    //-----------------------------
                    //inizia a creare comando
                    string comando =
                        string.Format(@"UPDATE ProgressivoMisure SET misure='{0}' WHERE ROWID=1;", value);

                    //inizio transazione
                    var transaction = db.BeginTransaction();

                    try
                    {
                        //esegui query creazione tabella (se esiste)
                        command.Transaction = transaction;
                        command.CommandTimeout = 30;
                        command.CommandText = comando;
                        command.ExecuteNonQuery();

                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        transaction.Rollback();

                        return false;
                    }
                    finally
                    {
                        transaction.Dispose();
                    }

                    db.Close();

                }
                catch (Exception eSql)
                {
                    Console.WriteLine("Exception: " + eSql.Message);
                }
            }

            return true;

        }

        //---------------------------------------------------------------------------------------------------------
        //salva una misura nel DB
        public bool putLogMisura(Misura misura)
        {
            using (SqliteConnection db =
                new SqliteConnection("Filename=" + Cleaner_IOT.App.DB_PATH))
            {
                try
                {
                    var command = db.CreateCommand();

                    //apre database
                    db.Open();

                    //-----------------------------
                    //---Inserisce nel log la misura
                    //-----------------------------
                    //inizia a creare comando
                    string comando =
                        @"INSERT INTO logMisure (ID, dataOra, prodotto, " +
                        @"tempoMisura, potenzaVentilatore, minimo, massimo, pesoInizialeIngresso, pesoInizialeUscita, " +
                        @"pesoIngresso, pesoUscita, pesoPulito, impurita, Note) VALUES (" +
                        string.Format(@"'{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', ",
                            misura.ID,
                            misura.dataOra,
                            misura.prodotto,
                            misura.tempoMisura,
                            misura.potenzaVentilatore,
                            misura.minimo,
                            misura.massimo,
                            misura.pesoInizialeIngresso);

                    comando +=
                        string.Format(@"'{0}', '{1}', '{2}', '{3}', '{4}', '",
                            misura.pesoInizialeUscita,
                            misura.pesoIngresso,
                            misura.pesoUscita,
                            misura.pesoPulito,
                            misura.impurita);

                    comando += misura.note;

                    comando += "');";

                    //inizio transazione
                    var transaction = db.BeginTransaction();

                    try
                    {
                        //esegui query creazione tabella (se esiste)
                        command.Transaction = transaction;
                        command.CommandTimeout = 30;
                        command.CommandText = comando;
                        command.ExecuteNonQuery();

                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        transaction.Rollback();

                        return false;
                    }
                    finally
                    {
                        transaction.Dispose();
                    }

                    db.Close();

                }
                catch (Exception eSql)
                {
                    Console.WriteLine("Exception: " + eSql.Message);
                }
            }

            return true;

        }

        //------------------------------------------------------------------------------------------------
        //ottiene log misure
        public ObservableCollection<Misura> getLogMisure()
        {
            var misure
            = new ObservableCollection<Misura>();

            using (SqliteConnection db =
                new SqliteConnection("Filename=" + Cleaner_IOT.App.DB_PATH))
            {
                try
                {
                    db.Open();

                    SqliteCommand selectCommand = new SqliteCommand
                        ("SELECT * FROM logMisure;", db);

                    SqliteDataReader query = selectCommand.ExecuteReader();

                    misure.Clear();

                    while (query.Read())
                    {
                        //crea variabile Impostazione Impianto
                        var tabI = new Misura();

                        //legge campi
                        tabI.ID = query.GetInt32(0);
                        tabI.dataOra = Convert.ToDateTime(query.GetString(1));
                        tabI.prodotto = query.GetString(2);
                        tabI.tempoMisura = query.GetInt32(3);
                        tabI.potenzaVentilatore = query.GetInt32(4);
                        tabI.minimo = query.GetFloat(5);
                        tabI.massimo = query.GetFloat(6);
                        tabI.pesoInizialeIngresso = query.GetFloat(7);
                        tabI.pesoInizialeUscita = query.GetFloat(8);
                        tabI.pesoIngresso = query.GetFloat(9);
                        tabI.pesoUscita = query.GetFloat(10);
                        tabI.pesoPulito = query.GetFloat(11);
                        tabI.impurita = query.GetFloat(12);
                        tabI.note = query.GetString(13);

                        //Aggiungi riga
                        misure.Add(tabI);

                    }

                    db.Close();

                    return misure;
                }
                catch (Exception eSql)
                {
                    Console.WriteLine("Exception: " + eSql.Message);
                }

                return null;
            }


        }

        //---------------------------------------------------------------------------------------------------
        //aggiorna la nota all'interno del DB
        public bool updateNoteLogMisura(int ID, string txtNota)
        {
            using (SqliteConnection db =
                new SqliteConnection("Filename=" + Cleaner_IOT.App.DB_PATH))
            {
                try
                {
                    var command = db.CreateCommand();

                    //apre database
                    db.Open();

                    //-----------------------------
                    //---Inserisce nel log la misura
                    //-----------------------------
                    //inizia a creare comando
                    string comando =
                        @"UPDATE logMisure SET note = '" +
                        txtNota +
                        @"' WHERE " +
                        string.Format(@"ID = '{0}';", ID);

                    //inizio transazione
                    var transaction = db.BeginTransaction();

                    try
                    {
                        //esegui query creazione tabella (se esiste)
                        command.Transaction = transaction;
                        command.CommandTimeout = 30;
                        command.CommandText = comando;
                        command.ExecuteNonQuery();

                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        transaction.Rollback();

                        return false;
                    }
                    finally
                    {
                        transaction.Dispose();
                    }

                    db.Close();

                }
                catch (Exception eSql)
                {
                    Console.WriteLine("Exception: " + eSql.Message);
                }
            }

            return true;
        }

        //---------------------------------------------------------------------------------------------------
        //cancella l'intero log misure
        public void cancellaLogMisura()
        {
            using (SqliteConnection db =
                new SqliteConnection("Filename=" + Cleaner_IOT.App.DB_PATH))
            {
                try
                {
                    var command = db.CreateCommand();

                    //apre database
                    db.Open();

                    //-----------------------------
                    //---Inserisce nel log la misura
                    //-----------------------------
                    //inizia a creare comando
                    string comando = @"DELETE FROM logMisure";

                    //inizio transazione
                    var transaction = db.BeginTransaction();

                    try
                    {
                        //esegui query creazione tabella (se esiste)
                        command.Transaction = transaction;
                        command.CommandTimeout = 30;
                        command.CommandText = comando;
                        command.ExecuteNonQuery();

                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        transaction.Rollback();

                    }
                    finally
                    {
                        transaction.Dispose();
                    }

                    db.Close();

                }
                catch (Exception eSql)
                {
                    Console.WriteLine("Exception: " + eSql.Message);
                }
            }


        }

        //---------------------------------------------------------------------------------------------------
        //legge intestazione apparecchio dal DB
        public string getIntestazione()
        {
            string ret = "";

            using (SqliteConnection db =
                new SqliteConnection("Filename=" + Cleaner_IOT.App.DB_PATH))
            {
                try
                {
                    db.Open();

                    SqliteCommand selectCommand = new SqliteCommand
                        ("SELECT * FROM Intestazione;", db);

                    SqliteDataReader query = selectCommand.ExecuteReader();

                    query.Read();

                    for (int i = 0; i < 8 ; i++)
                    {
                        //legge tutti i campi, se non nulli li aggiunge
                        if(!string.IsNullOrEmpty(query.GetString(i)))
                            ret += query.GetString(i) + "\r";

                    }

                    db.Close();

                }
                catch (Exception eSql)
                {
                    Console.WriteLine("Exception: " + eSql.Message);
                    //return "";

                }

            }

            return (ret);
        }

        //salva intestazione scontrino nel DB
        public bool putIntestazione(string testoIntestazione)
        {
            using (SqliteConnection db =
                new SqliteConnection("Filename=" + Cleaner_IOT.App.DB_PATH))
            {
                try
                {
                    var command = db.CreateCommand();

                    //crea liste stringhe
                    string[] splitIntestazione = testoIntestazione.Split("\r");
                    string[] colonne = {
                        "RagioneSociale", "Indirizzo", "Indirizzo2", "Citta", 
                        "Nazione", "Telefono", "Mail", "Altro"
                    };

                    //inizia a creare comando
                    string comando = @"UPDATE Intestazione SET";

                    for (int i = 0; i < splitIntestazione.Length; i++)
                    {
                        //controlla se riga è nulla, altrimenti skip
                        if(splitIntestazione[i] != null)
                        {
                            comando +=
                                " " +
                                colonne[i] +
                                "='" +
                                splitIntestazione[i].Trim() +
                                "',";

                        }
                    }

                    //rimuove virgola in più
                    int l = comando.Length - 1;
                    comando = comando.Substring(0, l);

                    //termina stringa comando
                    comando += " WHERE ROWID = 1;";

                    //apre database
                    db.Open();

                    //inizio transazione
                    var transaction = db.BeginTransaction();

                    try
                    {
                        //esegui query creazione tabella (se esiste)
                        command.Transaction = transaction;
                        command.CommandTimeout = 30;
                        command.CommandText = comando;
                        command.ExecuteNonQuery();

                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        transaction.Rollback();

                        return false;
                    }
                    finally
                    {
                        transaction.Dispose();
                    }

                    db.Close();

                }
                catch (Exception eSql)
                {
                    Console.WriteLine("Exception: " + eSql.Message);

                }
            }

            return true;

        }

    }


}
