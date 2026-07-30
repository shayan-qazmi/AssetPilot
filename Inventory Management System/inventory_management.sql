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
) ENGINE=InnoDB AUTO_INCREMENT=25 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.allocations: ~0 rows (approximately)

-- Dumping structure for table inventory_management.app_settings
DROP TABLE IF EXISTS `app_settings`;
CREATE TABLE IF NOT EXISTS `app_settings` (
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
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.categories: ~6 rows (approximately)
INSERT INTO `categories` (`id`, `name`, `created_at`) VALUES
	(2, 'IT Equipment', '2026-07-20 11:55:22'),
	(3, 'Accessory', '2026-07-20 11:56:33'),
	(5, 'Other', '2026-07-20 11:58:23'),
	(6, 'Furniture', '2026-07-20 11:58:38'),
	(7, 'Rented', '2026-07-20 11:58:56'),
	(10, 'New', '2026-07-21 04:48:05');

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
  PRIMARY KEY (`id`),
  UNIQUE KEY `employee_code` (`employee_code`),
  KEY `default_location_id` (`default_location_id`),
  CONSTRAINT `employees_ibfk_1` FOREIGN KEY (`default_location_id`) REFERENCES `locations` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.employees: ~3 rows (approximately)
INSERT INTO `employees` (`id`, `employee_code`, `full_name`, `department`, `email`, `phone`, `default_location_id`, `is_active`, `created_at`, `updated_at`) VALUES
	(1, 'EMP001', 'Shayan', 'IT', 'shayan@gmail.com', '0987654', NULL, 1, '2026-06-10 10:12:19', '2026-06-24 05:06:57'),
	(4, 'EMP002', 'Fahad', 'QR', 'fahad@gmail.com', '11223344', NULL, 1, '2026-06-17 10:57:05', '2026-06-17 10:57:05'),
	(6, 'EMP003', 'Zia', 'IT', 'zia@gmail.com', '6435634', NULL, 1, '2026-07-20 07:30:30', '2026-07-20 07:30:30'),
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
) ENGINE=InnoDB AUTO_INCREMENT=35 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.equipment: ~0 rows (approximately)

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
) ENGINE=InnoDB AUTO_INCREMENT=182 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.equipment_logs: ~0 rows (approximately)

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
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.locations: ~4 rows (approximately)
INSERT INTO `locations` (`id`, `location_name`, `building`, `room_number`, `created_at`, `room_name`) VALUES
	(1, 'Main Store', 'Evacuee Trust Complex', '108', '2026-06-10 10:12:31', 'Quantum Lab'),
	(3, 'QuantumRonics', 'Evacuee Trust Complex', '109', '2026-06-24 05:04:37', 'Confrence Room'),
	(4, 'Common Criteria PK', 'Evacuee Trust Complex', '110', '2026-06-24 05:05:11', 'Main Lab'),
	(5, 'Quantum Communications', 'Evacuee', '111', '2026-06-24 05:05:33', 'PKI Lab'),
	(6, 'Quantum Valley', 'Evacuee', '123', '2026-07-28 04:27:45', 'Lab 01');

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
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Dumping data for table inventory_management.maintenance_records: ~0 rows (approximately)

-- Dumping structure for table inventory_management.procurement_types
DROP TABLE IF EXISTS `procurement_types`;
CREATE TABLE IF NOT EXISTS `procurement_types` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `type_name` varchar(50) NOT NULL COMMENT 'Tender, Direct_Purchase, Loan, Donation',
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `type_name` (`type_name`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.procurement_types: ~5 rows (approximately)
INSERT INTO `procurement_types` (`id`, `type_name`, `created_at`) VALUES
	(1, 'Purchased', '2026-06-10 10:12:39'),
	(4, 'Wow', '2026-07-10 06:38:17'),
	(5, 'Yes', '2026-07-15 07:13:15'),
	(6, 'Borrowed', '2026-07-15 11:34:39'),
	(7, 'Stole', '2026-07-30 09:25:19');

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

-- Dumping data for table inventory_management.user_roles: ~2 rows (approximately)
INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_at`) VALUES
	(1, 1, '2026-06-22 07:35:50'),
	(19, 2, '2026-07-28 04:50:14');

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
  PRIMARY KEY (`id`),
  UNIQUE KEY `username` (`username`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB AUTO_INCREMENT=20 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dumping data for table inventory_management.users: ~2 rows (approximately)
INSERT INTO `users` (`id`, `username`, `password_hash`, `email`, `full_name`, `is_active`, `created_at`, `updated_at`) VALUES
	(1, 'admin', 'admin123', 'shayan@gmail.com', 'Shayan', 1, '2026-06-11 09:25:22', '2026-07-29 09:48:12'),
	(19, 'New operator', 'admin1234', 'Ali@gmail.com', 'Ali', 0, '2026-07-28 04:50:14', '2026-07-30 09:49:19');

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
