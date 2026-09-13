-- --------------------------------------------------------
-- Host:                         192.168.18.240
-- Server version:               11.8.6-MariaDB-0+deb13u1 from Debian - -- Please help get to 10k stars at https://github.com/MariaDB/Server
-- Server OS:                    debian-linux-gnu
-- HeidiSQL Version:             12.20.0.7320
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- Dumping database structure for inventory_management
DROP DATABASE IF EXISTS `inventory_management`;
CREATE DATABASE IF NOT EXISTS `inventory_management` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_uca1400_ai_ci */;
USE `inventory_management`;

-- Dumping structure for table inventory_management.allocations
DROP TABLE IF EXISTS `allocations`;
CREATE TABLE IF NOT EXISTS `allocations` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `equipment_id` int(11) NOT NULL,
  `employee_id` int(11) DEFAULT NULL,
  `location_id` int(11) DEFAULT NULL,
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
) ENGINE=InnoDB AUTO_INCREMENT=28 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.allocations: ~3 rows (approximately)
INSERT INTO `allocations` (`id`, `equipment_id`, `employee_id`, `location_id`, `allocated_by_user_id`, `approved_by_user_id`, `allocation_date`, `approval_date`, `expected_return_date`, `actual_return_date`, `status`, `rejection_reason`, `previous_allocation_id`) VALUES
	(25, 40, 4, 4, 1, 1, '2026-07-31 09:14:01', '2026-07-31 09:14:26', '2026-05-13', NULL, 'returned', NULL, NULL),
	(26, 40, 1, 1, 1, 1, '2026-07-31 09:22:24', '2026-07-31 09:22:31', '2026-08-01', '2026-07-31', 'returned', NULL, NULL),
	(27, 40, 4, 3, 1, 1, '2026-07-31 09:48:53', '2026-07-31 09:48:53', NULL, NULL, 'returned', NULL, 26);

-- Dumping structure for table inventory_management.app_settings
DROP TABLE IF EXISTS `app_settings`;
CREATE TABLE IF NOT EXISTS `app_settings` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `company_name` varchar(150) NOT NULL DEFAULT 'AssetPilot',
  `theme` varchar(20) NOT NULL DEFAULT 'Light',
  `db_server` varchar(150) NOT NULL DEFAULT 'localhost',
  `db_name` varchar(150) NOT NULL DEFAULT 'inventory_management',
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `printer_name` varchar(200) DEFAULT NULL,
  `label_width_mm` int(11) DEFAULT 62,
  `label_height_mm` int(11) DEFAULT 29,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.app_settings: ~1 rows (approximately)
INSERT INTO `app_settings` (`id`, `company_name`, `theme`, `db_server`, `db_name`, `updated_at`, `printer_name`, `label_width_mm`, `label_height_mm`) VALUES
	(1, 'Quantum Communications', 'Light', 'localhost', 'inventory_management', '2026-07-30 18:48:51', '', 62, 29);

