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
    async public Task<List<string>> GetAuthors()
    {
        List<string> authors = new List<string>();
        try
        {
            var sql = "SELECT CONCAT(imie, ' ', nazwisko) AS author_name FROM Autor";
            using (var cmd = new NpgsqlCommand(sql, _dbConnection))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        authors.Add(reader["author_name"].ToString());
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

    async public Task AddBook(string tytul, string rok_wydania, string author, string publisher, string genre)
    {
        try
        {
            if (DateTime.TryParse(rok_wydania, out DateTime parsedDate))
            {
                using var cmd = new NpgsqlCommand("INSERT INTO ksiazka (tytul, rok_wydania) VALUES (@tytul, @rok_wydania)", _dbConnection);
                
                cmd.Parameters.AddWithValue("@tytul", tytul);
                cmd.Parameters.AddWithValue("@rok_wydania", NpgsqlDbType.Date, parsedDate);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"Inserted {rowsAffected} row(s)");
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
