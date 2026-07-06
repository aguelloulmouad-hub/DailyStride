CREATE DATABASE GestionTachesDB
USE GestionTachesDB

--Creation des tables
CREATE TABLE Utilisateur (
    Id_utilisateur INT IDENTITY(1,1) PRIMARY KEY,
    nom VARCHAR(255),
    prenom VARCHAR(255),
    date_naissance DATE,
    email VARCHAR(255) UNIQUE,
    mot_de_passe VARCHAR(255)
)

CREATE TABLE Objectif (
    Id_objectif INT IDENTITY(1,1) PRIMARY KEY,
    titre VARCHAR(255),
    description TEXT,
    date_debut DATE,
    date_fin DATE,
    statut VARCHAR(50) NOT NULL CHECK (statut IN ('Non commencé', 'En cours', 'Terminé')),
    Id_utilisateur INT,
    FOREIGN KEY (Id_utilisateur) REFERENCES Utilisateur(Id_utilisateur)
)

CREATE TABLE Tache (
    Id_tache INT IDENTITY(1,1) PRIMARY KEY,
    titre VARCHAR(255),
    description TEXT,
    date_debut DATE,
    date_limite DATE,
    statut VARCHAR(50) CHECK (statut IN ('Non commencé', 'En cours', 'Terminé', 'Abandonné')),
    Priorite VARCHAR(50) CHECK (Priorite IN ('Haute', 'Moyenne', 'Basse')),
    Id_utilisateur INT,
    Id_objectif INT,
    FOREIGN KEY (Id_utilisateur) REFERENCES Utilisateur(Id_utilisateur),
    FOREIGN KEY (Id_objectif) REFERENCES Objectif(Id_objectif)
)

CREATE TABLE Notification (
    Id_notification INT IDENTITY(1,1) PRIMARY KEY,
    date_envoie DATETIME,
    type VARCHAR(50),
    Id_utilisateur INT,
    Id_objectif INT,
    Id_tache INT,
    message TEXT,
	affichage BIT DEFAULT 0,
    FOREIGN KEY (Id_utilisateur) REFERENCES Utilisateur(Id_utilisateur),
    FOREIGN KEY (Id_objectif) REFERENCES Objectif(Id_objectif),
    FOREIGN KEY (Id_tache) REFERENCES Tache(Id_tache)
)
GO

--Login
CREATE PROCEDURE VerifierUtilisateur (@Email NVARCHAR(50), @MotDePasse VARCHAR(50), @UtilisateurExiste BIT OUTPUT)
AS
SET @UtilisateurExiste = 0
IF EXISTS (
	SELECT 1 FROM GestionTachesDB.dbo.Utilisateur
	WHERE email = @Email AND mot_de_passe = @MotDePasse
)
BEGIN
	SET @UtilisateurExiste = 1
END
GO

--Isrciption
CREATE PROCEDURE InsererUtilisateur (@Nom VARCHAR(50), @Prenom VARCHAR(50), @DateNaissance DATE, @Email VARCHAR(50), @MotDePasse NVARCHAR(50))
AS
INSERT INTO GestionTachesDB.dbo.Utilisateur (nom, prenom, date_naissance, email, mot_de_passe)
VALUES (@Nom, @Prenom, @DateNaissance, @Email, @MotDePasse)
GO

CREATE PROCEDURE AjouterNotificationObjectif (@Id_utilisateur INT, @Id_objectif INT, @Titre NVARCHAR(255), @Statut VARCHAR(50), @Date_fin DATE)
AS
DECLARE @Message VARCHAR(MAX)
IF @Statut = 'Non commencé'
    SET @Message = 'Un nouvel objectif a été créé : "' + @Titre + '".'
ELSE IF @Statut = 'En cours'
    SET @Message = 'L''objectif "' + @Titre + '" est maintenant en cours.'
ELSE IF @Statut = 'Terminé'
    SET @Message = 'Félicitations ! L''objectif "' + @Titre + '" a été terminé.'
ELSE
    SET @Message = 'L''objectif "' + @Titre + '" a un statut inconnu.'
INSERT INTO GestionTachesDB.dbo.Notification (date_envoie, type, message, Id_utilisateur, Id_objectif, affichage)
VALUES (GETDATE(), 'Objectif', @Message, @Id_utilisateur, @Id_objectif, 0)
GO

