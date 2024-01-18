drop schema projekt_bazy cascade;
create schema projekt_bazy;

set search_path to projekt_bazy;
CREATE TABLE Wydawnictwo (
    wydawnictwo_id SERIAL,
    nazwa VARCHAR(100),
    adres VARCHAR(255),
    CONSTRAINT wydawnictwo_pk PRIMARY KEY (wydawnictwo_id)
);

CREATE TABLE Dziedzina (
    dziedzina_id SERIAL,
    nazwa VARCHAR(100),
    dziedzina_nadrzedna_id INT,
    CONSTRAINT dziedzina_pk PRIMARY KEY (dziedzina_id),
    CONSTRAINT dziedzina_fk FOREIGN KEY (dziedzina_nadrzedna_id) REFERENCES Dziedzina(dziedzina_id)
);

CREATE TABLE Ksiazka (
    ksiazka_id SERIAL,
    tytul VARCHAR(255),
    rok_wydania date,
    wydawnictwo_id INT,
    dziedzina_id INT,
    dostepnosc BOOLEAN,
    CONSTRAINT ksiazka_pk PRIMARY KEY (ksiazka_id),
    CONSTRAINT dziedzina_fk FOREIGN KEY (dziedzina_id) REFERENCES Dziedzina(dziedzina_id),
    CONSTRAINT wydawnictwo_fk FOREIGN KEY (wydawnictwo_id) REFERENCES Wydawnictwo(wydawnictwo_id)
);

CREATE TABLE Autor (
    autor_id SERIAL,
    imie VARCHAR(50),
    nazwisko VARCHAR(50),
    CONSTRAINT autor_pk PRIMARY KEY (autor_id)
);

CREATE TABLE Ksiazka_Autor (
    ksiazka_id INT,
    autor_id INT,
    CONSTRAINT ksiazka_fk FOREIGN KEY (ksiazka_id) REFERENCES Ksiazka(ksiazka_id),
    CONSTRAINT autor_fk FOREIGN KEY (autor_id) REFERENCES Autor(autor_id)
);

CREATE TABLE Czytelnik (
    czytelnik_id SERIAL,
    imie VARCHAR(50),
    nazwisko VARCHAR(50),
    adres VARCHAR(255),
    email VARCHAR(100),
    telefon VARCHAR(20),
    CONSTRAINT czytelnik_pk PRIMARY KEY (czytelnik_id)
);

CREATE TABLE Egzemplarz (
    egzemplarz_id SERIAL,
    ksiazka_id INT,
    isbn VARCHAR(17),
    CONSTRAINT egzemplarz_pk PRIMARY KEY (egzemplarz_id),
    CONSTRAINT ksiazka_fk FOREIGN KEY (ksiazka_id) REFERENCES Ksiazka(ksiazka_id)
);

CREATE TABLE Wypozyczenie (
    wypozyczenie_id SERIAL,
    czytelnik_id INT,
    egzemplarz_id INT,
    data_wypozyczenia TIMESTAMP,
    data_zwrotu TIMESTAMP,
    CONSTRAINT wypozyczenie_pk PRIMARY KEY (wypozyczenie_id),
    CONSTRAINT czytelnik_fk FOREIGN KEY (czytelnik_id) REFERENCES Czytelnik(czytelnik_id),
    CONSTRAINT egzemplarz_fk FOREIGN KEY (egzemplarz_id) REFERENCES Egzemplarz(egzemplarz_id)
);


CREATE TABLE Opinia_Czytelnika (
    opinia_id SERIAL,
    czytelnik_id INT,
    ksiazka_id INT,
    opinia INT CHECK (opinia >= 0 AND opinia <= 5),
    CONSTRAINT opinia_pk PRIMARY KEY (opinia_id),
    CONSTRAINT czytelnik_fk FOREIGN KEY (czytelnik_id) REFERENCES Czytelnik(czytelnik_id),
    CONSTRAINT ksiazka_fk FOREIGN KEY (ksiazka_id) REFERENCES Ksiazka(ksiazka_id)
);

INSERT INTO Wydawnictwo (nazwa, adres) VALUES 
('Wydawnictwo ABC', 'ul. Sloneczna 8, Gdansk'),
('Nowoczesne Wydawnictwo', 'al. Piastowska 15, Wroclaw'),
('Czytaj Zawsze', 'ul. Morska 3, Szczecin'),
('Ksiazkowe Marzenia', 'pl. Kwiatowy 7, Lodz'),
('Edukacyjne Wydawnictwo', 'ul. Naukowa 21, Poznan');

