DROP TRIGGER IF EXISTS AktualizacjaDostepnosciKsiazki ON Wypozyczenie;
DROP FUNCTION IF EXISTS ZwrotKsiazkiUstawDostepnosc;
DROP TRIGGER IF EXISTS AktualizacjaDostepnosciKsiazkiPoWypozyczeniu ON Wypozyczenie;
DROP FUNCTION IF EXISTS SprawdzDostepnoscKsiazki;
DROP TRIGGER IF EXISTS AktualizacjaDostepnosciKsiazkiPoDodaniuEgzemplarza ON Egzemplarz;
DROP FUNCTION IF EXISTS DodanieEgzemplarzaUstawDostepnosc;
DROP TRIGGER IF EXISTS AktualizacjaDostepnosciKsiazkiPoJejDodaniu ON Ksiazka;
DROP FUNCTION IF EXISTS DodanieKsiazkiUstawDostepnosc;
-- trigger wywoływany po zwróceniu ksiązki -> ustawia dostępność książki na TRUE
CREATE OR REPLACE FUNCTION ZwrotKsiazkiUstawDostepnosc() RETURNS TRIGGER AS $$
BEGIN
    UPDATE Ksiazka
    SET dostepnosc = TRUE
    WHERE ksiazka_id IN (
        SELECT ksiazka_id 
        FROM Ksiazka k 
        WHERE k.ksiazka_id IN (
            SELECT ksiazka_id 
            FROM Egzemplarz e 
            WHERE e.egzemplarz_id IN (
                SELECT egzemplarz_id 
                FROM Wypozyczenie w 
                WHERE w.wypozyczenie_id = NEW.wypozyczenie_id
            )
        )
    );
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER AktualizacjaDostepnosciKsiazki
AFTER UPDATE ON Wypozyczenie
FOR EACH ROW
EXECUTE FUNCTION ZwrotKsiazkiUstawDostepnosc();



-- trigger wywoywany po wypożyczeniu książki -> sprawdza ilość dostępnych egzemplarzy 
-- i ustawia dostępność na TRUE lub FALSE
CREATE OR REPLACE FUNCTION SprawdzDostepnoscKsiazki() RETURNS TRIGGER AS $$
BEGIN
    IF NEW.data_zwrotu IS NULL THEN
        IF NOT EXISTS (
            SELECT 1
            FROM Egzemplarz e
            WHERE e.ksiazka_id = (
                SELECT ksiazka_id
                FROM Egzemplarz
                WHERE egzemplarz_id = NEW.egzemplarz_id
            ) AND NOT EXISTS (
                SELECT *
                FROM Wypozyczenie
                WHERE egzemplarz_id = e.egzemplarz_id AND data_zwrotu IS NULL
            )
        ) THEN
            UPDATE Ksiazka
            SET dostepnosc = FALSE
            WHERE ksiazka_id = (
                SELECT ksiazka_id FROM Egzemplarz
                WHERE egzemplarz_id = NEW.egzemplarz_id
            );
        END IF;
    ELSE
        UPDATE Ksiazka
        SET dostepnosc = TRUE
        WHERE ksiazka_id = (
            SELECT ksiazka_id FROM Egzemplarz
            WHERE egzemplarz_id = NEW.egzemplarz_id
        );
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER AktualizacjaDostepnosciKsiazkiPoWypozyczeniu
AFTER INSERT ON Wypozyczenie
FOR EACH ROW
EXECUTE FUNCTION SprawdzDostepnoscKsiazki();


-- trigger wywoływany po dodaniu egzemplarza -> ustawia dosepność książki na TRUE
CREATE OR REPLACE FUNCTION DodanieEgzemplarzaUstawDostepnosc()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE Ksiazka
    SET dostepnosc = TRUE
    WHERE ksiazka_id = NEW.ksiazka_id;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER AktualizacjaDostepnosciKsiazkiPoDodaniuEgzemplarza
AFTER INSERT ON Egzemplarz
FOR EACH ROW
EXECUTE FUNCTION DodanieEgzemplarzaUstawDostepnosc();


-- trigger wywoływany po dodaniu nowej książki -> ustawia dostępność książki na FALSE
CREATE OR REPLACE FUNCTION DodanieKsiazkiUstawDostepnosc()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE Ksiazka
    SET dostepnosc = FALSE
    WHERE ksiazka_id = NEW.ksiazka_id;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER AktualizacjaDostepnosciKsiazkiPoJejDodaniu
AFTER INSERT ON Ksiazka
FOR EACH ROW
EXECUTE FUNCTION DodanieKsiazkiUstawDostepnosc();