-- widok prezentujący najważniejsze informacje o czytelniku w aktualnym miesiącu
CREATE OR REPLACE VIEW CzytelnikMiesiaca AS
SELECT 
    c.czytelnik_id, 
    c.imie, 
    c.nazwisko, 
    COUNT(W.wypozyczenie_id) AS ilosc_wypozyczen
FROM Czytelnik c
JOIN Wypozyczenie W ON c.czytelnik_id = W.czytelnik_id
WHERE EXTRACT(MONTH FROM W.data_wypozyczenia) = EXTRACT(MONTH FROM CURRENT_DATE)
GROUP BY c.czytelnik_id, c.imie, c.nazwisko
ORDER BY ilosc_wypozyczen DESC
LIMIT 1;


-- widok prezentujący najważniejsze informacje o książce 
CREATE OR REPLACE VIEW InformacjeOKsiazce AS
SELECT 
    k.ksiazka_id,
    k.tytul,
    STRING_AGG(DISTINCT CONCAT(A.imie, ' ', A.nazwisko), ', ') AS "autorzy",
    k.rok_wydania,
    Wy.nazwa AS "wydawnictwo",
    COALESCE(AVG(OC.opinia), 0) AS "srednia_ocen"
FROM 
    Ksiazka k
JOIN 
    Ksiazka_Autor ka ON k.ksiazka_id = ka.ksiazka_id
JOIN 
    Autor a ON ka.autor_id = a.autor_id
LEFT JOIN 
    Wydawnictwo wy ON k.wydawnictwo_id = wy.wydawnictwo_id
LEFT JOIN 
    Opinia_Czytelnika oc ON k.ksiazka_id = oc.ksiazka_id
GROUP BY 
    k.ksiazka_id, k.tytul, k.rok_wydania, wy.nazwa;