-- phpMyAdmin SQL Dump
-- version 4.5.4.1
-- http://www.phpmyadmin.net
--
-- Client :  localhost
-- Généré le :  Mer 11 Mars 2026 à 12:39
-- Version du serveur :  5.7.11
-- Version de PHP :  7.0.3

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Base de données :  `application_crm_lourde`
--

-- --------------------------------------------------------

--
-- Structure de la table `clients`
--

CREATE TABLE `clients` (
  `IdCli` int(11) NOT NULL,
  `NomCli` varchar(30) NOT NULL,
  `PrenomCli` varchar(30) NOT NULL,
  `MailCli` varchar(100) NOT NULL,
  `TelCli` varchar(10) NOT NULL,
  `VilleCli` varchar(30) NOT NULL,
  `CPCli` int(5) NOT NULL,
  `RueCli` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Contenu de la table `clients`
--

INSERT INTO `clients` (`IdCli`, `NomCli`, `PrenomCli`, `MailCli`, `TelCli`, `VilleCli`, `CPCli`, `RueCli`) VALUES
(1, 'Dupont', 'Jean', 'jean.dupont@example.com', '0601020304', 'Paris', 75001, '10 Rue de Rivoli');

-- --------------------------------------------------------

--
-- Structure de la table `contacts`
--

CREATE TABLE `contacts` (
  `IdContact` int(11) NOT NULL,
  `IdCli` int(11) NOT NULL,
  `IdProsp` int(11) NOT NULL,
  `DateRdv` datetime NOT NULL,
  `HeureRdv` time(6) NOT NULL,
  `DureeRdv` int(3) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Structure de la table `factures`
--

CREATE TABLE `factures` (
  `IdFact` int(11) NOT NULL,
  `IdCli` int(11) NOT NULL,
  `PrixFact` double(8,2) NOT NULL,
  `DateFact` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Contenu de la table `factures`
--

INSERT INTO `factures` (`IdFact`, `IdCli`, `PrixFact`, `DateFact`) VALUES
(2, 1, 4.00, '0000-00-00 00:00:00'),
(3, 1, 490000.00, '0000-00-00 00:00:00');

-- --------------------------------------------------------

--
-- Structure de la table `lignefact`
--

CREATE TABLE `lignefact` (
  `IdFact` int(11) NOT NULL,
  `IdProd` int(11) NOT NULL,
  `Qte` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Structure de la table `managers`
--

CREATE TABLE `managers` (
  `IdMan` int(11) NOT NULL,
  `NomMan` varchar(30) NOT NULL,
  `PrenomMan` varchar(30) NOT NULL,
  `MailMan` varchar(100) NOT NULL,
  `MdpMan` varchar(200) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Contenu de la table `managers`
--

INSERT INTO `managers` (`IdMan`, `NomMan`, `PrenomMan`, `MailMan`, `MdpMan`) VALUES
(1, 'Borde', 'Rémy', 'remy.borde34@gmail.com', 'Remy2009@#34B');

-- --------------------------------------------------------

--
-- Structure de la table `produits`
--

CREATE TABLE `produits` (
  `IdProd` int(11) NOT NULL,
  `NomProd` varchar(50) NOT NULL,
  `DescProd` varchar(300) NOT NULL,
  `PrixProd` decimal(8,2) NOT NULL,
  `StockProd` int(4) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Contenu de la table `produits`
--

INSERT INTO `produits` (`IdProd`, `NomProd`, `DescProd`, `PrixProd`, `StockProd`) VALUES
(9, 'efgdsbhsf', 'sfgnfhjdfj', '45.00', 45),
(10, 'eyh dsyht', 'drydsty', '1254.00', 12);

-- --------------------------------------------------------

--
-- Structure de la table `prospects`
--

CREATE TABLE `prospects` (
  `IdProsp` int(11) NOT NULL,
  `NomProsp` varchar(30) NOT NULL,
  `PrenomProsp` varchar(30) NOT NULL,
  `MailProsp` varchar(100) NOT NULL,
  `TelProsp` varchar(10) NOT NULL,
  `VilleProsp` varchar(30) NOT NULL,
  `CPProsp` int(5) NOT NULL,
  `RueProsp` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

--
-- Index pour les tables exportées
--

--
-- Index pour la table `clients`
--
ALTER TABLE `clients`
  ADD PRIMARY KEY (`IdCli`);

--
-- Index pour la table `contacts`
--
ALTER TABLE `contacts`
  ADD PRIMARY KEY (`IdContact`),
  ADD KEY `IdProsp` (`IdProsp`),
  ADD KEY `IdCli` (`IdCli`);

--
-- Index pour la table `factures`
--
ALTER TABLE `factures`
  ADD PRIMARY KEY (`IdFact`),
  ADD KEY `IdCli` (`IdCli`);

--
-- Index pour la table `lignefact`
--
ALTER TABLE `lignefact`
  ADD PRIMARY KEY (`IdFact`),
  ADD KEY `IdFact` (`IdFact`),
  ADD KEY `IdProd` (`IdProd`);

--
-- Index pour la table `managers`
--
ALTER TABLE `managers`
  ADD PRIMARY KEY (`IdMan`);

--
-- Index pour la table `produits`
--
ALTER TABLE `produits`
  ADD PRIMARY KEY (`IdProd`);

--
-- Index pour la table `prospects`
--
ALTER TABLE `prospects`
  ADD PRIMARY KEY (`IdProsp`);

--
-- AUTO_INCREMENT pour les tables exportées
--

--
-- AUTO_INCREMENT pour la table `clients`
--
ALTER TABLE `clients`
  MODIFY `IdCli` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;
--
-- AUTO_INCREMENT pour la table `contacts`
--
ALTER TABLE `contacts`
  MODIFY `IdContact` int(11) NOT NULL AUTO_INCREMENT;
--
-- AUTO_INCREMENT pour la table `factures`
--
ALTER TABLE `factures`
  MODIFY `IdFact` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;
--
-- AUTO_INCREMENT pour la table `managers`
--
ALTER TABLE `managers`
  MODIFY `IdMan` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;
--
-- AUTO_INCREMENT pour la table `produits`
--
ALTER TABLE `produits`
  MODIFY `IdProd` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=11;
--
-- AUTO_INCREMENT pour la table `prospects`
--
ALTER TABLE `prospects`
  MODIFY `IdProsp` int(11) NOT NULL AUTO_INCREMENT;
--
-- Contraintes pour les tables exportées
--

--
-- Contraintes pour la table `contacts`
--
ALTER TABLE `contacts`
  ADD CONSTRAINT `FK_contacts_clients` FOREIGN KEY (`IdCli`) REFERENCES `clients` (`IdCli`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_contacts_prospects` FOREIGN KEY (`IdProsp`) REFERENCES `prospects` (`IdProsp`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `factures`
--
ALTER TABLE `factures`
  ADD CONSTRAINT `FK_factures_clients` FOREIGN KEY (`IdCli`) REFERENCES `clients` (`IdCli`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Contraintes pour la table `lignefact`
--
ALTER TABLE `lignefact`
  ADD CONSTRAINT `FK_lignefact_factures` FOREIGN KEY (`IdFact`) REFERENCES `factures` (`IdFact`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `FK_lignefact_produits` FOREIGN KEY (`IdProd`) REFERENCES `produits` (`IdProd`) ON DELETE CASCADE ON UPDATE CASCADE;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