INSERT INTO Dziedzina (nazwa, dziedzina_nadrzedna_id) VALUES 
('Horror', NULL), --1
('Komedia', NULL), --2
('Dramat', NULL), --3
('Romans', 4), --4
('Science Fiction', NULL), --5
('Kryminal', 4), --6

('Komedia', 3), --7 
('Komedio-Dramat', 7), -- 8
('Thriller', 6), -- 9
('Thriller Szpiegowski', 9),--10
('Dramat', 2); --11


INSERT INTO Ksiazka (tytul, rok_wydania, wydawnictwo_id, dziedzina_id, dostepnosc) VALUES 
('Mroczne Zakatki', '2005-01-01', 4, 10, TRUE),
('Klub Walki', '1999-01-01', 3, 9, TRUE),
('Granica Piekla', '2009-01-01', 5, 8, TRUE),
('Romeo i Julia XXI wieku', '2003-01-01', 4, 8, TRUE),
('Milosc i Intryga', '2012-01-01', 2, 4, TRUE),
('Bitwa o Galaktyke', '2018-01-01', 5, 5, TRUE),
('Morderstwo w Orient Expressie', '2001-01-01', 3, 10, TRUE),
('Wladca Pierscieni: Druzyna Pierscienia', '2002-01-01', 1, 5, TRUE),
('Zaginiony Symbol', '2009-01-01', 5, 3, TRUE),
('Drakula', '2015-01-01', 2, 1, TRUE);

INSERT INTO Autor (imie, nazwisko) VALUES 
('Stephen', 'King'),
('Chuck', 'Palahniuk'),
('Paula', 'Hawkins'),
('William', 'Shakespeare'),
('Jane', 'Austen'),
('Isaac', 'Asimov'),
('Agatha', 'Christie'),
('J.R.R.', 'Tolkien'),
('Dan', 'Brown'),
('Bram', 'Stoker');

INSERT INTO Ksiazka_Autor (ksiazka_id, autor_id) VALUES 
(1, 1),
(2, 2),
(3, 3),
(4, 4),
(5, 5),
(6, 6),
(7, 7),
(8, 8),
(9, 9),
(10, 10);

INSERT INTO Czytelnik (imie, nazwisko, adres, email, telefon) VALUES 
('Anna', 'Nowak', 'ul. Kwiatowa 2, Warszawa', 'anna@mail.com', '987654321'),
('Piotr', 'Duda', 'ul. Lesna 15, Krakow', 'piotr@mail.com', '456789012'),
('Karolina', 'Kwiatkowska', 'al. Piastowska 6, Wroclaw', 'karolina@mail.com', '345678901'),
('Tomasz', 'Jablonski', 'ul. Sloneczna 10, Gdansk', 'tomasz@mail.com', '234567890'),
('Magdalena', 'Kowalczyk', 'pl. Kwiatowy 3, Lodz', 'magda@mail.com', '1234567890');

INSERT INTO Egzemplarz (ksiazka_id, isbn) VALUES 
(3, '978-3-16-148410-1'),
(4, '132-4-21-132111-3'),
(5, '978-1-23-456789-0'),
(6, '978-0-14-030169-5'),
(7, '978-1-43-959663-0'),
(8, '978-0-06-112008-0'),
(9, '978-0-06-112007-3'),
(10, '978-3-16-148410-2'),
(1, '978-3-16-148410-3'),
(2, '978-1-23-456789-1');

INSERT INTO Wypozyczenie (czytelnik_id, egzemplarz_id, data_wypozyczenia, data_zwrotu) VALUES 
(2, 1, '2023-02-15 14:30:00', '2023-03-10 14:00:00'),
(3, 2, '2023-03-20 10:00:00', '2023-04-05 13:00:00'),
(4, 3, '2023-04-01 12:45:00', '2023-04-20 15:00:00'),
(5, 4, '2023-05-10 09:15:00', '2023-05-30 11:00:00'),
(1, 5, '2023-06-05 17:00:00', '2023-06-25 12:00:00'),
(2, 6, '2023-07-15 11:30:00', '2023-08-05 16:00:00'),
(3, 7, '2023-08-20 14:00:00', '2023-09-10 19:00:00'),
(4, 8, '2023-09-01 10:45:00', '2023-09-20 08:30:00'),
(5, 9, '2024-01-03 09:30:00', '2024-01-03 10:30:00'),
(1, 10, '2024-01-05 16:15:00', '2024-01-15 07:30:00');

INSERT INTO Opinia_Czytelnika (czytelnik_id, ksiazka_id, opinia) VALUES 
(2, 1, 3),
(3, 2, 2),
(4, 3, 5),
(5, 4, 1),
(1, 5, 3),
(2, 6, 2),
(3, 7, 1),
(4, 8, 3),
(5, 9, 4),
(1, 10, 3);