-- Dumping structure for table inventory_management.categories
DROP TABLE IF EXISTS `categories`;
CREATE TABLE IF NOT EXISTS `categories` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `category_name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.categories: ~8 rows (approximately)
INSERT INTO `categories` (`id`, `name`, `created_at`) VALUES
	(2, 'IT Equipment', '2026-07-20 11:55:22'),
	(3, 'Accessory', '2026-07-20 11:56:33'),
	(5, 'Other', '2026-07-20 11:58:23'),
	(6, 'Furniture', '2026-07-20 11:58:38'),
	(7, 'Rented', '2026-07-20 11:58:56'),
	(11, 'Electronics', '2026-07-31 05:11:40'),
	(12, 'Crockery', '2026-07-31 05:20:05'),
	(13, 'Renovation', '2026-07-31 05:20:14'),
	(14, 'Specialized Equipment', '2026-07-31 05:20:31');

-- Dumping structure for table inventory_management.employees
DROP TABLE IF EXISTS `employees`;
CREATE TABLE IF NOT EXISTS `employees` (
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
  `PicturePath` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `employee_code` (`employee_code`),
  KEY `default_location_id` (`default_location_id`),
  CONSTRAINT `employees_ibfk_1` FOREIGN KEY (`default_location_id`) REFERENCES `locations` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.employees: ~3 rows (approximately)
INSERT INTO `employees` (`id`, `employee_code`, `full_name`, `department`, `email`, `phone`, `default_location_id`, `is_active`, `created_at`, `updated_at`) VALUES
	(1, 'EMP001', 'Shayan', 'Intern', 'shayan@gmail.com', '0987654', NULL, 1, '2026-06-10 10:12:19', '2026-08-03 06:02:05'),
	(4, 'EMP002', 'Fahad', 'Developer', 'fahad@gmail.com', '11223344', NULL, 1, '2026-06-17 10:57:05', '2026-08-03 06:01:51'),
	(6, 'EMP003', 'Zia', 'Team Lead', 'zia@gmail.com', '6435634', NULL, 1, '2026-07-20 07:30:30', '2026-08-03 06:02:17'),
	(7, 'EMP004', 'Haseeb', 'HR', 'haseeb@gmail.com', '38976459', NULL, 1, '2026-07-22 11:43:45', '2026-07-22 11:43:45');

-- Dumping structure for table inventory_management.equipment
DROP TABLE IF EXISTS `equipment`;
CREATE TABLE IF NOT EXISTS `equipment` (
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
  `picture_path` varchar(500) DEFAULT NULL,
  `is_deleted` tinyint(1) NOT NULL DEFAULT 0,
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
) ENGINE=InnoDB AUTO_INCREMENT=103 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.equipment: ~68 rows (approximately)
INSERT INTO `equipment` (`id`, `equipment_code`, `qr_code`, `name`, `make`, `model`, `category`, `description`, `vendor_name`, `condition_grade`, `warranty_expiry`, `serial_number`, `purchase_date`, `price`, `procurement_type_id`, `created_by_user_id`, `current_status`, `current_location_id`, `current_employee_id`, `current_allocation_id`, `notes`, `created_at`, `updated_at`, `picture_path`, `is_deleted`) VALUES
	(35, 'EQ001', 'QR001', 'Vacuum Cleaner', 'Kleaner', 'BF575', 'Electronics', 'Laptop for finance dept', 'Dell Pakistan', 'New', '2027-03-10', 'BF-575', '2025-11-26', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:45', '2026-07-31 05:07:45', NULL, 0),
	(36, 'EQ002', 'QR002', 'Refrigerator', 'Haier', 'HRF 2761PGA', 'Electronics', NULL, 'HP Authorized Reseller', 'New', NULL, 'BL043JE00090DR9B0085', '2025-11-26', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:45', '2026-07-31 05:07:45', NULL, 0),
	(37, 'EQ003', 'QR003', 'AC - Tower 2 Ton', 'Haier', '24 HE', 'Electronics', NULL, NULL, 'New', NULL, 'AC6RG0000005NR9G0019', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:45', '2026-07-31 05:07:45', NULL, 0),
	(38, 'EQ004', 'QR004', 'AC - Tower 2 Ton', 'Haier', '25 HE', 'Electronics', NULL, NULL, 'New', NULL, 'AC6RG0000005NR3N0164', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:45', '2026-07-31 07:17:38', NULL, 0),
	(39, 'EQ005', 'QR005', 'AC - Tower 2 Ton', 'Haier', '26 HE', 'Electronics', NULL, NULL, 'New', NULL, 'AC6RG0000005NR4J0238', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:45', '2026-07-31 07:17:52', NULL, 0),
	(40, 'EQ006', 'QR006', 'AC - Split 1 Ton', 'Haier', '13 HFC', 'Electronics', NULL, NULL, 'New', NULL, 'ASCGVYE0000N9R6K0221', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:45', '2026-07-31 07:18:43', NULL, 0),
	(41, 'EQ007', 'QR007', 'AC - Split 1.5 Ton', 'Haier', '19 HFC', 'Electronics', NULL, NULL, 'New', NULL, 'AAB25LE0000N9R5U0344', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:45', '2026-07-31 05:07:45', NULL, 0),
	(42, 'EQ008', 'QR008', 'AC - Split 1.5 Ton', 'Haier', '19 HFC', 'Electronics', NULL, NULL, 'New', NULL, 'AAB25LE0000N9R641174', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:45', '2026-07-31 05:07:45', NULL, 0),
	(43, 'EQ009', 'QR009', 'AC - Split 1.5 Ton', 'Haier', '19 HFC', 'Electronics', NULL, NULL, 'New', NULL, 'AAB25LE0000N9R641181', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(44, 'EQ010', 'QR010', 'AC - Split 1.5 Ton', 'Haier', '19 HFC', 'Electronics', NULL, NULL, 'New', NULL, 'AAB25LE0000N9R5U0297', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(45, 'EQ011', 'QR011', 'AC - Split 1.5 Ton', 'Haier', '19 HFC', 'Electronics', NULL, NULL, 'New', NULL, 'AAB25LE0000N9R641159', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:10:27', NULL, 0),
	(46, 'EQ012', 'QR012', 'AC - Split 1.5 Ton', 'Haier', '19 HFC', 'Electronics', NULL, NULL, 'New', NULL, 'AAB25LE0000N9R5U0327', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(47, 'EQ013', 'QR013', 'AC - Split 1.5 Ton', 'Haier', '19 HFC', 'Electronics', NULL, NULL, 'New', NULL, 'AAB25LE0000N9R651038', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(48, 'EQ014', 'QR014', 'AC - Split 1.5 Ton', 'Haier', '19 HFC', 'Electronics', NULL, NULL, 'New', NULL, 'AAB25LE0000N9R651032', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(49, 'EQ015', 'QR015', 'AC - Split 2 Ton', 'Haier', '24 HFCD', 'Electronics', NULL, NULL, 'New', NULL, 'AACVX8E0000N9R740562', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(50, 'EQ016', 'QR016', 'AC - Split 2 Ton', 'Haier', '24 HFCD', 'Electronics', NULL, NULL, 'New', NULL, 'AACVX8E0000N9R740280', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(51, 'EQ017', 'QR017', 'AC - Split 2 Ton', 'Haier', '24 HFCD', 'Electronics', NULL, NULL, 'New', NULL, 'AACVX8E0000N9R740570', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(52, 'EQ018', 'QR018', 'Microwave', 'Dawlance', 'DW131HP', 'Electronics', NULL, NULL, 'New', NULL, 'DMAAD-DW-M-003744', '2025-12-23', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(53, 'EQ019', 'QR019', 'Water Dispenser', 'Homeage', 'HWD49432', 'Electronics', NULL, NULL, 'New', NULL, 'HWD432BGA2E000530', '2025-11-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(54, 'EQ020', 'QR020', 'Water Dispenser', 'Orient', 'G5022K2', 'Electronics', NULL, NULL, 'New', NULL, 'ZFL0000097', '2025-11-26', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(55, 'EQ021', 'QR021', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, 'New', NULL, 'SN2209001771', '2025-11-26', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(56, 'EQ022', 'QR022', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, 'New', NULL, 'SN2209000336', '2025-11-27', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(57, 'EQ023', 'QR023', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, 'New', NULL, 'SN2209000625', '2025-11-28', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(58, 'EQ024', 'QR024', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, 'New', NULL, 'SN2209000333', '2025-11-29', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(59, 'EQ025', 'QR025', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, 'New', NULL, 'SN2209001773', '2025-11-30', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(60, 'EQ026', 'QR026', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, NULL, NULL, 'SN2209000884', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(61, 'EQ027', 'QR027', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, NULL, NULL, 'SN2209001300', '2025-12-02', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(62, 'EQ028', 'QR028', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, NULL, NULL, 'SN2209000334', '2025-12-03', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(63, 'EQ029', 'QR029', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, NULL, NULL, 'SN2208001514', '2025-12-04', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(64, 'EQ030', 'QR030', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, NULL, NULL, 'SN2208001522', '2025-12-05', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(65, 'EQ031', 'QR031', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, NULL, NULL, 'SN2209001643', '2025-12-06', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:46', '2026-07-31 05:07:46', NULL, 0),
	(66, 'EQ032', 'QR032', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, NULL, NULL, 'SN2209001298', '2025-12-07', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(67, 'EQ033', 'QR033', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, NULL, NULL, 'SN2209001291', '2025-12-08', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(68, 'EQ034', 'QR034', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, NULL, NULL, 'SN2209000819', '2025-12-09', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(69, 'EQ035', 'QR035', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, NULL, NULL, 'SN2209001292', '2025-12-10', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(70, 'EQ036', 'QR036', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, NULL, NULL, 'MISSING SERIAL NUMBER', '2025-12-11', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(71, 'EQ037', 'QR037', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, NULL, NULL, 'SN2209000805', '2025-12-12', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(72, 'EQ038', 'QR038', 'Telephone Sets', 'GAOINQI', 'HCD 339', 'Electronics', NULL, NULL, NULL, NULL, 'SN2208001521', '2025-12-13', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(73, 'EQ039', 'QR039', 'Telephone Sets', 'Panasonic', 'KK-T7730X', 'Electronics', NULL, NULL, NULL, NULL, 'KX-AT7730', '2025-12-14', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(74, 'EQ040', 'QR040', 'Electric Burner', 'Signature', '4 Plates 4808', 'Electronics', NULL, NULL, NULL, NULL, 'SN540S51021034C131100013', '2025-12-15', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(75, 'EQ041', 'QR041', 'Insect Killer', 'Signature', 'GC30', 'Electronics', NULL, NULL, NULL, NULL, 'GC-3001250206', '2025-12-16', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(76, 'EQ042', 'QR042', 'Electric Kettle', 'Philips', 'HD9350', 'Electronics', NULL, NULL, NULL, NULL, 'HD9350', '2025-11-26', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(77, 'EQ043', 'QR043', 'Coffee Machine', 'Aruba', 'SCE11', 'Electronics', NULL, NULL, NULL, NULL, 'SCE11', NULL, NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(78, 'EQ044', 'QR044', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN01', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(79, 'EQ045', 'QR045', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN02', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(80, 'EQ046', 'QR046', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN03', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(81, 'EQ047', 'QR047', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN04', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(82, 'EQ048', 'QR048', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN05', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(83, 'EQ049', 'QR049', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN06', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(84, 'EQ050', 'QR050', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN07', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(85, 'EQ051', 'QR051', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN08', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(86, 'EQ052', 'QR052', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN09', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(87, 'EQ053', 'QR053', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN10', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(88, 'EQ054', 'QR054', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN11', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(89, 'EQ055', 'QR055', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN12', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:47', '2026-07-31 05:07:47', NULL, 0),
	(90, 'EQ056', 'QR056', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN13', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:48', '2026-07-31 05:07:48', NULL, 0),
	(91, 'EQ057', 'QR057', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN14', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:48', '2026-07-31 05:07:48', NULL, 0),
	(92, 'EQ058', 'QR058', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN15', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:48', '2026-07-31 05:07:48', NULL, 0),
	(93, 'EQ059', 'QR059', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN16', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:48', '2026-07-31 05:07:48', NULL, 0),
	(94, 'EQ060', 'QR060', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN17', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:48', '2026-07-31 05:07:48', NULL, 0),
	(95, 'EQ061', 'QR061', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN18', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:48', '2026-07-31 05:07:48', NULL, 0),
	(96, 'EQ062', 'QR062', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN19', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:48', '2026-07-31 05:07:48', NULL, 0),
	(97, 'EQ063', 'QR063', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN20', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:48', '2026-07-31 05:07:48', NULL, 0),
	(98, 'EQ064', 'QR064', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN21', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:48', '2026-07-31 05:07:48', NULL, 0),
	(99, 'EQ065', 'QR065', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN22', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:48', '2026-07-31 05:07:48', NULL, 0),
	(100, 'EQ066', 'QR066', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN23', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:48', '2026-07-31 05:07:48', NULL, 0),
	(101, 'EQ067', 'QR067', 'Smoke Detectors', 'Eyston', 'SD-131HSF', 'Electronics', NULL, NULL, NULL, NULL, 'SN24', '2025-12-01', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:48', '2026-07-31 05:07:48', NULL, 0),
	(102, 'EQ068', 'QR068', 'Blower', 'Dewalt', '3009', NULL, NULL, NULL, NULL, NULL, 'MOD3009', '2025-11-26', NULL, 8, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-07-31 05:07:48', '2026-07-31 05:07:48', NULL, 0);

-- Dumping structure for table inventory_management.equipment_location_history
DROP TABLE IF EXISTS `equipment_location_history`;
CREATE TABLE IF NOT EXISTS `equipment_location_history` (
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

-- Dumping data for table inventory_management.equipment_location_history: ~0 rows (approximately)

-- Dumping structure for table inventory_management.equipment_logs
DROP TABLE IF EXISTS `equipment_logs`;
CREATE TABLE IF NOT EXISTS `equipment_logs` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `equipment_id` int(11) NOT NULL,
  `action_type` varchar(50) NOT NULL,
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
) ENGINE=InnoDB AUTO_INCREMENT=270 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.equipment_logs: ~77 rows (approximately)
INSERT INTO `equipment_logs` (`id`, `equipment_id`, `action_type`, `changed_by_user_id`, `old_value`, `new_value`, `ip_address`, `created_at`) VALUES
	(182, 35, 'equipment_created', 1, NULL, 'Created: EQ001 - Vacuum Cleaner', NULL, '2026-07-31 09:07:47'),
	(183, 36, 'equipment_created', 1, NULL, 'Created: EQ002 - Refrigerator', NULL, '2026-07-31 09:07:47'),
	(184, 37, 'equipment_created', 1, NULL, 'Created: EQ003 - AC - Tower 2 Ton', NULL, '2026-07-31 09:07:47'),
	(185, 38, 'equipment_created', 1, NULL, 'Created: EQ004 - AC - Tower 2 Ton', NULL, '2026-07-31 09:07:47'),
	(186, 39, 'equipment_created', 1, NULL, 'Created: EQ005 - AC - Tower 2 Ton', NULL, '2026-07-31 09:07:47'),
	(187, 40, 'equipment_created', 1, NULL, 'Created: EQ006 - AC - Split 1 Ton', NULL, '2026-07-31 09:07:47'),
	(188, 41, 'equipment_created', 1, NULL, 'Created: EQ007 - AC - Split 1.5 Ton', NULL, '2026-07-31 09:07:47'),
	(189, 42, 'equipment_created', 1, NULL, 'Created: EQ008 - AC - Split 1.5 Ton', NULL, '2026-07-31 09:07:47'),
	(190, 43, 'equipment_created', 1, NULL, 'Created: EQ009 - AC - Split 1.5 Ton', NULL, '2026-07-31 09:07:47'),
	(191, 44, 'equipment_created', 1, NULL, 'Created: EQ010 - AC - Split 1.5 Ton', NULL, '2026-07-31 09:07:47'),
	(192, 45, 'equipment_created', 1, NULL, 'Created: EQ011 - AC - Split 1.5 Ton', NULL, '2026-07-31 09:07:47'),
	(193, 46, 'equipment_created', 1, NULL, 'Created: EQ012 - AC - Split 1.5 Ton', NULL, '2026-07-31 09:07:47'),
	(194, 47, 'equipment_created', 1, NULL, 'Created: EQ013 - AC - Split 1.5 Ton', NULL, '2026-07-31 09:07:47'),
	(195, 48, 'equipment_created', 1, NULL, 'Created: EQ014 - AC - Split 1.5 Ton', NULL, '2026-07-31 09:07:47'),
	(196, 49, 'equipment_created', 1, NULL, 'Created: EQ015 - AC - Split 2 Ton', NULL, '2026-07-31 09:07:47'),
	(197, 50, 'equipment_created', 1, NULL, 'Created: EQ016 - AC - Split 2 Ton', NULL, '2026-07-31 09:07:48'),
	(198, 51, 'equipment_created', 1, NULL, 'Created: EQ017 - AC - Split 2 Ton', NULL, '2026-07-31 09:07:48'),
	(199, 52, 'equipment_created', 1, NULL, 'Created: EQ018 - Microwave', NULL, '2026-07-31 09:07:48'),
	(200, 53, 'equipment_created', 1, NULL, 'Created: EQ019 - Water Dispenser', NULL, '2026-07-31 09:07:48'),
	(201, 54, 'equipment_created', 1, NULL, 'Created: EQ020 - Water Dispenser', NULL, '2026-07-31 09:07:48'),
	(202, 55, 'equipment_created', 1, NULL, 'Created: EQ021 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(203, 56, 'equipment_created', 1, NULL, 'Created: EQ022 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(204, 57, 'equipment_created', 1, NULL, 'Created: EQ023 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(205, 58, 'equipment_created', 1, NULL, 'Created: EQ024 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(206, 59, 'equipment_created', 1, NULL, 'Created: EQ025 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(207, 60, 'equipment_created', 1, NULL, 'Created: EQ026 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(208, 61, 'equipment_created', 1, NULL, 'Created: EQ027 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(209, 62, 'equipment_created', 1, NULL, 'Created: EQ028 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(210, 63, 'equipment_created', 1, NULL, 'Created: EQ029 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(211, 64, 'equipment_created', 1, NULL, 'Created: EQ030 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(212, 65, 'equipment_created', 1, NULL, 'Created: EQ031 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(213, 66, 'equipment_created', 1, NULL, 'Created: EQ032 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(214, 67, 'equipment_created', 1, NULL, 'Created: EQ033 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(215, 68, 'equipment_created', 1, NULL, 'Created: EQ034 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(216, 69, 'equipment_created', 1, NULL, 'Created: EQ035 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(217, 70, 'equipment_created', 1, NULL, 'Created: EQ036 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(218, 71, 'equipment_created', 1, NULL, 'Created: EQ037 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(219, 72, 'equipment_created', 1, NULL, 'Created: EQ038 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(220, 73, 'equipment_created', 1, NULL, 'Created: EQ039 - Telephone Sets', NULL, '2026-07-31 09:07:48'),
	(221, 74, 'equipment_created', 1, NULL, 'Created: EQ040 - Electric Burner', NULL, '2026-07-31 09:07:48'),
	(222, 75, 'equipment_created', 1, NULL, 'Created: EQ041 - Insect Killer', NULL, '2026-07-31 09:07:48'),
	(223, 76, 'equipment_created', 1, NULL, 'Created: EQ042 - Electric Kettle', NULL, '2026-07-31 09:07:49'),
	(224, 77, 'equipment_created', 1, NULL, 'Created: EQ043 - Coffee Machine', NULL, '2026-07-31 09:07:49'),
	(225, 78, 'equipment_created', 1, NULL, 'Created: EQ044 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(226, 79, 'equipment_created', 1, NULL, 'Created: EQ045 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(227, 80, 'equipment_created', 1, NULL, 'Created: EQ046 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(228, 81, 'equipment_created', 1, NULL, 'Created: EQ047 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(229, 82, 'equipment_created', 1, NULL, 'Created: EQ048 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(230, 83, 'equipment_created', 1, NULL, 'Created: EQ049 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(231, 84, 'equipment_created', 1, NULL, 'Created: EQ050 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(232, 85, 'equipment_created', 1, NULL, 'Created: EQ051 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(233, 86, 'equipment_created', 1, NULL, 'Created: EQ052 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(234, 87, 'equipment_created', 1, NULL, 'Created: EQ053 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(235, 88, 'equipment_created', 1, NULL, 'Created: EQ054 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(236, 89, 'equipment_created', 1, NULL, 'Created: EQ055 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(237, 90, 'equipment_created', 1, NULL, 'Created: EQ056 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(238, 91, 'equipment_created', 1, NULL, 'Created: EQ057 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(239, 92, 'equipment_created', 1, NULL, 'Created: EQ058 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(240, 93, 'equipment_created', 1, NULL, 'Created: EQ059 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(241, 94, 'equipment_created', 1, NULL, 'Created: EQ060 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(242, 95, 'equipment_created', 1, NULL, 'Created: EQ061 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(243, 96, 'equipment_created', 1, NULL, 'Created: EQ062 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(244, 97, 'equipment_created', 1, NULL, 'Created: EQ063 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(245, 98, 'equipment_created', 1, NULL, 'Created: EQ064 - Smoke Detectors', NULL, '2026-07-31 09:07:49'),
	(246, 99, 'equipment_created', 1, NULL, 'Created: EQ065 - Smoke Detectors', NULL, '2026-07-31 09:07:50'),
	(247, 100, 'equipment_created', 1, NULL, 'Created: EQ066 - Smoke Detectors', NULL, '2026-07-31 09:07:50'),
	(248, 101, 'equipment_created', 1, NULL, 'Created: EQ067 - Smoke Detectors', NULL, '2026-07-31 09:07:50'),
	(249, 102, 'equipment_created', 1, NULL, 'Created: EQ068 - Blower', NULL, '2026-07-31 09:07:50'),
	(250, 45, 'equipment_deleted', 1, 'EQ011 - AC - Split 1.5 Ton - in_store', NULL, NULL, '2026-07-31 09:10:24'),
	(251, 45, 'equipment_restored', 1, NULL, 'EQ011 - AC - Split 1.5 Ton restored', NULL, '2026-07-31 09:10:29'),
	(252, 40, 'allocation_requested', 1, NULL, 'Allocated to Employee ID 4', NULL, '2026-07-31 09:14:01'),
	(253, 40, 'allocation_approved', 1, 'pending', 'approved', NULL, '2026-07-31 09:14:26'),
	(254, 40, 'Expected Return Date Updated', 1, '2026-08-01', '2026-05-13', NULL, '2026-07-31 09:15:03'),
	(255, 40, 'allocation_returned', 1, 'allocated', 'in_store', NULL, '2026-07-31 09:15:43'),
	(256, 40, 'allocation_requested', 1, NULL, 'Allocated to Employee ID 1', NULL, '2026-07-31 09:22:24'),
	(257, 40, 'allocation_approved', 1, 'pending', 'approved', NULL, '2026-07-31 09:22:31'),
	(258, 39, 'equipment_updated', 1, 'EQ005 - AC - Tower 2 Ton - in_store', 'EQ005 - AC - Tower 2 Ton - under_repair', NULL, '2026-07-31 09:32:25'),
	(259, 40, 'transferred', 1, 'Employee 1', 'Employee 4', NULL, '2026-07-31 09:48:53'),
	(260, 38, 'equipment_updated', 1, 'EQ004 - AC - Tower 2 Ton - in_store', 'EQ004 - AC - Tower 2 Ton - decommissioned', NULL, '2026-07-31 09:52:07'),
	(261, 38, 'equipment_updated', 1, 'EQ004 - AC - Tower 2 Ton - decommissioned', 'EQ004 - AC - Tower 2 Ton - in_store', NULL, '2026-07-31 11:17:39'),
	(262, 39, 'equipment_updated', 1, 'EQ005 - AC - Tower 2 Ton - under_repair', 'EQ005 - AC - Tower 2 Ton - in_store', NULL, '2026-07-31 11:17:54'),
	(263, 40, 'allocation_returned', 1, 'allocated', 'in_store', NULL, '2026-07-31 11:18:45');

-- Dumping structure for table inventory_management.locations
DROP TABLE IF EXISTS `locations`;
CREATE TABLE IF NOT EXISTS `locations` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `location_name` varchar(100) NOT NULL COMMENT 'Lab, Kitchen, Server Room, Store',
  `building` varchar(100) DEFAULT NULL,
  `room_number` varchar(50) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  `room_name` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.locations: ~4 rows (approximately)
INSERT INTO `locations` (`id`, `location_name`, `building`, `room_number`, `created_at`, `room_name`) VALUES
	(1, 'Main Store', 'Evacuee Trust Complex', '108', '2026-06-10 10:12:31', 'Quantum Lab'),
	(3, 'QuantumRonics', 'Evacuee Trust Complex', '109', '2026-06-24 05:04:37', 'Confrence Room'),
	(4, 'Common Criteria PK', 'Evacuee Trust Complex', '110', '2026-06-24 05:05:11', 'Main Lab'),
	(5, 'Quantum Communications', 'Evacuee', '111', '2026-06-24 05:05:33', 'PKI Lab'),
	(6, 'Quantum Valley', 'Evacuee', '123', '2026-07-28 04:27:45', 'Lab 01'),
	(7, 'Cafe', 'Evacuee', '1', '2026-07-31 07:53:58', 'Cafe'),
	(8, 'QuantumRonics', 'Evacuee Trust Complex', '33', '2026-08-03 05:51:08', 'PKI Lab03');

-- Dumping structure for table inventory_management.maintenance_records
DROP TABLE IF EXISTS `maintenance_records`;
CREATE TABLE IF NOT EXISTS `maintenance_records` (
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
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Dumping data for table inventory_management.maintenance_records: ~1 rows (approximately)
INSERT INTO `maintenance_records` (`id`, `equipment_id`, `issue_description`, `reported_date`, `resolved_date`, `cost`, `serviced_by`, `status`, `notes`, `created_at`) VALUES
	(6, 39, 'Amri broke it', '2026-07-31', '2026-07-31', 52468.00, 'Ali', 'resolved', NULL, '2026-07-31 05:32:21');

-- Dumping structure for table inventory_management.procurement_types
DROP TABLE IF EXISTS `procurement_types`;
CREATE TABLE IF NOT EXISTS `procurement_types` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `type_name` varchar(50) NOT NULL COMMENT 'Tender, Direct_Purchase, Loan, Donation',
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `type_name` (`type_name`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.procurement_types: ~6 rows (approximately)
INSERT INTO `procurement_types` (`id`, `type_name`, `created_at`) VALUES
	(1, 'Purchased', '2026-06-10 10:12:39'),
	(4, 'Wow', '2026-07-10 06:38:17'),
	(5, 'Yes', '2026-07-15 07:13:15'),
	(6, 'Borrowed', '2026-07-15 11:34:39'),
	(7, 'Stole', '2026-07-30 09:25:19'),
	(8, 'Local Purchase', '2026-07-31 05:07:44');

-- Dumping structure for table inventory_management.roles
DROP TABLE IF EXISTS `roles`;
CREATE TABLE IF NOT EXISTS `roles` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `role_name` varchar(50) NOT NULL COMMENT 'Admin, Operator',
  `description` text DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `role_name` (`role_name`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.roles: ~2 rows (approximately)
INSERT INTO `roles` (`id`, `role_name`, `description`, `created_at`) VALUES
	(1, 'Admin', 'Full system access', '2026-06-22 07:35:03'),
	(2, 'Operator', 'Standard day-to-day access', '2026-06-22 07:35:03');

-- Dumping structure for table inventory_management.transactions
DROP TABLE IF EXISTS `transactions`;
CREATE TABLE IF NOT EXISTS `transactions` (
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
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.transactions: ~0 rows (approximately)

-- Dumping structure for table inventory_management.user_roles
DROP TABLE IF EXISTS `user_roles`;
CREATE TABLE IF NOT EXISTS `user_roles` (
  `user_id` int(11) NOT NULL,
  `role_id` int(11) NOT NULL,
  `assigned_at` timestamp NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`user_id`,`role_id`),
  KEY `role_id` (`role_id`),
  CONSTRAINT `user_roles_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `user_roles_ibfk_2` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.user_roles: ~3 rows (approximately)
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_at`) VALUES
	(1, 1, '2026-06-22 07:35:50'),
	(20, 2, '2026-07-31 07:12:00'),
	(21, 2, '2026-08-03 07:00:00');

-- Dumping structure for table inventory_management.users
DROP TABLE IF EXISTS `users`;
CREATE TABLE IF NOT EXISTS `users` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(100) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `email` varchar(255) DEFAULT NULL,
  `full_name` varchar(255) NOT NULL,
  `is_active` tinyint(1) DEFAULT 1,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `PicturePath` varchar(255) DEFAULT NULL,
  `PasswordChangedAt` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `username` (`username`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB AUTO_INCREMENT=22 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.users: ~2 rows (approximately)
INSERT INTO `users` (`id`, `username`, `password_hash`, `email`, `full_name`, `is_active`, `created_at`, `updated_at`, `PicturePath`) VALUES
	(1, 'admin', 'admin1234', 'shayan@gmail.com', 'Shayan', 1, '2026-06-11 09:25:22', '2026-08-03 05:39:42', 'admin_7747c04c07d34993b8bdcc17ba16706b.png'),
	(20, 'amir', 'Pakistan@123!', 'amir@quantum.com', 'Muhammad Amir', 1, '2026-07-31 07:12:00', '2026-08-03 05:00:53', NULL),
	(21, 'operator', 'admin123', 'fahad@gmail.com', 'Fahad', 1, '2026-08-03 06:59:18', '2026-08-03 06:59:18', NULL);

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
