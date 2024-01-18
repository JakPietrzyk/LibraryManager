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
using System.IO;
using System.Reflection;

public class Database
{
    private const string _connectionString = DbConstants.ConnectionString;
    private NpgsqlConnection? _dbConnection;
    private static NLog.Logger _logger;
    public Database() 
    {
        _logger =  NLog.LogManager.GetCurrentClassLogger(); 

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
    public bool IsConnection()
    {
        if(_dbConnection is not null)
            if (_dbConnection.State == System.Data.ConnectionState.Open)
                return true;
        return false;
    }
    public async Task CreateDatabase()
    {
        try
        {
            await ExecuteSqlScript("sql/createDatabase.sql");
            await ExecuteSqlScript("sql/views.sql");
            await ExecuteSqlScript("sql/triggers.sql");

            _logger.Info("Database created successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating database: {ex.Message}");
        }
    }

    private async Task ExecuteSqlScript(string scriptPath)
    {
        try
        {
            string sqlScript = File.ReadAllText(scriptPath);
            using var cmd = new NpgsqlCommand(sqlScript, _dbConnection);
            cmd.ExecuteNonQuery();
            _logger.Info($"Script '{scriptPath}' executed successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error executing script '{scriptPath}': {ex.Message}");
            throw;
        }
    }
    public async Task<int> GetEgzemplarzKsiazki(int ksiazkaId)
    {
        int result = -1;
        try
        {
            string sql = @"
            SELECT DISTINCT e.egzemplarz_id
            FROM Egzemplarz e
            WHERE e.ksiazka_id = @ksiazka_id
            EXCEPT
            SELECT DISTINCT e.egzemplarz_id
            FROM Egzemplarz e
            JOIN Wypozyczenie w ON e.egzemplarz_id = w.egzemplarz_id
            WHERE e.ksiazka_id = @ksiazka_id AND data_zwrotu IS NULL
            LIMIT 1";


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
            string sql = "SELECT * FROM czytelnik ORDER BY nazwisko";


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
                            Telefon = reader["telefon"].ToString(),
                            PelneImieNazwisko = reader["imie"].ToString() + " " + reader["nazwisko"].ToString()
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
    async public Task<List<KsiazkaDto>> GetAllBooks(string? nazwa = null, string tytul = null)
    {
        List<KsiazkaDto> results = new List<KsiazkaDto>();
        try
        {
            string sql = "SELECT * FROM InformacjeOKsiazce ORDER BY tytul;";

            if (tytul != null && nazwa != null)
                sql = "WITH RECURSIVE Subcategories AS " +
                    "( SELECT dziedzina_id, nazwa " +
                    "FROM Dziedzina " +
                    "WHERE nazwa = @nazwa " +
                    "UNION " +
                    "SELECT d.dziedzina_id, d.nazwa " +
                    "FROM Dziedzina d " +
                    "JOIN Subcategories s " +
                    "ON d.dziedzina_nadrzedna_id = s.dziedzina_id ) " +
                    "SELECT DISTINCT k.ksiazka_id, k.tytul, iok.autorzy, k.rok_wydania, iok.wydawnictwo " +
                    "FROM Ksiazka k " +
                    "JOIN Subcategories s ON k.dziedzina_id = s.dziedzina_id " +
                    "JOIN InformacjeOKsiazce iok ON k.ksiazka_id = iok.ksiazka_id " +
                    "WHERE LOWER(k.tytul) LIKE LOWER(@tytul) " +
                    "GROUP BY k.ksiazka_id, k.tytul, iok.autorzy, k.rok_wydania, iok.wydawnictwo " +
                    "ORDER BY k.tytul";
            else if (nazwa != null)
                sql = "WITH RECURSIVE Subcategories AS " +
                    "( SELECT dziedzina_id, nazwa " +
                    "FROM Dziedzina " +
                    "WHERE nazwa = @nazwa " +
                    "UNION " +
                    "SELECT d.dziedzina_id, d.nazwa " +
                    "FROM Dziedzina d " +
                    "JOIN Subcategories s " +
                    "ON d.dziedzina_nadrzedna_id = s.dziedzina_id ) " +
                    "SELECT DISTINCT k.ksiazka_id, k.tytul, iok.autorzy, k.rok_wydania, iok.wydawnictwo " +
                    "FROM Ksiazka k " +
                    "JOIN Subcategories s ON k.dziedzina_id = s.dziedzina_id " +
                    "JOIN InformacjeOKsiazce iok ON k.ksiazka_id = iok.ksiazka_id " +
                    "GROUP BY k.ksiazka_id, k.tytul, iok.autorzy, k.rok_wydania, iok.wydawnictwo " +
                    "ORDER BY k.tytul;";
            else if (tytul != null)
                sql = "SELECT * FROM InformacjeOKsiazce WHERE LOWER(tytul) LIKE LOWER(@tytul) ORDER BY tytul";


            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                if (nazwa != null)
                    cmd.Parameters.AddWithValue("@nazwa", nazwa);
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
                            RokWydania =DateTime.Parse(reader["rok_wydania"].ToString()).Date,
                            Autor = new AutorDto
                            {
                                FullName = reader["autorzy"].ToString()
                            },
                            Wydawnictwo = reader["wydawnictwo"].ToString()
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
    async public Task<List<KsiazkaDto>> GetTopBooks()
    {
        List<KsiazkaDto> results = new List<KsiazkaDto>();
        try
        {
            string sql = "SELECT * FROM InformacjeOKsiazce ORDER BY srednia_ocen DESC LIMIT 3";


            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        results.Add(new KsiazkaDto
                        {
                            Tytul = reader["tytul"].ToString(),
                            RokWydania = DateTime.Parse(reader["rok_wydania"].ToString()).Date,
                            Autor = new AutorDto
                            {
                                FullName = reader["autorzy"].ToString()
                            },
                            Opinia = (decimal)reader["srednia_ocen"],
                            Wydawnictwo = reader["wydawnictwo"].ToString()
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
    async public Task<CzytelnikDto?> GetTopReader()
    {
        CzytelnikDto? result = null;
        try
        {
            string sql = "SELECT * FROM CzytelnikMiesiaca";


            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        result = new CzytelnikDto()
                        {
                            Imie = reader["imie"].ToString(),
                            Nazwisko = reader["nazwisko"].ToString(),
                            IloscWypozyczen = (long)reader["ilosc_wypozyczen"]
                        };
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
                "WHERE czytelnik_id = @czytelnik_id " +
                "ORDER BY data_wypozyczenia DESC";

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
            var sql = "SELECT autor_id, CONCAT(imie, ' ', nazwisko) AS author_name FROM Autor ORDER BY author_name";
            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        authors.Add(new AutorDto
                        {
                            Id = Convert.ToInt32(reader["autor_id"]),
                            FullName = reader["author_name"].ToString()
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
            var sql = "SELECT wydawnictwo_id, nazwa FROM Wydawnictwo ORDER BY nazwa";
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
    public async Task<string> GetGenresOfBook(int ksiazka_id)
    {
        try
        {
            var sql = "WITH RECURSIVE BookCategories AS ( " +
                "SELECT dziedzina_id, dziedzina_nadrzedna_id, nazwa " +
                "FROM Dziedzina " +
                "WHERE dziedzina_id IN (" +
                "SELECT dziedzina_id " +
                "FROM Ksiazka " +
                "WHERE ksiazka_id = @ksiazka_id) " +
                "UNION " +
                "SELECT d.dziedzina_id, d.dziedzina_nadrzedna_id, d.nazwa " +
                "FROM Dziedzina d " +
                "JOIN BookCategories bc ON d.dziedzina_id = bc.dziedzina_nadrzedna_id ) " +
                "SELECT STRING_AGG(nazwa, ', ') AS wszystkie_dziedziny " +
                "FROM BookCategories";
            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                cmd.Parameters.AddWithValue("@ksiazka_id", ksiazka_id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        return reader["wszystkie_dziedziny"].ToString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
        return "";
    }
    public async Task<List<DziedzinaDto>> GetGenresDistinct()
    {
        List<DziedzinaDto> genres = new List<DziedzinaDto>();
        try
        {
            var sql = "SELECT nazwa FROM Dziedzina GROUP BY nazwa ORDER BY nazwa";
            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        var genre = new DziedzinaDto
                        {
                            Nazwa = reader["nazwa"].ToString(),
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
    public async Task<double> GetRating(int ksiazka_id)
    {
        try
        {
            var sql = "SELECT AVG(opinia) AS srednia_ocen FROM Opinia_Czytelnika WHERE ksiazka_id = @ksiazka_id";
            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                cmd.Parameters.AddWithValue("@ksiazka_id", ksiazka_id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        if(reader["srednia_ocen"] is not DBNull)
                        {
                            var opinia = Convert.ToDouble(reader["srednia_ocen"]);
                            return opinia;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
        return -1;
    }
    async public Task AddBook(string tytul, string rok_wydania, List<int> authorIds, int publisher_id, int genre_id)
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
                    foreach (int author_id in authorIds)
                    {
                        using var cmd2 = new NpgsqlCommand("INSERT INTO ksiazka_autor VALUES(@ksiazka_id, @autor_id)", _dbConnection);
                        cmd2.Parameters.AddWithValue("@ksiazka_id", ksiazkaId);
                        cmd2.Parameters.AddWithValue("@autor_id", author_id);
                        await cmd2.ExecuteNonQueryAsync();
                    }

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
            if (ex.Message.StartsWith("P0001"))
                MessageBox.Show("Autor o podanym imieniu i nazwisku już istnieje");
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
    public async Task AddGenre(string nazwa, int? dziedzina_nadrzedna_id = null)
    {
        try
        {
            using var cmd = new NpgsqlCommand("INSERT INTO Dziedzina (nazwa, dziedzina_nadrzedna_id) VALUES (@nazwa, @dziedzina_nadrzedna_id)", _dbConnection);
            cmd.Parameters.AddWithValue("@nazwa", nazwa);
            if(dziedzina_nadrzedna_id > 0)
                cmd.Parameters.AddWithValue("@dziedzina_nadrzedna_id", dziedzina_nadrzedna_id);
            else
                cmd.Parameters.AddWithValue("@dziedzina_nadrzedna_id", DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }
    public async Task<List<int>> GetHierarchyIds(string genreName, int? parentId = null)
    {
        try
        {
            var hierarchyIds = new List<int>();
                string query = @"
                WITH RECURSIVE Subcategories AS (
                    SELECT dziedzina_id, dziedzina_nadrzedna_id
                    FROM Dziedzina
                    WHERE nazwa = @nazwa
                    UNION
                    SELECT d.dziedzina_id, d.dziedzina_nadrzedna_id
                    FROM Dziedzina d
                    JOIN Subcategories s ON d.dziedzina_nadrzedna_id = s.dziedzina_id
                )
                SELECT dziedzina_id
                FROM Subcategories";
                if (parentId != null)
                    query += " WHERE dziedzina_nadrzedna_id = @dziedzina_nadrzedna_id";
                using (var cmd = new NpgsqlCommand(query, _dbConnection))
                {
                    cmd.Parameters.AddWithValue("@nazwa", genreName);
                    if (parentId != null)
                        cmd.Parameters.AddWithValue("@dziedzina_nadrzedna_id", parentId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                hierarchyIds.Add(reader.GetInt32(0));
                            }
                        }
                    }
                }
            

            return hierarchyIds;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error getting hierarchy IDs: " + ex.Message);
            return null;
        }
    }
    public async Task<int?> GetExistingHierarchyId(List<string> genreNames)
    {
        List<int> singleNameParents = new List<int>();
        foreach (var genreName in genreNames)
        {
            try
            {
                if(singleNameParents.Count > 0 )
                {
                    List<int> parentsToAdd = new List<int>();
                    foreach (var parentId in singleNameParents)
                    {
                        parentsToAdd.AddRange(await GetHierarchyIds(genreName, parentId));
                    }
                    singleNameParents.Clear();
                    singleNameParents.AddRange(parentsToAdd);
                    if (singleNameParents.Count == 0)
                        break;
                }
                else
                {
                    singleNameParents = await GetHierarchyIds(genreName);
                }
            }
            catch(Exception ex)
            {

            }
        }
        if(singleNameParents.Count == 1)
        {
            return singleNameParents.First();
        }
        else
        {
            return null;
        }
    }





    public async Task<int> AddGenreWithSubTypes(List<string> genreNames)
    {
        int? lastInsertedId = null;

        try
        {
            foreach (var genreName in genreNames)
            {
                lastInsertedId = await AddSingleGenre(lastInsertedId, genreName);
            }

            return lastInsertedId ?? 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return 0;
        }
    }

    public async Task<int?> AddSingleGenre(int? dziedzina_nadrzedna_id, string genreName)
    {
        try
        {
            using var cmd = new NpgsqlCommand("INSERT INTO Dziedzina (nazwa, dziedzina_nadrzedna_id) VALUES (@nazwa, @dziedzina_nadrzedna_id) RETURNING dziedzina_id", _dbConnection);
            cmd.Parameters.AddWithValue("@nazwa", genreName);

            if (dziedzina_nadrzedna_id != null)
            {
                cmd.Parameters.AddWithValue("@dziedzina_nadrzedna_id", dziedzina_nadrzedna_id);
            }
            else
            {
                cmd.Parameters.AddWithValue("@dziedzina_nadrzedna_id", DBNull.Value);
            }

            var lastId = await cmd.ExecuteScalarAsync();
            return lastId as int?;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return null;
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
            throw ex;
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
    public async Task DodajOcene(int wypozyczenie_id, int czytelnik_id, int opinia)
    {
        try
        {
            int ksiazka_id = -1;
            var sql = "SELECT ksiazka_id " +
                "FROM Ksiazka k " +
                "WHERE k.ksiazka_id IN ( " +
                "SELECT ksiazka_id " +
                "FROM Egzemplarz e " +
                "WHERE e.egzemplarz_id IN ( " +
                "SELECT egzemplarz_id " +
                "FROM Wypozyczenie w " +
                "WHERE w.wypozyczenie_id = @wypozyczenie_id ) )";
            using (var cmdSelect = new NpgsqlCommand(sql, _dbConnection))
            {
                cmdSelect.Parameters.AddWithValue("@wypozyczenie_id", wypozyczenie_id);
                using (var reader = await cmdSelect.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        ksiazka_id = (int)reader["ksiazka_id"];
                    }
                }
            }
            if (ksiazka_id < 0)
                throw new NullReferenceException();
            using var cmd = new NpgsqlCommand("INSERT INTO Opinia_Czytelnika (czytelnik_id, ksiazka_id, opinia) VALUES( @czytelnik_id, @ksiazka_id, @opinia);", _dbConnection);
            cmd.Parameters.AddWithValue("@czytelnik_id", czytelnik_id);
            cmd.Parameters.AddWithValue("@ksiazka_id", ksiazka_id);
            cmd.Parameters.AddWithValue("@opinia", opinia);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }
    }
    
}