--Notification
CREATE PROCEDURE AjouterNotificationTache (@Id_utilisateur INT, @Id_tache INT, @Id_objectif INT, @Titre NVARCHAR(255), @Statut NVARCHAR(50), @Date_limite DATE)
AS
DECLARE @Message VARCHAR(MAX)
IF @Statut = 'Non commencé'
    SET @Message = 'Une nouvelle tâche a été ajoutée : "' + @Titre + '".'
ELSE IF @Statut = 'En cours'
    SET @Message = 'La tâche "' + @Titre + '" est maintenant en cours.'
ELSE IF @Statut = 'Terminé'
    SET @Message = 'La tâche "' + @Titre + '" a été complétée avec succès.'
ELSE IF @Statut = 'Abandonné'
    SET @Message = 'La tâche "' + @Titre + '" a été abandonnée.'
ELSE
    SET @Message = 'La tâche "' + @Titre + '" a un statut inconnu.'
INSERT INTO GestionTachesDB.dbo.Notification (date_envoie, type, message, Id_utilisateur, Id_objectif, Id_tache, affichage)
VALUES (GETDATE(), 'Tâche', @Message, @Id_utilisateur, @Id_objectif, @Id_tache, 0)
GO

CREATE PROCEDURE GererNotificationsObjectif (@Id_objectif INT, @NouveauStatut VARCHAR(50), @titre VARCHAR(50), @Id_utilisateur INT)
AS
DELETE FROM GestionTachesDB.dbo.Notification
WHERE Id_objectif = @Id_objectif
DECLARE @Message VARCHAR(MAX)
IF @NouveauStatut = 'Non commencé'
    SET @Message = 'Objectif : ' + @titre + ' pas encore commencé.'
ELSE IF @NouveauStatut = 'En cours'
    SET @Message = 'Objectif : ' + @titre + ' est maintenant en cours.'
ELSE IF @NouveauStatut = 'Terminé'
    SET @Message = 'Félicitations ! Vous avez terminé objectif : ' + @titre + '.'
INSERT INTO Notification (date_envoie, type, message, Id_utilisateur, Id_objectif, affichage)
VALUES (GETDATE(), 'Statut', @Message, @Id_utilisateur, @Id_objectif, 0)
GO


CREATE PROCEDURE VerifierEtNotifierObjectifsNonTermines (@Id_utilisateur INT)
AS
DECLARE @Id_objectif INT, @Titre NVARCHAR(255), @Date_fin DATE, @Statut NVARCHAR(50), @Message NVARCHAR(MAX)
DECLARE ObjectifsCursor CURSOR FOR
SELECT 
    Id_objectif, titre, date_fin, statut
FROM GestionTachesDB.dbo.Objectif
WHERE Id_utilisateur = @Id_utilisateur
    AND statut IN ('Non commencé', 'En cours')
    AND (date_fin <= GETDATE() OR DATEADD(DAY, -1, date_fin) = CAST(GETDATE() AS DATE))
OPEN ObjectifsCursor
FETCH NEXT FROM ObjectifsCursor INTO @Id_objectif, @Titre, @Date_fin, @Statut
WHILE @@FETCH_STATUS = 0
BEGIN
    IF DATEADD(DAY, -1, @Date_fin) = CAST(GETDATE() AS DATE)
        SET @Message = 'Rappel : Objectif "' + @Titre + '" approche de sa date limite (' + FORMAT(@Date_fin, 'yyyy-MM-dd') + '). Veuillez le compléter avant demain.'
    ELSE IF @Date_fin < GETDATE()
        SET @Message = 'Objectif "' + @Titre + '" est en retard. Date limite dépassée : ' + FORMAT(@Date_fin, 'yyyy-MM-dd') + '.'
    INSERT INTO Notification (date_envoie, type, message, Id_utilisateur, Id_objectif, affichage)
    VALUES (GETDATE(), 'Rappel', @Message, @Id_utilisateur, @Id_objectif, 0)
    FETCH NEXT FROM ObjectifsCursor INTO @Id_objectif, @Titre, @Date_fin, @Statut
END
CLOSE ObjectifsCursor
DEALLOCATE ObjectifsCursor
GO

