-- MariaDB dump 10.19  Distrib 10.4.32-MariaDB, for Win64 (AMD64)
--
-- Host: localhost    Database: inventory_management
-- ------------------------------------------------------
-- Server version	10.4.32-MariaDB

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `allocations`
--

DROP TABLE IF EXISTS `allocations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `allocations` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `equipment_id` int(11) NOT NULL,
  `employee_id` int(11) NOT NULL,
  `location_id` int(11) NOT NULL COMMENT 'Location where equipment will be used',
  `allocated_by_user_id` int(11) NOT NULL COMMENT 'Operator who requested',
  `approved_by_user_id` int(11) DEFAULT NULL COMMENT 'Admin who approved',
  `allocation_date` timestamp NOT NULL DEFAULT current_timestamp(),
  `approval_date` timestamp NULL DEFAULT NULL,
  `expected_return_date` date DEFAULT NULL,
  `actual_return_date` date DEFAULT NULL,
  `status` enum('pending','approved','rejected','returned') NOT NULL DEFAULT 'pending',
  `rejection_reason` text DEFAULT NULL,
  `previous_allocation_id` int(11) DEFAULT NULL COMMENT 'Link to previous allocation (for transfers)',
  PRIMARY KEY (`id`),
  KEY `location_id` (`location_id`),
  KEY `allocated_by_user_id` (`allocated_by_user_id`),
  KEY `approved_by_user_id` (`approved_by_user_id`),
  KEY `previous_allocation_id` (`previous_allocation_id`),
  KEY `idx_allocations_status` (`status`),
  KEY `idx_allocations_equipment` (`equipment_id`),
  KEY `idx_allocations_employee` (`employee_id`),
  KEY `idx_allocations_active` (`equipment_id`,`status`),
  CONSTRAINT `allocations_ibfk_1` FOREIGN KEY (`equipment_id`) REFERENCES `equipment` (`id`),
  CONSTRAINT `allocations_ibfk_2` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`),
  CONSTRAINT `allocations_ibfk_3` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`),
  CONSTRAINT `allocations_ibfk_4` FOREIGN KEY (`allocated_by_user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `allocations_ibfk_5` FOREIGN KEY (`approved_by_user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `allocations_ibfk_6` FOREIGN KEY (`previous_allocation_id`) REFERENCES `allocations` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `allocations`
--

LOCK TABLES `allocations` WRITE;
/*!40000 ALTER TABLE `allocations` DISABLE KEYS */;
INSERT INTO `allocations` VALUES (2,3,1,1,1,1,'2026-06-22 05:10:46','2026-06-22 06:41:55','2026-06-23','2026-06-24','returned',NULL,NULL),(3,21,4,1,1,1,'2026-06-23 23:58:07','2026-06-23 23:58:16',NULL,NULL,'returned',NULL,NULL),(4,3,5,3,1,1,'2026-06-24 01:33:24','2026-06-24 01:33:24',NULL,NULL,'returned',NULL,2),(5,5,5,1,1,1,'2026-06-30 00:52:21','2026-06-30 00:52:34','2026-07-01',NULL,'approved',NULL,NULL),(6,22,5,4,1,1,'2026-07-02 01:01:28','2026-07-02 01:03:53',NULL,NULL,'returned',NULL,NULL),(7,3,1,4,1,1,'2026-07-06 13:51:33','2026-07-06 13:51:43','2027-07-22','2026-07-09','returned',NULL,NULL),(9,26,1,3,1,1,'2026-07-07 00:47:36','2026-07-07 00:47:41','2026-07-08','2026-07-14','returned',NULL,NULL),(11,26,5,3,1,1,'2026-07-15 12:56:35','2026-07-15 12:56:35',NULL,NULL,'returned',NULL,9),(12,26,4,5,1,1,'2026-07-15 14:18:19','2026-07-15 14:18:27',NULL,NULL,'approved',NULL,NULL);
/*!40000 ALTER TABLE `allocations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `app_settings`
--

DROP TABLE IF EXISTS `app_settings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `app_settings` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `company_name` varchar(150) NOT NULL DEFAULT 'Q-Inventory',
  `theme` varchar(20) NOT NULL DEFAULT 'Light',
  `db_server` varchar(150) NOT NULL DEFAULT 'localhost',
  `db_name` varchar(150) NOT NULL DEFAULT 'inventory_management',
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `printer_name` varchar(200) DEFAULT NULL,
  `label_width_mm` int(11) DEFAULT 62,
  `label_height_mm` int(11) DEFAULT 29,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `app_settings`
--

LOCK TABLES `app_settings` WRITE;
/*!40000 ALTER TABLE `app_settings` DISABLE KEYS */;
INSERT INTO `app_settings` VALUES (1,'QuantumRonics','Light','localhost','inventory_management','2026-07-15 10:01:02','',62,29);
/*!40000 ALTER TABLE `app_settings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `employees`
--

DROP TABLE IF EXISTS `employees`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `employees` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `employee_code` varchar(50) NOT NULL,
  `full_name` varchar(255) NOT NULL,
  `department` varchar(100) DEFAULT NULL,
  `email` varchar(255) DEFAULT NULL,
  `phone` varchar(20) DEFAULT NULL,
  `default_location_id` int(11) DEFAULT NULL COMMENT 'Employees primary work location',
  `is_active` tinyint(1) DEFAULT 1,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `employee_code` (`employee_code`),
  KEY `default_location_id` (`default_location_id`),
  CONSTRAINT `employees_ibfk_1` FOREIGN KEY (`default_location_id`) REFERENCES `locations` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `employees`
--

LOCK TABLES `employees` WRITE;
/*!40000 ALTER TABLE `employees` DISABLE KEYS */;
INSERT INTO `employees` VALUES (1,'EMP001','Shayan','IT','shayan@gmail.com','0987654',NULL,1,'2026-06-10 10:12:19','2026-06-24 05:06:57'),(4,'EMP002','Fahad','QR','fahad@gmail.com','11223344',NULL,1,'2026-06-17 10:57:05','2026-06-17 10:57:05'),(5,'EM003','Zohaib','WD','zohaib@gmail.com','4567890',NULL,1,'2026-06-24 05:06:36','2026-06-24 05:07:05');
/*!40000 ALTER TABLE `employees` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `equipment`
--

DROP TABLE IF EXISTS `equipment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `equipment` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `equipment_code` varchar(50) NOT NULL,
  `qr_code` varchar(255) NOT NULL,
  `name` varchar(255) NOT NULL,
  `make` varchar(100) DEFAULT NULL,
  `model` varchar(100) DEFAULT NULL,
  `category` varchar(100) DEFAULT NULL,
  `description` text DEFAULT NULL,
  `vendor_name` varchar(255) DEFAULT NULL,
  `condition_grade` enum('New','Good','Fair','Poor','Unserviceable') DEFAULT NULL,
  `warranty_expiry` date DEFAULT NULL,
  `serial_number` varchar(100) DEFAULT NULL,
  `purchase_date` date DEFAULT NULL,
  `price` decimal(12,2) DEFAULT NULL,
  `procurement_type_id` int(11) DEFAULT NULL,
  `created_by_user_id` int(11) NOT NULL,
  `current_status` enum('in_store','allocated','decommissioned','under_repair') NOT NULL DEFAULT 'in_store',
  `current_location_id` int(11) DEFAULT NULL COMMENT 'Where is it right now?',
  `current_employee_id` int(11) DEFAULT NULL COMMENT 'Who has it right now? (NULL if in store)',
  `current_allocation_id` int(11) DEFAULT NULL COMMENT 'Reference to active allocation',
  `notes` text DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `equipment_code` (`equipment_code`),
  UNIQUE KEY `qr_code` (`qr_code`),
  KEY `procurement_type_id` (`procurement_type_id`),
  KEY `created_by_user_id` (`created_by_user_id`),
  KEY `idx_equipment_qr` (`qr_code`),
  KEY `idx_equipment_status` (`current_status`),
  KEY `idx_equipment_current_holder` (`current_employee_id`),
  KEY `idx_equipment_current_location` (`current_location_id`),
  KEY `idx_equipment_code` (`equipment_code`),
  CONSTRAINT `equipment_ibfk_1` FOREIGN KEY (`procurement_type_id`) REFERENCES `procurement_types` (`id`),
  CONSTRAINT `equipment_ibfk_2` FOREIGN KEY (`created_by_user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `equipment_ibfk_3` FOREIGN KEY (`current_location_id`) REFERENCES `locations` (`id`),
  CONSTRAINT `equipment_ibfk_4` FOREIGN KEY (`current_employee_id`) REFERENCES `employees` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=30 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `equipment`
--

LOCK TABLES `equipment` WRITE;
/*!40000 ALTER TABLE `equipment` DISABLE KEYS */;
INSERT INTO `equipment` VALUES (3,'EQ001','QR001','Dell Latitude','Dell','5400','IT Equipment','minor dent on edge','Ali','Good','2027-12-01','SN12345','2026-06-24',NULL,1,1,'in_store',NULL,NULL,NULL,'pin pack','2026-06-11 09:25:51','2026-07-15 07:57:00'),(5,'EQ002','QR002','Lenovo Thinkbook','Lenovo','6900','IT Equipment',NULL,NULL,'Poor',NULL,'ASR34R','2026-06-17',NULL,NULL,1,'in_store',NULL,NULL,NULL,NULL,'2026-06-19 18:43:48','2026-07-15 07:25:04'),(21,'EQ003','QR003','MacBook','Apple','231','Accessory',NULL,NULL,NULL,NULL,'SN1324','2026-06-09',NULL,1,1,'under_repair',NULL,NULL,NULL,NULL,'2026-06-24 04:57:09','2026-07-10 11:57:34'),(22,'EQ004','QR004','Dell EliteBook','HP','8973','Accessory',NULL,NULL,'Unserviceable',NULL,'FP63G','2026-06-24',NULL,1,1,'decommissioned',NULL,NULL,NULL,NULL,'2026-06-24 05:09:16','2026-07-15 07:56:55'),(26,'EQ005','QR005','Barcode Scanner','deli','14952','IT Equipment','2D plug and plau Barcode Scanner.','Zia','New','2027-07-07','A26480111949','2026-07-03',NULL,1,1,'allocated',NULL,NULL,NULL,'Purchased by Sir Zia, for IMS','2026-07-07 05:26:29','2026-07-15 07:18:27'),(28,'EQ006','QR006','Keyboard','Lenovo','26555','IT Equipment','yes','fahad','New',NULL,'PI234H5','2026-07-09',NULL,4,1,'in_store',NULL,NULL,NULL,NULL,'2026-07-10 06:38:17','2026-07-10 11:46:18');
/*!40000 ALTER TABLE `equipment` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `equipment_location_history`
--

DROP TABLE IF EXISTS `equipment_location_history`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `equipment_location_history` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `equipment_id` int(11) NOT NULL,
  `location_id` int(11) NOT NULL,
  `assigned_date` timestamp NOT NULL DEFAULT current_timestamp(),
  `end_date` timestamp NULL DEFAULT NULL COMMENT 'NULL means currently active location',
  `changed_by_user_id` int(11) NOT NULL,
  `notes` text DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `location_id` (`location_id`),
  KEY `changed_by_user_id` (`changed_by_user_id`),
  KEY `idx_location_history_active` (`equipment_id`,`end_date`),
  CONSTRAINT `equipment_location_history_ibfk_1` FOREIGN KEY (`equipment_id`) REFERENCES `equipment` (`id`) ON DELETE CASCADE,
  CONSTRAINT `equipment_location_history_ibfk_2` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`),
  CONSTRAINT `equipment_location_history_ibfk_3` FOREIGN KEY (`changed_by_user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `equipment_location_history`
--

LOCK TABLES `equipment_location_history` WRITE;
/*!40000 ALTER TABLE `equipment_location_history` DISABLE KEYS */;
/*!40000 ALTER TABLE `equipment_location_history` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `equipment_logs`
--

DROP TABLE IF EXISTS `equipment_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `equipment_logs` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `equipment_id` int(11) NOT NULL,
  `action_type` enum('equipment_created','location_changed','allocation_requested','allocation_approved','allocation_rejected','allocation_returned','decommission_requested','decommission_approved','decommission_rejected','transferred') NOT NULL,
  `changed_by_user_id` int(11) NOT NULL,
  `old_value` text DEFAULT NULL,
  `new_value` text DEFAULT NULL,
  `ip_address` varchar(45) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `changed_by_user_id` (`changed_by_user_id`),
  KEY `idx_logs_equipment` (`equipment_id`),
  KEY `idx_logs_created` (`created_at`),
  CONSTRAINT `equipment_logs_ibfk_1` FOREIGN KEY (`equipment_id`) REFERENCES `equipment` (`id`) ON DELETE CASCADE,
  CONSTRAINT `equipment_logs_ibfk_2` FOREIGN KEY (`changed_by_user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=123 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `equipment_logs`
--

LOCK TABLES `equipment_logs` WRITE;
/*!40000 ALTER TABLE `equipment_logs` DISABLE KEYS */;
INSERT INTO `equipment_logs` VALUES (1,5,'equipment_created',1,NULL,'Created: EQ002 - Lenovo Thinkbook',NULL,'2026-06-19 13:43:48'),(2,5,'location_changed',1,'EQ002 - Lenovo Thinkbook - allocated','EQ002 - Lenovo Thinkbook - decommissioned',NULL,'2026-06-19 13:45:49'),(12,3,'allocation_requested',1,NULL,'Allocated to Employee ID 1',NULL,'2026-06-22 05:10:47'),(13,5,'location_changed',1,'EQ002 - Lenovo Thinkbook - decommissioned','EQ002 - Lenovo Thinkbook - in_store',NULL,'2026-06-22 06:41:38'),(14,3,'allocation_approved',1,'pending','approved',NULL,'2026-06-22 06:41:55'),(16,3,'location_changed',1,'EQ001 - Dell Latitude - allocated','EQ001 - Dell Latitude - under_repair',NULL,'2026-06-23 23:55:44'),(17,3,'location_changed',1,'EQ001 - Dell Latitude - under_repair','EQ001 - Dell Latitude - decommissioned',NULL,'2026-06-23 23:55:58'),(18,21,'equipment_created',1,NULL,'Created: EQ003 - MacBook',NULL,'2026-06-23 23:57:09'),(19,21,'location_changed',1,'EQ003 - MacBook - allocated','EQ003 - MacBook - in_store',NULL,'2026-06-23 23:57:54'),(20,21,'allocation_requested',1,NULL,'Allocated to Employee ID 4',NULL,'2026-06-23 23:58:07'),(21,21,'allocation_approved',1,'pending','approved',NULL,'2026-06-23 23:58:16'),(22,21,'allocation_returned',1,'allocated','in_store',NULL,'2026-06-23 23:58:50'),(23,3,'location_changed',1,'EQ001 - Dell Latitude - decommissioned','EQ001 - Dell Latitude - under_repair',NULL,'2026-06-24 00:07:39'),(24,22,'equipment_created',1,NULL,'Created: EQ004 - Dell EliteBook',NULL,'2026-06-24 00:09:16'),(25,3,'location_changed',1,'EQ001 - Dell Latitude - under_repair','EQ001 - Dell Latitude - allocated',NULL,'2026-06-24 00:09:28'),(26,3,'location_changed',1,'EQ001 - Dell Latitude - allocated','EQ001 - Dell Latitude - in_store',NULL,'2026-06-24 00:09:36'),(27,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-06-24 01:31:56'),(28,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-06-24 01:32:06'),(29,3,'transferred',1,'Employee 1','Employee 5',NULL,'2026-06-24 01:33:25'),(30,22,'decommission_approved',1,'in_store','Damaged â€” Fahad Broke it',NULL,'2026-06-24 01:33:49'),(31,22,'location_changed',1,'EQ004 - Dell EliteBook - decommissioned','EQ004 - Dell EliteBook - in_store',NULL,'2026-06-24 04:25:13'),(32,21,'location_changed',1,'EQ003 - MacBook - in_store','EQ003 - MacBook - allocated',NULL,'2026-06-30 00:51:29'),(33,5,'allocation_requested',1,NULL,'Allocated to Employee ID 5',NULL,'2026-06-30 00:52:21'),(34,5,'allocation_approved',1,'pending','approved',NULL,'2026-06-30 00:52:34'),(35,21,'location_changed',1,'EQ003 - MacBook - allocated','EQ003 - MacBook - under_repair',NULL,'2026-07-01 06:30:03'),(36,22,'allocation_requested',1,NULL,'Allocated to Employee ID 5',NULL,'2026-07-02 01:01:29'),(37,22,'allocation_approved',1,'pending','approved',NULL,'2026-07-02 01:03:53'),(38,22,'allocation_returned',1,'allocated','in_store',NULL,'2026-07-02 01:04:09'),(39,21,'location_changed',1,'EQ003 - MacBook - under_repair','EQ003 - MacBook - decommissioned',NULL,'2026-07-02 02:35:47'),(40,21,'location_changed',1,'EQ003 - MacBook - decommissioned','EQ003 - MacBook - decommissioned',NULL,'2026-07-02 02:36:05'),(41,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - under_repair',NULL,'2026-07-02 04:59:54'),(45,3,'location_changed',1,'EQ001 - Dell Latitude - under_repair','EQ001 - Dell Latitude - under_repair',NULL,'2026-07-05 06:41:02'),(46,3,'location_changed',1,'EQ001 - Dell Latitude - under_repair','EQ001 - Dell Latitude - under_repair',NULL,'2026-07-05 06:41:40'),(47,5,'location_changed',1,'EQ002 - Lenovo Thinkbook - allocated','EQ002 - Lenovo Thinkbook - allocated',NULL,'2026-07-05 06:41:55'),(48,3,'allocation_returned',1,'allocated','in_store',NULL,'2026-07-05 06:46:32'),(49,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-05 07:12:03'),(50,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 05:21:08'),(51,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 05:21:24'),(52,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 05:21:43'),(53,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 05:21:54'),(54,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 05:48:27'),(55,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 05:53:23'),(56,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 05:53:39'),(57,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 05:53:54'),(58,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 05:56:38'),(59,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 05:58:17'),(60,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:00:11'),(61,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:02:02'),(62,5,'location_changed',1,'EQ002 - Lenovo Thinkbook - allocated','EQ002 - Lenovo Thinkbook - allocated',NULL,'2026-07-06 06:02:16'),(63,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:02:44'),(64,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:04:33'),(65,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:08:40'),(66,21,'location_changed',1,'EQ003 - MacBook - decommissioned','EQ003 - MacBook - decommissioned',NULL,'2026-07-06 06:09:03'),(67,5,'location_changed',1,'EQ002 - Lenovo Thinkbook - allocated','EQ002 - Lenovo Thinkbook - allocated',NULL,'2026-07-06 06:12:54'),(68,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:13:12'),(69,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:13:51'),(70,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:24:43'),(71,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:25:45'),(72,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:29:27'),(73,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:29:38'),(74,22,'location_changed',1,'EQ004 - Dell EliteBook - in_store','EQ004 - Dell EliteBook - under_repair',NULL,'2026-07-06 06:30:40'),(75,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:35:14'),(76,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:35:23'),(77,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:35:39'),(78,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:39:44'),(79,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-06 06:39:55'),(80,3,'allocation_requested',1,NULL,'Allocated to Employee ID 1',NULL,'2026-07-06 13:51:34'),(81,3,'allocation_approved',1,'pending','approved',NULL,'2026-07-06 13:51:43'),(83,26,'equipment_created',1,NULL,'Created: EQ005 - Barcode Scanner',NULL,'2026-07-07 00:26:29'),(84,26,'location_changed',1,'EQ005 - Barcode Scanner - allocated','EQ005 - Barcode Scanner - in_store',NULL,'2026-07-07 00:47:04'),(85,26,'allocation_requested',1,NULL,'Allocated to Employee ID 1',NULL,'2026-07-07 00:47:36'),(86,26,'allocation_approved',1,'pending','approved',NULL,'2026-07-07 00:47:41'),(87,21,'location_changed',1,'EQ003 - MacBook - decommissioned','EQ003 - MacBook - decommissioned',NULL,'2026-07-08 16:58:55'),(93,26,'location_changed',1,'EQ005 - Barcode Scanner - allocated','EQ005 - Barcode Scanner - allocated',NULL,'2026-07-09 15:01:06'),(95,3,'allocation_returned',1,'allocated','in_store',NULL,'2026-07-09 18:16:10'),(97,26,'location_changed',1,'EQ005 - Barcode Scanner - allocated','EQ005 - Barcode Scanner - under_repair',NULL,'2026-07-09 18:51:39'),(99,26,'location_changed',1,'EQ005 - Barcode Scanner - under_repair','EQ005 - Barcode Scanner - in_store',NULL,'2026-07-10 12:12:16'),(100,28,'equipment_created',1,NULL,'Created: EQ006 - Keyboard',NULL,'2026-07-10 13:38:17'),(101,22,'location_changed',1,'EQ004 - Dell EliteBook - under_repair','EQ004 - Dell EliteBook - under_repair',NULL,'2026-07-10 18:27:58'),(102,22,'location_changed',1,'EQ004 - Dell EliteBook - under_repair','EQ004 - Dell EliteBook - under_repair',NULL,'2026-07-10 18:28:08'),(103,26,'location_changed',1,'EQ005 - Barcode Scanner - in_store','EQ005 - Barcode Scanner - under_repair',NULL,'2026-07-10 18:28:21'),(104,28,'location_changed',1,'EQ006 - Keyboard - in_store','EQ006 - Keyboard - under_repair',NULL,'2026-07-10 18:39:18'),(105,28,'location_changed',1,'EQ006 - Keyboard - under_repair','EQ006 - Keyboard - under_repair',NULL,'2026-07-10 18:40:56'),(106,28,'location_changed',1,'EQ006 - Keyboard - under_repair','EQ006 - Keyboard - in_store',NULL,'2026-07-10 18:41:20'),(107,28,'location_changed',1,'EQ006 - Keyboard - in_store','EQ006 - Keyboard - in_store',NULL,'2026-07-10 18:46:18'),(108,21,'location_changed',1,'EQ003 - MacBook - decommissioned','EQ003 - MacBook - under_repair',NULL,'2026-07-10 18:57:34'),(109,26,'transferred',1,'Employee 1','Employee 5',NULL,'2026-07-15 12:56:35'),(110,5,'location_changed',1,'EQ002 - Lenovo Thinkbook - allocated','EQ002 - Lenovo Thinkbook - under_repair',NULL,'2026-07-15 13:02:31'),(111,5,'location_changed',1,'EQ002 - Lenovo Thinkbook - under_repair','EQ002 - Lenovo Thinkbook - in_store',NULL,'2026-07-15 13:36:58'),(112,3,'location_changed',1,'EQ001 - Dell Latitude - in_store','EQ001 - Dell Latitude - in_store',NULL,'2026-07-15 13:38:55'),(115,22,'location_changed',1,'EQ004 - Dell EliteBook - under_repair','EQ004 - Dell EliteBook - decommissioned',NULL,'2026-07-15 14:13:52'),(116,3,'decommission_approved',1,'in_store','Damaged',NULL,'2026-07-15 14:14:07'),(117,22,'location_changed',1,'EQ004 - Dell EliteBook - decommissioned','EQ004 - Dell EliteBook - in_store',NULL,'2026-07-15 14:15:13'),(118,26,'allocation_returned',1,'allocated','in_store',NULL,'2026-07-15 14:17:45'),(119,26,'allocation_requested',1,NULL,'Allocated to Employee ID 4',NULL,'2026-07-15 14:18:19'),(120,26,'allocation_approved',1,'pending','approved',NULL,'2026-07-15 14:18:27'),(121,22,'location_changed',1,'EQ004 - Dell EliteBook - in_store','EQ004 - Dell EliteBook - decommissioned',NULL,'2026-07-15 14:56:55'),(122,3,'location_changed',1,'EQ001 - Dell Latitude - decommissioned','EQ001 - Dell Latitude - in_store',NULL,'2026-07-15 14:57:00');
/*!40000 ALTER TABLE `equipment_logs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `locations`
--

DROP TABLE IF EXISTS `locations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `locations` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `location_name` varchar(100) NOT NULL COMMENT 'Lab, Kitchen, Server Room, Store',
  `building` varchar(100) DEFAULT NULL,
  `room_number` varchar(50) DEFAULT NULL,
  `allocated_person` varchar(255) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `locations`
--

LOCK TABLES `locations` WRITE;
/*!40000 ALTER TABLE `locations` DISABLE KEYS */;
INSERT INTO `locations` VALUES (1,'Main Store','Evacuee Trust Complex','108','Zia','2026-06-10 10:12:31'),(3,'QuantumRonics','Evacuee Trust Complex','109','Shayan','2026-06-24 05:04:37'),(4,'Common Criteria PK','Evacuee Trust Complex','110','Fahad','2026-06-24 05:05:11'),(5,'Quantum Communications','Evacuee','111','Haider','2026-06-24 05:05:33');
/*!40000 ALTER TABLE `locations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `maintenance_records`
--

DROP TABLE IF EXISTS `maintenance_records`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `maintenance_records` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `equipment_id` int(11) NOT NULL,
  `issue_description` text NOT NULL,
  `reported_date` date NOT NULL,
  `resolved_date` date DEFAULT NULL,
  `cost` decimal(12,2) DEFAULT NULL,
  `serviced_by` varchar(255) DEFAULT NULL,
  `status` enum('open','in_progress','resolved') NOT NULL DEFAULT 'open',
  `notes` text DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `idx_maintenance_equipment` (`equipment_id`),
  KEY `idx_maintenance_status` (`status`),
  CONSTRAINT `maintenance_records_ibfk_1` FOREIGN KEY (`equipment_id`) REFERENCES `equipment` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `maintenance_records`
--

LOCK TABLES `maintenance_records` WRITE;
/*!40000 ALTER TABLE `maintenance_records` DISABLE KEYS */;
INSERT INTO `maintenance_records` VALUES (1,26,'He broke it','2026-07-09','2026-07-09',10.00,'ALi','resolved',NULL,'2026-07-09 11:51:36'),(2,28,'Dropped and cracked by sir','2026-07-10','2026-07-10',210.00,'fahad ali','resolved',NULL,'2026-07-10 11:39:01'),(3,21,'sfsa','2026-07-10',NULL,14.00,'sdfsdfsd','open',NULL,'2026-07-10 11:57:33'),(4,5,'afdsaeasda','2026-07-14','2026-07-14',1200.00,'asda','resolved',NULL,'2026-07-15 06:02:30');
/*!40000 ALTER TABLE `maintenance_records` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `procurement_types`
--

DROP TABLE IF EXISTS `procurement_types`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `procurement_types` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `type_name` varchar(50) NOT NULL COMMENT 'Tender, Direct_Purchase, Loan, Donation',
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `type_name` (`type_name`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `procurement_types`
--

LOCK TABLES `procurement_types` WRITE;
/*!40000 ALTER TABLE `procurement_types` DISABLE KEYS */;
INSERT INTO `procurement_types` VALUES (1,'Purchased','2026-06-10 10:12:39'),(4,'Wow','2026-07-10 06:38:17'),(5,'Yes','2026-07-15 07:13:15');
/*!40000 ALTER TABLE `procurement_types` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `roles`
--

DROP TABLE IF EXISTS `roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `roles` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `role_name` varchar(50) NOT NULL COMMENT 'Admin, Operator',
  `description` text DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `role_name` (`role_name`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `roles`
--

LOCK TABLES `roles` WRITE;
/*!40000 ALTER TABLE `roles` DISABLE KEYS */;
INSERT INTO `roles` VALUES (1,'Admin','Full system access','2026-06-22 07:35:03'),(2,'Operator','Standard day-to-day access','2026-06-22 07:35:03');
/*!40000 ALTER TABLE `roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `transactions`
--

DROP TABLE IF EXISTS `transactions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `transactions` (
  `transaction_id` int(11) NOT NULL AUTO_INCREMENT,
  `equipment_id` int(11) NOT NULL,
  `transaction_type` enum('Check-In','Check-Out','Transfer','Maintenance','Disposal') NOT NULL,
  `issued_to` varchar(255) DEFAULT NULL,
  `department` varchar(100) DEFAULT NULL,
  `operator_name` varchar(255) NOT NULL,
  `transaction_date` datetime NOT NULL DEFAULT current_timestamp(),
  `expected_return` date DEFAULT NULL,
  `actual_return` date DEFAULT NULL,
  `status` enum('Pending','Completed','Overdue','Cancelled') NOT NULL DEFAULT 'Pending',
  `condition_before` enum('New','Good','Fair','Poor','Unserviceable') DEFAULT NULL,
  `condition_after` enum('New','Good','Fair','Poor','Unserviceable') DEFAULT NULL,
  `remarks` text DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`transaction_id`),
  KEY `fk_transaction_equipment` (`equipment_id`),
  CONSTRAINT `fk_transaction_equipment` FOREIGN KEY (`equipment_id`) REFERENCES `equipment` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `transactions`
--

LOCK TABLES `transactions` WRITE;
/*!40000 ALTER TABLE `transactions` DISABLE KEYS */;
INSERT INTO `transactions` VALUES (2,5,'Check-Out','Ali','IT','Administrator','2026-07-15 07:24:52','2026-07-22','2026-07-15','Completed','Good','Poor','','2026-07-15 07:24:52');
/*!40000 ALTER TABLE `transactions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_roles`
--

DROP TABLE IF EXISTS `user_roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `user_roles` (
  `user_id` int(11) NOT NULL,
  `role_id` int(11) NOT NULL,
  `assigned_at` timestamp NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`user_id`,`role_id`),
  KEY `role_id` (`role_id`),
  CONSTRAINT `user_roles_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `user_roles_ibfk_2` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_roles`
--

LOCK TABLES `user_roles` WRITE;
/*!40000 ALTER TABLE `user_roles` DISABLE KEYS */;
INSERT INTO `user_roles` VALUES (1,1,'2026-06-22 07:35:50'),(2,2,'2026-06-24 05:28:40');
/*!40000 ALTER TABLE `user_roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `users` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(100) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `email` varchar(255) DEFAULT NULL,
  `full_name` varchar(255) NOT NULL,
  `is_active` tinyint(1) DEFAULT 1,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `username` (`username`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'admin','admin123','shayan@gmail.com','Administrator',1,'2026-06-11 09:25:22','2026-07-09 06:08:33'),(2,'operator','admin1234','test@gmail.com','Test',1,'2026-06-24 05:26:20','2026-07-15 05:59:32');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-07-15  3:20:46
