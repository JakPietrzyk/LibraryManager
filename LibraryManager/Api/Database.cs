using System;
using Npgsql;
using System.Collections.Generic;
using System.Configuration;
using projekt.Constants;
using System.Threading.Tasks;
using NpgsqlTypes;
using System.Linq;
using LibraryManager.Dtos;

public class Database
{
    private const string _connectionString = DbConstants.ConnectionString;
    private NpgsqlConnection? _dbConnection;
    public Database() 
    {
        try 
        {
            _dbConnection = new NpgsqlConnection(_connectionString);
            _dbConnection.Open();
            Console.WriteLine("Connected to PostgreSQL");
        }
        catch (NpgsqlException ex) 
        { 
            Console.WriteLine(ex.Message);
        }
    }
    async public Task<List<KsiazkaDto>> GetAllBooks()
    {
        List<KsiazkaDto> results = new();
            try
            {
                var sql = "SELECT * FROM ksiazka";
                using (var cmd = new NpgsqlCommand(sql, _dbConnection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            string data = $"{reader["tytul"]} - {reader["rok_wydania"]}";
                            results.Add(new KsiazkaDto { 
                                Tytul = reader["tytul"].ToString(), 
                                RokWydania = reader["rok_wydania"].ToString() 
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        return results;
    }
    async public Task<List<RentalDto>> GetAllRentals()
    {
        List<RentalDto> results = new List<RentalDto>();
        try
        {
            var sql =
                "SELECT nazwisko, imie, tytul, data_wypozyczenia, data_zwrotu, rok_wydania " +
                "FROM wypozyczenie w " +
                "JOIN egzemplarz e USING (egzemplarz_id) " +
                "JOIN ksiazka k USING (ksiazka_id) " +
                "JOIN czytelnik c USING (czytelnik_id);";

            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        RentalDto rental = new RentalDto
                        {
                            Nazwisko = reader["nazwisko"].ToString(),
                            Imie = reader["imie"].ToString(),
                            Tytul = reader["tytul"].ToString(),
                            DataWypozyczenia = Convert.ToDateTime(reader["data_wypozyczenia"]),
                            DataZwrotu = Convert.ToDateTime(reader["data_zwrotu"]),
                            RokWydania = Convert.ToDateTime(reader["rok_wydania"]).Date
                        };
                        results.Add(rental);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
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
            Console.WriteLine(ex.Message);
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
            Console.WriteLine(ex.Message);
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
            Console.WriteLine(ex.Message);
        }
        return genres;
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

                    Console.WriteLine("Inserted data successfully!");
                }
                else
                {
                    Console.WriteLine("Failed to insert data into 'ksiazka' table.");
                }
            }
            else
            {
                Console.WriteLine("Invalid date format for rokWydania.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

}