CREATE PROCEDURE GererNotificationsTache (@Id_tache INT, @Statut VARCHAR(50), @Titre VARCHAR(50), @Date_limite DATE, @Id_utilisateur INT, @Id_objectif INT)
AS
DELETE FROM Notification
WHERE Id_tache = @Id_tache
DECLARE @Message VARCHAR(MAX)
IF @Statut = 'Non commencé'
    SET @Message = 'Une nouvelle tâche a été ajoutée : "' + @Titre + '".'
ELSE IF @Statut = 'En cours'
    SET @Message = 'La tâche "' + @Titre + '" est maintenant en cours.'
ELSE IF @Statut = 'Terminé'
    SET @Message = 'La tâche "' + @Titre + '" a été complétée avec succès.'
ELSE IF @Statut = 'Abandonné'
    SET @Message = 'La tâche "' + @Titre + '" a été abandonnée.'
ELSE
    SET @Message = 'La tâche "' + @Titre + '" a un statut inconnu.'
INSERT INTO Notification (date_envoie, type, message, Id_utilisateur, Id_objectif, Id_tache, affichage)
VALUES (GETDATE(), 'Tâche', @Message, @Id_utilisateur, @Id_objectif, @Id_tache, 0);
GO

CREATE PROCEDURE VerifierEtNotifierTachesNonTerminees (@Id_utilisateur INT)
AS
DECLARE @Id_tache INT, @Titre VARCHAR(255), @Date_limite DATE, @Statut VARCHAR(50), @Message VARCHAR(MAX), @Id_objectif INT
DECLARE TachesCursor CURSOR FOR
SELECT 
    Id_tache, titre, date_limite, statut, Id_objectif
FROM GestionTachesDB.dbo.Tache
WHERE Id_utilisateur = @Id_utilisateur
    AND statut IN ('Non commencé', 'En cours')
    AND (date_limite <= GETDATE() OR DATEADD(DAY, -1, date_limite) = CAST(GETDATE() AS DATE))
OPEN TachesCursor
FETCH NEXT FROM TachesCursor INTO @Id_tache, @Titre, @Date_limite, @Statut, @Id_objectif
WHILE @@FETCH_STATUS = 0
BEGIN
    IF DATEADD(DAY, -1, @Date_limite) = CAST(GETDATE() AS DATE)
        SET @Message = 'Rappel : La tâche "' + @Titre + '" approche de sa date limite (' + FORMAT(@Date_limite, 'yyyy-MM-dd') + '). Veuillez la terminer avant demain.'
    ELSE IF @Date_limite < GETDATE()
        SET @Message = 'La tâche "' + @Titre + '" est en retard. Date limite dépassée : ' + FORMAT(@Date_limite, 'yyyy-MM-dd') + '.'
    INSERT INTO Notification (date_envoie, type, message, Id_utilisateur, Id_objectif, Id_tache, affichage)
    VALUES (GETDATE(), 'Rappel', @Message, @Id_utilisateur, @Id_objectif, @Id_tache, 0)
    FETCH NEXT FROM TachesCursor INTO @Id_tache, @Titre, @Date_limite, @Statut, @Id_objectif
END
CLOSE TachesCursor
DEALLOCATE TachesCursor
GO

CREATE PROCEDURE RecupererEtMarquerNotifications (@IdUtilisateur INT)
AS
BEGIN
    -- Temp table pour stocker les notifications à afficher
    CREATE TABLE #TempNotifications (
        type VARCHAR(50),
        message VARCHAR(MAX)
    )
    -- Insérer les notifications non affichées dans la table temporaire
    INSERT INTO #TempNotifications (type, message)
    SELECT type, message
    FROM GestionTachesDB.dbo.Notification
    WHERE Id_utilisateur = @IdUtilisateur
      AND CAST(date_envoie AS DATE) = CAST(GETDATE() AS DATE)
      AND affichage = 0; -- 'affichage' est une colonne qui marque les notifications déjà montrées
    -- Marquer ces notifications comme affichées
    UPDATE GestionTachesDB.dbo.Notification
    SET affichage = 1
	WHERE Id_utilisateur = @IdUtilisateur
      AND CAST(date_envoie AS DATE) = CAST(GETDATE() AS DATE)
      AND affichage = 0
    -- Retourner les notifications
    SELECT * FROM #TempNotifications

    DROP TABLE #TempNotifications
END

