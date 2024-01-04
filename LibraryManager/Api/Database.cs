using System;
using Npgsql;
using System.Collections.Generic;
using System.Configuration;
using projekt.Constants;
using System.Threading.Tasks;
using NpgsqlTypes;
using System.Linq;
using LibraryManager.Dtos;
using System.Text;
using System.Globalization;
using NLog.Config;
using NLog;
using System.Windows;
using NLog.Layouts;

public class Database
{
    private const string _connectionString = DbConstants.ConnectionString;
    private NpgsqlConnection? _dbConnection;
    private static NLog.Logger _logger;
    public Database() 
    {
        _logger =  NLog.LogManager.GetCurrentClassLogger(); 
        _logger.Info("This is an informational log message");

        try
        {
            _dbConnection = new NpgsqlConnection(_connectionString);
            _dbConnection.Open();
            _logger.Info("Connected to PostgreSQL");
        }
        catch (NpgsqlException ex) 
        { 
            _logger.Error(ex.Message);
        }
    }
    public async Task<int> GetEgzemplarzKsiazki(int ksiazkaId)
    {
        int result = -1;
        try
        {
            string sql = "SELECT * FROM egzemplarz WHERE ksiazka_id = @ksiazka_id LIMIT 1";


            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                cmd.Parameters.AddWithValue("@ksiazka_id", ksiazkaId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        result = (int)reader["egzemplarz_id"];
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
        return result;
    }
    public async Task<List<CzytelnikDto>> GetCzytelnicy()
    {
        List<CzytelnikDto> results = new List<CzytelnikDto>();
        try
        {
            string sql = "SELECT * FROM czytelnik";


            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        results.Add(new CzytelnikDto
                        {
                            Id = (int)reader["czytelnik_id"],
                            Imie = reader["imie"].ToString(),
                            Nazwisko = reader["nazwisko"].ToString(),
                            Adres = reader["adres"].ToString(),
                            Email = reader["email"].ToString(),
                            Telefon = reader["telefon"].ToString()
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
        return results;
    }
    async public Task<List<KsiazkaDto>> GetAllBooks(int? sortby = null, string tytul = null)
    {
        List<KsiazkaDto> results = new List<KsiazkaDto>();
        try
        {
            string sql = "SELECT * FROM ksiazka";

            if (sortby != null)
                sql = "SELECT * FROM Ksiazka WHERE dziedzina_id = @dziedzinaId";
            if(tytul != null)
                sql = "SELECT * FROM Ksiazka WHERE tytul LIKE @tytul";

            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                if (sortby != null)
                    cmd.Parameters.AddWithValue("@dziedzinaId", sortby);
                if(tytul != null)
                {
                    tytul = "%" + tytul + "%";
                    cmd.Parameters.AddWithValue("@tytul", tytul);
                }
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        results.Add(new KsiazkaDto
                        {
                            Id = (int)reader["ksiazka_id"],
                            Tytul = reader["tytul"].ToString(),
                            RokWydania = reader["rok_wydania"].ToString()
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
        return results;
    }

    async public Task<List<RentalDto>> GetAllRentals(int czytelnik_id)
    {
        List<RentalDto> results = new List<RentalDto>();
        try
        {
            var sql =
                "SELECT wypozyczenie_id, nazwisko, imie, tytul, data_wypozyczenia, data_zwrotu, rok_wydania " +
                "FROM wypozyczenie w " +
                "JOIN egzemplarz e USING (egzemplarz_id) " +
                "JOIN ksiazka k USING (ksiazka_id) " +
                "JOIN czytelnik c USING (czytelnik_id) " +
                "WHERE czytelnik_id = @czytelnik_id";

            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                cmd.Parameters.AddWithValue("@czytelnik_id", czytelnik_id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {

                        RentalDto rental = new RentalDto
                        {
                            Id = (int)reader["wypozyczenie_id"],
                            Nazwisko = reader["nazwisko"].ToString(),
                            Imie = reader["imie"].ToString(),
                            Tytul = reader["tytul"].ToString(),
                            DataWypozyczenia = Convert.ToDateTime(reader["data_wypozyczenia"]),
                            RokWydania = Convert.ToDateTime(reader["rok_wydania"]).Date
                        };
                        if(reader["data_zwrotu"] is not DBNull)
                            rental.DataZwrotu = Convert.ToDateTime(reader["data_zwrotu"]);

                        results.Add(rental);

                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
        return results;
    }
    async public Task<List<AutorDto>> GetAuthors()
    {
        List<AutorDto> authors = new List<AutorDto>();
        try
        {
            var sql = "SELECT autor_id, CONCAT(imie, ' ', nazwisko) AS author_name FROM Autor";
            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        authors.Add(new AutorDto
                        {
                            Id = Convert.ToInt32(reader["autor_id"]),
                            Name = reader["author_name"].ToString()
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
        return authors;
    }
    public async Task<List<WydawnictwoDto>> GetPublishers()
    {
        List<WydawnictwoDto> publishers = new List<WydawnictwoDto>();
        try
        {
            var sql = "SELECT wydawnictwo_id, nazwa FROM Wydawnictwo";
            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        var publisher = new WydawnictwoDto
                        {
                            Id = Convert.ToInt32(reader["wydawnictwo_id"]),
                            Nazwa = reader["nazwa"].ToString()
                        };
                        publishers.Add(publisher);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
        return publishers;
    }
    public async Task<List<DziedzinaDto>> GetGenres()
    {
        List<DziedzinaDto> genres = new List<DziedzinaDto>();
        try
        {
            var sql = "SELECT dziedzina_id, nazwa FROM Dziedzina";
            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        var genre = new DziedzinaDto
                        {
                            Id = Convert.ToInt32(reader["dziedzina_id"]),
                            Nazwa = reader["nazwa"].ToString()
                        };
                        genres.Add(genre);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
        return genres;
    }
    
    public async Task<bool> GetAvailability(int ksiazka_id)
    {
        try
        {
            var sql = "SELECT dostepnosc FROM Ksiazka WHERE ksiazka_id = @ksiazka_id";
            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                cmd.Parameters.AddWithValue("@ksiazka_id", ksiazka_id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        var dostepnosc = (bool)reader["dostepnosc"];
                        return dostepnosc;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
        return false;
    }

    async public Task AddBook(string tytul, string rok_wydania, int author_id, int publisher_id, int genre_id)
    {
        try
        {
            if (DateTime.TryParse(rok_wydania, out DateTime parsedDate))
            {
                using var cmd = new NpgsqlCommand("INSERT INTO ksiazka (tytul, rok_wydania, wydawnictwo_id, dziedzina_id) VALUES (@tytul, @rok_wydania, @wydawnictwo_id, @dziedzina_id)", _dbConnection);
                
                cmd.Parameters.AddWithValue("@tytul", tytul);
                cmd.Parameters.AddWithValue("@rok_wydania", NpgsqlDbType.Date, parsedDate);
                cmd.Parameters.AddWithValue("@wydawnictwo_id", publisher_id);
                cmd.Parameters.AddWithValue("@dziedzina_id", genre_id);
                object result = await cmd.ExecuteNonQueryAsync();

                var selectSql = "SELECT ksiazka_id FROM ksiazka WHERE tytul = @tytul AND rok_wydania = @rok_wydania AND wydawnictwo_id = @wydawnictwo_id AND dziedzina_id = @dziedzina_id";

                using var selectCmd = new NpgsqlCommand(selectSql, _dbConnection);
                selectCmd.Parameters.AddWithValue("@tytul", tytul);
                selectCmd.Parameters.AddWithValue("@rok_wydania", NpgsqlDbType.Date, parsedDate);
                selectCmd.Parameters.AddWithValue("@wydawnictwo_id", publisher_id);
                selectCmd.Parameters.AddWithValue("@dziedzina_id", genre_id);
                int ksiazkaId = (int)(await selectCmd.ExecuteScalarAsync() ?? -1);

            if (ksiazkaId != 0) 
                {
                    using var cmd2 = new NpgsqlCommand("INSERT INTO ksiazka_autor VALUES(@ksiazka_id, @autor_id)", _dbConnection);
                    cmd2.Parameters.AddWithValue("@ksiazka_id", ksiazkaId);     
                    cmd2.Parameters.AddWithValue("@autor_id", author_id);
                    await cmd2.ExecuteNonQueryAsync();

                    _logger.Debug("Inserted data successfully!");
                }
                else
                {
                    _logger.Error("Failed to insert data into 'ksiazka' table.");
                }
            }
            else
            {
                _logger.Error("Invalid date format for rokWydania.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }
    public async Task AddAuthor(string imie, string nazwisko)
    {
        try
        {
            using var cmd = new NpgsqlCommand("INSERT INTO Autor (imie, nazwisko) VALUES (@imie, @nazwisko)", _dbConnection);
            cmd.Parameters.AddWithValue("@imie", imie);
            cmd.Parameters.AddWithValue("@nazwisko", nazwisko);
            await cmd.ExecuteNonQueryAsync();
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }
    public async Task AddPublisher(string nazwa, string adres)
    {
        try
        {
            using var cmd = new NpgsqlCommand("INSERT INTO Wydawnictwo (nazwa, adres) VALUES (@nazwa, @adres)", _dbConnection);
            cmd.Parameters.AddWithValue("@nazwa", nazwa);
            cmd.Parameters.AddWithValue("@adres", adres);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }
    public async Task AddGenre(string nazwa, int dziedzina_nadrzedna_id)
    {
        try
        {
            using var cmd = new NpgsqlCommand("INSERT INTO Dziedzina (nazwa, dziedzina_nadrzedna_id) VALUES (@nazwa, @dziedzina_nadrzedna_id)", _dbConnection);
            cmd.Parameters.AddWithValue("@nazwa", nazwa);
            cmd.Parameters.AddWithValue("@dziedzina_nadrzedna_id", dziedzina_nadrzedna_id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }
    public async Task AddCopyOfBook(int ksiazka_id, string isbn)
    {
        try
        {
            using var cmd = new NpgsqlCommand("INSERT INTO Egzemplarz (ksiazka_id, isbn) VALUES (@ksiazka_id, @isbn)", _dbConnection);
            cmd.Parameters.AddWithValue("@ksiazka_id", ksiazka_id);
            cmd.Parameters.AddWithValue("@isbn", isbn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }
    public async Task DodajCzytelnika(string imie, string nazwisko, string adres, string email, string telefon)
    {
        try
        {
                using var cmd = new NpgsqlCommand("INSERT INTO Czytelnik (imie, nazwisko, adres, email, telefon) VALUES (@imie, @nazwisko, @adres, @email, @telefon)", _dbConnection);
                cmd.Parameters.AddWithValue("@imie", imie);
                cmd.Parameters.AddWithValue("@nazwisko", nazwisko);
                cmd.Parameters.AddWithValue("@adres", adres);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@telefon", telefon);
                var result = await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    public async Task WypozyczEgzemplarzKsiazki(int czytelnik_id, int egzemplarz_id)
    {
        try
        {
            using var cmd = new NpgsqlCommand("INSERT INTO Wypozyczenie (czytelnik_id, egzemplarz_id, data_wypozyczenia) VALUES (@czytelnik_id, @egzemplarz_id, @data_wypozyczenia)", _dbConnection);
            cmd.Parameters.AddWithValue("@czytelnik_id", czytelnik_id);
            cmd.Parameters.AddWithValue("@egzemplarz_id", egzemplarz_id);
            cmd.Parameters.AddWithValue("@data_wypozyczenia", NpgsqlDbType.Timestamp, DateTime.Now);
            var result = await cmd.ExecuteNonQueryAsync();
        }
        catch(PostgresException ex)
        {
            if (ex.Message.StartsWith("23503"))
                MessageBox.Show("Brak dostępnych egzemplarzy");
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }

    public async Task DodajDateZwrotu(int wypozyczenie_id)
    {
        try
        {
            using var cmd = new NpgsqlCommand("UPDATE wypozyczenie SET data_zwrotu = @data_zwrotu WHERE wypozyczenie_id = @wypozyczenie_id", _dbConnection);
            cmd.Parameters.AddWithValue("@wypozyczenie_id", wypozyczenie_id);
            cmd.Parameters.AddWithValue("@data_zwrotu", NpgsqlDbType.Timestamp, DateTime.Now);
            await cmd.ExecuteNonQueryAsync();

        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }
}
