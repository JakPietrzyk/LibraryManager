-- zwraca dostępny egzemplarz książki
SELECT DISTINCT e.egzemplarz_id
FROM Egzemplarz e
WHERE e.ksiazka_id = @ksiazka_id
EXCEPT
SELECT DISTINCT e.egzemplarz_id
FROM Egzemplarz e
JOIN Wypozyczenie w ON e.egzemplarz_id = w.egzemplarz_id
WHERE e.ksiazka_id = @ksiazka_id AND data_zwrotu IS NULL
LIMIT 1;

-- wyszukiwanie wszystkich książek dla podanego słowa kluczowego i wybranej dziedziny  
WITH RECURSIVE Subcategories AS (
    SELECT dziedzina_id, nazwa
    FROM Dziedzina
    WHERE nazwa = @nazwa
    UNION
    SELECT d.dziedzina_id, d.nazwa
    FROM Dziedzina d
    JOIN Subcategories s ON d.dziedzina_nadrzedna_id = s.dziedzina_id
)
SELECT DISTINCT
    k.ksiazka_id,
    k.tytul,
    iok.autorzy,
    k.rok_wydania,
    iok.wydawnictwo
FROM
    Ksiazka k
    JOIN Subcategories s ON k.dziedzina_id = s.dziedzina_id
    JOIN InformacjeOKsiazce iok ON k.ksiazka_id = iok.ksiazka_id
WHERE
    LOWER(k.tytul) LIKE LOWER(@tytul)
GROUP BY
    k.ksiazka_id,
    k.tytul,
    iok.autorzy,
    k.rok_wydania,
    iok.wydawnictwo
ORDER BY
    k.tytul;

-- pobiera wszystkie dziedziny dla danej id książki i łączy je w jednen string wszystkie_dziedziny
WITH RECURSIVE BookCategories AS (
    SELECT dziedzina_id, dziedzina_nadrzedna_id, nazwa
    FROM Dziedzina
    WHERE dziedzina_id IN (
        SELECT dziedzina_id
        FROM Ksiazka
        WHERE ksiazka_id = @ksiazka_id
    )
    UNION
    SELECT d.dziedzina_id, d.dziedzina_nadrzedna_id, d.nazwa
    FROM Dziedzina d
    JOIN BookCategories bc ON d.dziedzina_id = bc.dziedzina_nadrzedna_id
)
SELECT STRING_AGG(nazwa, ', ') AS wszystkie_dziedziny
FROM BookCategories;

-- zwraca średnią ocen książki
SELECT AVG(opinia) AS srednia_ocen
FROM Opinia_Czytelnika 
WHERE ksiazka_id = @ksiazka_id;

-- zwraca wszystkie id dziedzin, które są związane z podaną dziedziną na różnych poziomach hierarchii
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
FROM Subcategories;