--Ajout
CREATE PROCEDURE AjouterTache (@Titre VARCHAR(255), @Description TEXT, @DateDebut DATE, @DateFin DATE, @Statut NVARCHAR(50), @Priorite NVARCHAR(50), @IdUtilisateur INT, @IdObjectif INT)
AS
INSERT INTO GestionTachesDB.dbo.Tache (titre, description, date_debut, date_limite, statut, Priorite, Id_utilisateur, Id_objectif)
VALUES (@Titre, @Description, @DateDebut, @DateFin, @Statut, @Priorite, @IdUtilisateur, @IdObjectif);
GO

CREATE PROCEDURE AjouterObjectif (@Titre VARCHAR(255), @Description TEXT, @DateDebut DATETIME, @DateFin DATETIME, @Statut VARCHAR(50), @Priorite VARCHAR(50), @IdUtilisateur INT)
AS
INSERT INTO GestionTachesDB.dbo.Objectif (titre, description, date_debut, date_fin, statut, Priorite, Id_utilisateur)
VALUES (@Titre, @Description, @DateDebut, @DateFin, @Statut, @Priorite, @IdUtilisateur)
GO

--Affichage
CREATE FUNCTION GetTachesParStatutEtUtilisateur (@Statut VARCHAR(50), @Id_utilisateur INT, @Id_objectif INT) RETURNS TABLE
AS
RETURN
(
    SELECT titre, description, Priorite, date_debut, date_limite, statut, Id_tache
    FROM GestionTachesDB.dbo.Tache
    WHERE statut = @Statut 
    AND Id_utilisateur = @Id_utilisateur
    AND Id_objectif = @Id_objectif
)
GO

CREATE FUNCTION GetObjectifsParStatutEtUtilisateur (@Statut VARCHAR(50), @Id_utilisateur INT) RETURNS TABLE
AS
RETURN
(
SELECT titre, description,  date_debut, date_fin, statut, Id_objectif
FROM GestionTachesDB.dbo.Objectif
WHERE statut = @Statut AND Id_utilisateur = @Id_utilisateur
)
GO

--Modification
CREATE PROCEDURE UpdateTache (@Id_tache INT, @Titre VARCHAR(255), @Description TEXT, @DateDebut DATE, @DateLimite DATE, @Statut VARCHAR(50), @Priorite VARCHAR(50))
AS
UPDATE GestionTachesDB.dbo.Tache
SET 
    titre = @Titre,
    description = @Description,
    date_debut = @DateDebut,
    date_limite = @DateLimite,
    statut = @Statut,
	Priorite = @Priorite
WHERE Id_tache = @Id_tache
GO

CREATE PROCEDURE UpdateObjectif (@Id_objectif INT, @Titre VARCHAR(255), @Description VARCHAR(MAX), @DateDebut DATE, @DateFin DATE, @Statut VARCHAR(50))
AS
UPDATE GestionTachesDB.dbo.Objectif
SET 
    titre = @Titre,
    description = @Description,
    date_debut = @DateDebut,
    date_fin = @DateFin,
    statut = @Statut
WHERE Id_objectif = @Id_objectif
GO

--Suppression
CREATE PROCEDURE SupprimerObjectif (@Id_objectif INT)
AS
DELETE FROM GestionTachesDB.dbo.Notification WHERE Id_objectif = @Id_objectif
DELETE FROM GestionTachesDB.dbo.Tache WHERE Id_objectif = @Id_objectif
DELETE FROM GestionTachesDB.dbo.Objectif WHERE Id_objectif = @Id_objectif
GO

CREATE PROCEDURE SupprimerTache (@Id_tache INT)
AS
DELETE FROM GestionTachesDB.dbo.Notification WHERE Id_tache = @Id_tache
DELETE FROM GestionTachesDB.dbo.Tache WHERE Id_tache = @Id_tache
GO

--Statistiques
CREATE PROCEDURE ObtenirRepartitionTachesParStatut (@Id_utilisateur INT)
AS
SELECT statut, COUNT(*) AS Total
FROM GestionTachesDB.dbo.Tache WHERE Id_utilisateur = @Id_utilisateur
GROUP BY statut
GO

CREATE PROCEDURE GetRepartitionObjectifsParStatut (@Id_utilisateur INT)
AS
SELECT statut, COUNT(*) AS Total
FROM Objectif
WHERE Id_utilisateur = @Id_utilisateur
GROUP BY statut
GO
