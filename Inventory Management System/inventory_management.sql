-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: Jun 24, 2026 at 01:17 PM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `inventory_management`
--

-- --------------------------------------------------------

--
-- Table structure for table `allocations`
--

CREATE TABLE `allocations` (
  `id` int(11) NOT NULL,
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
  `previous_allocation_id` int(11) DEFAULT NULL COMMENT 'Link to previous allocation (for transfers)'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `allocations`
--

INSERT INTO `allocations` (`id`, `equipment_id`, `employee_id`, `location_id`, `allocated_by_user_id`, `approved_by_user_id`, `allocation_date`, `approval_date`, `expected_return_date`, `actual_return_date`, `status`, `rejection_reason`, `previous_allocation_id`) VALUES
(2, 3, 1, 1, 1, 1, '2026-06-22 05:10:46', '2026-06-22 06:41:55', '2026-06-23', '2026-06-24', 'returned', NULL, NULL),
(3, 21, 4, 1, 1, 1, '2026-06-23 23:58:07', '2026-06-23 23:58:16', NULL, NULL, 'returned', NULL, NULL),
(4, 3, 5, 3, 1, 1, '2026-06-24 01:33:24', '2026-06-24 01:33:24', NULL, NULL, 'approved', NULL, 2);

-- --------------------------------------------------------

--
-- Table structure for table `app_settings`
--

CREATE TABLE `app_settings` (
  `id` int(11) NOT NULL,
  `company_name` varchar(150) NOT NULL DEFAULT 'Q-Inventory',
  `theme` varchar(20) NOT NULL DEFAULT 'Light',
  `db_server` varchar(150) NOT NULL DEFAULT 'localhost',
  `db_name` varchar(150) NOT NULL DEFAULT 'inventory_management',
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `printer_name` varchar(200) DEFAULT NULL,
  `label_width_mm` int(11) DEFAULT 62,
  `label_height_mm` int(11) DEFAULT 29
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `app_settings`
--

INSERT INTO `app_settings` (`id`, `company_name`, `theme`, `db_server`, `db_name`, `updated_at`, `printer_name`, `label_width_mm`, `label_height_mm`) VALUES
(1, 'Q-Inventory', 'Light', 'localhost', 'inventory_management', '2026-06-24 06:40:23', 'HP', 62, 29);

-- --------------------------------------------------------

--
-- Table structure for table `employees`
--

CREATE TABLE `employees` (
  `id` int(11) NOT NULL,
  `employee_code` varchar(50) NOT NULL,
  `full_name` varchar(255) NOT NULL,
  `department` varchar(100) DEFAULT NULL,
  `email` varchar(255) DEFAULT NULL,
  `phone` varchar(20) DEFAULT NULL,
  `default_location_id` int(11) DEFAULT NULL COMMENT 'Employees primary work location',
  `is_active` tinyint(1) DEFAULT 1,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `employees`
--

INSERT INTO `employees` (`id`, `employee_code`, `full_name`, `department`, `email`, `phone`, `default_location_id`, `is_active`, `created_at`, `updated_at`) VALUES
(1, 'EMP001', 'Shayan', 'IT', 'shayan@gmail.com', '0987654', NULL, 1, '2026-06-10 10:12:19', '2026-06-24 05:06:57'),
(4, 'EMP002', 'Fahad', 'QR', 'fahad@gmail.com', '11223344', NULL, 1, '2026-06-17 10:57:05', '2026-06-17 10:57:05'),
(5, 'EM003', 'Zohaib', 'WD', 'zohaib@gmail.com', '4567890', NULL, 1, '2026-06-24 05:06:36', '2026-06-24 05:07:05');

-- --------------------------------------------------------

--
-- Table structure for table `equipment`
--

CREATE TABLE `equipment` (
  `id` int(11) NOT NULL,
  `equipment_code` varchar(50) NOT NULL,
  `qr_code` varchar(255) NOT NULL,
  `name` varchar(255) NOT NULL,
  `make` varchar(100) DEFAULT NULL,
  `model` varchar(100) DEFAULT NULL,
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
  `updated_at` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `equipment`
--

INSERT INTO `equipment` (`id`, `equipment_code`, `qr_code`, `name`, `make`, `model`, `serial_number`, `purchase_date`, `price`, `procurement_type_id`, `created_by_user_id`, `current_status`, `current_location_id`, `current_employee_id`, `current_allocation_id`, `notes`, `created_at`, `updated_at`) VALUES
(3, 'EQ001', 'QR001', 'Dell Latitude', 'Dell', '5400', 'SN12345', '2026-06-24', NULL, NULL, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-06-11 09:25:51', '2026-06-24 05:09:36'),
(5, 'EQ002', 'QR002', 'Lenovo Thinkbook', 'Lenovo', '6900', 'ASR34R', '2026-06-17', NULL, NULL, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-06-19 18:43:48', '2026-06-22 11:41:38'),
(21, 'EQ003', 'QR003', 'MacBook', 'Apple', '231', 'SN1324', '2026-06-09', NULL, NULL, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-06-24 04:57:09', '2026-06-24 04:58:50'),
(22, 'EQ004', 'QR004', 'Dell EliteBook', 'HP', '8973', 'FP63G', '2026-06-24', NULL, NULL, 1, 'in_store', NULL, NULL, NULL, NULL, '2026-06-24 05:09:16', '2026-06-24 09:25:13');

-- --------------------------------------------------------

--
-- Table structure for table `equipment_location_history`
--

CREATE TABLE `equipment_location_history` (
  `id` int(11) NOT NULL,
  `equipment_id` int(11) NOT NULL,
  `location_id` int(11) NOT NULL,
  `assigned_date` timestamp NOT NULL DEFAULT current_timestamp(),
  `end_date` timestamp NULL DEFAULT NULL COMMENT 'NULL means currently active location',
  `changed_by_user_id` int(11) NOT NULL,
  `notes` text DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `equipment_logs`
--

CREATE TABLE `equipment_logs` (
  `id` int(11) NOT NULL,
  `equipment_id` int(11) NOT NULL,
  `action_type` enum('equipment_created','location_changed','allocation_requested','allocation_approved','allocation_rejected','allocation_returned','decommission_requested','decommission_approved','decommission_rejected','transferred') NOT NULL,
  `changed_by_user_id` int(11) NOT NULL,
  `old_value` text DEFAULT NULL,
  `new_value` text DEFAULT NULL,
  `ip_address` varchar(45) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `equipment_logs`
--

INSERT INTO `equipment_logs` (`id`, `equipment_id`, `action_type`, `changed_by_user_id`, `old_value`, `new_value`, `ip_address`, `created_at`) VALUES
(1, 5, 'equipment_created', 1, NULL, 'Created: EQ002 - Lenovo Thinkbook', NULL, '2026-06-19 13:43:48'),
(2, 5, 'location_changed', 1, 'EQ002 - Lenovo Thinkbook - allocated', 'EQ002 - Lenovo Thinkbook - decommissioned', NULL, '2026-06-19 13:45:49'),
(12, 3, 'allocation_requested', 1, NULL, 'Allocated to Employee ID 1', NULL, '2026-06-22 05:10:47'),
(13, 5, 'location_changed', 1, 'EQ002 - Lenovo Thinkbook - decommissioned', 'EQ002 - Lenovo Thinkbook - in_store', NULL, '2026-06-22 06:41:38'),
(14, 3, 'allocation_approved', 1, 'pending', 'approved', NULL, '2026-06-22 06:41:55'),
(16, 3, 'location_changed', 1, 'EQ001 - Dell Latitude - allocated', 'EQ001 - Dell Latitude - under_repair', NULL, '2026-06-23 23:55:44'),
(17, 3, 'location_changed', 1, 'EQ001 - Dell Latitude - under_repair', 'EQ001 - Dell Latitude - decommissioned', NULL, '2026-06-23 23:55:58'),
(18, 21, 'equipment_created', 1, NULL, 'Created: EQ003 - MacBook', NULL, '2026-06-23 23:57:09'),
(19, 21, 'location_changed', 1, 'EQ003 - MacBook - allocated', 'EQ003 - MacBook - in_store', NULL, '2026-06-23 23:57:54'),
(20, 21, 'allocation_requested', 1, NULL, 'Allocated to Employee ID 4', NULL, '2026-06-23 23:58:07'),
(21, 21, 'allocation_approved', 1, 'pending', 'approved', NULL, '2026-06-23 23:58:16'),
(22, 21, 'allocation_returned', 1, 'allocated', 'in_store', NULL, '2026-06-23 23:58:50'),
(23, 3, 'location_changed', 1, 'EQ001 - Dell Latitude - decommissioned', 'EQ001 - Dell Latitude - under_repair', NULL, '2026-06-24 00:07:39'),
(24, 22, 'equipment_created', 1, NULL, 'Created: EQ004 - Dell EliteBook', NULL, '2026-06-24 00:09:16'),
(25, 3, 'location_changed', 1, 'EQ001 - Dell Latitude - under_repair', 'EQ001 - Dell Latitude - allocated', NULL, '2026-06-24 00:09:28'),
(26, 3, 'location_changed', 1, 'EQ001 - Dell Latitude - allocated', 'EQ001 - Dell Latitude - in_store', NULL, '2026-06-24 00:09:36'),
(27, 3, 'location_changed', 1, 'EQ001 - Dell Latitude - in_store', 'EQ001 - Dell Latitude - in_store', NULL, '2026-06-24 01:31:56'),
(28, 3, 'location_changed', 1, 'EQ001 - Dell Latitude - in_store', 'EQ001 - Dell Latitude - in_store', NULL, '2026-06-24 01:32:06'),
(29, 3, 'transferred', 1, 'Employee 1', 'Employee 5', NULL, '2026-06-24 01:33:25'),
(30, 22, 'decommission_approved', 1, 'in_store', 'Damaged — Fahad Broke it', NULL, '2026-06-24 01:33:49'),
(31, 22, 'location_changed', 1, 'EQ004 - Dell EliteBook - decommissioned', 'EQ004 - Dell EliteBook - in_store', NULL, '2026-06-24 04:25:13');

-- --------------------------------------------------------

--
-- Table structure for table `locations`
--

CREATE TABLE `locations` (
  `id` int(11) NOT NULL,
  `location_name` varchar(100) NOT NULL COMMENT 'Lab, Kitchen, Server Room, Store',
  `building` varchar(100) DEFAULT NULL,
  `room_number` varchar(50) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `locations`
--

INSERT INTO `locations` (`id`, `location_name`, `building`, `room_number`, `created_at`) VALUES
(1, 'Main Store', 'Evacuee Trust Complex', '108', '2026-06-10 10:12:31'),
(3, 'QuantumRonics', 'Evacuee Trust Complex', '109', '2026-06-24 05:04:37'),
(4, 'Common Criteria PK', 'Evacuee Trust Complex', '110', '2026-06-24 05:05:11'),
(5, 'Quantum Communications', 'Evacuee', '111', '2026-06-24 05:05:33');

-- --------------------------------------------------------

--
-- Table structure for table `procurement_types`
--

CREATE TABLE `procurement_types` (
  `id` int(11) NOT NULL,
  `type_name` varchar(50) NOT NULL COMMENT 'Tender, Direct_Purchase, Loan, Donation',
  `created_at` timestamp NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `procurement_types`
--

INSERT INTO `procurement_types` (`id`, `type_name`, `created_at`) VALUES
(1, 'Purchased', '2026-06-10 10:12:39');

-- --------------------------------------------------------

--
-- Table structure for table `roles`
--

CREATE TABLE `roles` (
  `id` int(11) NOT NULL,
  `role_name` varchar(50) NOT NULL COMMENT 'Admin, Operator',
  `description` text DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `roles`
--

INSERT INTO `roles` (`id`, `role_name`, `description`, `created_at`) VALUES
(1, 'Admin', 'Full system access', '2026-06-22 07:35:03'),
(2, 'Operator', 'Standard day-to-day access', '2026-06-22 07:35:03');

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `id` int(11) NOT NULL,
  `username` varchar(100) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `email` varchar(255) DEFAULT NULL,
  `full_name` varchar(255) NOT NULL,
  `is_active` tinyint(1) DEFAULT 1,
  `created_at` timestamp NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `users`
--

INSERT INTO `users` (`id`, `username`, `password_hash`, `email`, `full_name`, `is_active`, `created_at`, `updated_at`) VALUES
(1, 'admin', 'admin123', NULL, 'Administrator', 1, '2026-06-11 09:25:22', '2026-06-11 09:25:22'),
(2, 'operator', 'admin1234', NULL, 'Test Operator', 1, '2026-06-24 05:26:20', '2026-06-24 05:26:20');

-- --------------------------------------------------------

--
-- Table structure for table `user_roles`
--

CREATE TABLE `user_roles` (
  `user_id` int(11) NOT NULL,
  `role_id` int(11) NOT NULL,
  `assigned_at` timestamp NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Dumping data for table `user_roles`
--

INSERT INTO `user_roles` (`user_id`, `role_id`, `assigned_at`) VALUES
(1, 1, '2026-06-22 07:35:50'),
(2, 2, '2026-06-24 05:28:40');

--
-- Indexes for dumped tables
--

--
-- Indexes for table `allocations`
--
ALTER TABLE `allocations`
  ADD PRIMARY KEY (`id`),
  ADD KEY `location_id` (`location_id`),
  ADD KEY `allocated_by_user_id` (`allocated_by_user_id`),
  ADD KEY `approved_by_user_id` (`approved_by_user_id`),
  ADD KEY `previous_allocation_id` (`previous_allocation_id`),
  ADD KEY `idx_allocations_status` (`status`),
  ADD KEY `idx_allocations_equipment` (`equipment_id`),
  ADD KEY `idx_allocations_employee` (`employee_id`),
  ADD KEY `idx_allocations_active` (`equipment_id`,`status`);

--
-- Indexes for table `app_settings`
--
ALTER TABLE `app_settings`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `employees`
--
ALTER TABLE `employees`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `employee_code` (`employee_code`),
  ADD KEY `default_location_id` (`default_location_id`);

--
-- Indexes for table `equipment`
--
ALTER TABLE `equipment`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `equipment_code` (`equipment_code`),
  ADD UNIQUE KEY `qr_code` (`qr_code`),
  ADD KEY `procurement_type_id` (`procurement_type_id`),
  ADD KEY `created_by_user_id` (`created_by_user_id`),
  ADD KEY `idx_equipment_qr` (`qr_code`),
  ADD KEY `idx_equipment_status` (`current_status`),
  ADD KEY `idx_equipment_current_holder` (`current_employee_id`),
  ADD KEY `idx_equipment_current_location` (`current_location_id`),
  ADD KEY `idx_equipment_code` (`equipment_code`);

--
-- Indexes for table `equipment_location_history`
--
ALTER TABLE `equipment_location_history`
  ADD PRIMARY KEY (`id`),
  ADD KEY `location_id` (`location_id`),
  ADD KEY `changed_by_user_id` (`changed_by_user_id`),
  ADD KEY `idx_location_history_active` (`equipment_id`,`end_date`);

--
-- Indexes for table `equipment_logs`
--
ALTER TABLE `equipment_logs`
  ADD PRIMARY KEY (`id`),
  ADD KEY `changed_by_user_id` (`changed_by_user_id`),
  ADD KEY `idx_logs_equipment` (`equipment_id`),
  ADD KEY `idx_logs_created` (`created_at`);

--
-- Indexes for table `locations`
--
ALTER TABLE `locations`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `procurement_types`
--
ALTER TABLE `procurement_types`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `type_name` (`type_name`);

--
-- Indexes for table `roles`
--
ALTER TABLE `roles`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `role_name` (`role_name`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `username` (`username`),
  ADD UNIQUE KEY `email` (`email`);

--
-- Indexes for table `user_roles`
--
ALTER TABLE `user_roles`
  ADD PRIMARY KEY (`user_id`,`role_id`),
  ADD KEY `role_id` (`role_id`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `allocations`
--
ALTER TABLE `allocations`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT for table `app_settings`
--
ALTER TABLE `app_settings`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT for table `employees`
--
ALTER TABLE `employees`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT for table `equipment`
--
ALTER TABLE `equipment`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=23;

--
-- AUTO_INCREMENT for table `equipment_location_history`
--
ALTER TABLE `equipment_location_history`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `equipment_logs`
--
ALTER TABLE `equipment_logs`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=32;

--
-- AUTO_INCREMENT for table `locations`
--
ALTER TABLE `locations`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT for table `procurement_types`
--
ALTER TABLE `procurement_types`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT for table `roles`
--
ALTER TABLE `roles`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT for table `users`
--
ALTER TABLE `users`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `allocations`
--
ALTER TABLE `allocations`
  ADD CONSTRAINT `allocations_ibfk_1` FOREIGN KEY (`equipment_id`) REFERENCES `equipment` (`id`),
  ADD CONSTRAINT `allocations_ibfk_2` FOREIGN KEY (`employee_id`) REFERENCES `employees` (`id`),
  ADD CONSTRAINT `allocations_ibfk_3` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`),
  ADD CONSTRAINT `allocations_ibfk_4` FOREIGN KEY (`allocated_by_user_id`) REFERENCES `users` (`id`),
  ADD CONSTRAINT `allocations_ibfk_5` FOREIGN KEY (`approved_by_user_id`) REFERENCES `users` (`id`),
  ADD CONSTRAINT `allocations_ibfk_6` FOREIGN KEY (`previous_allocation_id`) REFERENCES `allocations` (`id`);

--
-- Constraints for table `employees`
--
ALTER TABLE `employees`
  ADD CONSTRAINT `employees_ibfk_1` FOREIGN KEY (`default_location_id`) REFERENCES `locations` (`id`);

--
-- Constraints for table `equipment`
--
ALTER TABLE `equipment`
  ADD CONSTRAINT `equipment_ibfk_1` FOREIGN KEY (`procurement_type_id`) REFERENCES `procurement_types` (`id`),
  ADD CONSTRAINT `equipment_ibfk_2` FOREIGN KEY (`created_by_user_id`) REFERENCES `users` (`id`),
  ADD CONSTRAINT `equipment_ibfk_3` FOREIGN KEY (`current_location_id`) REFERENCES `locations` (`id`),
  ADD CONSTRAINT `equipment_ibfk_4` FOREIGN KEY (`current_employee_id`) REFERENCES `employees` (`id`);

--
-- Constraints for table `equipment_location_history`
--
ALTER TABLE `equipment_location_history`
  ADD CONSTRAINT `equipment_location_history_ibfk_1` FOREIGN KEY (`equipment_id`) REFERENCES `equipment` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `equipment_location_history_ibfk_2` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`),
  ADD CONSTRAINT `equipment_location_history_ibfk_3` FOREIGN KEY (`changed_by_user_id`) REFERENCES `users` (`id`);

--
-- Constraints for table `equipment_logs`
--
ALTER TABLE `equipment_logs`
  ADD CONSTRAINT `equipment_logs_ibfk_1` FOREIGN KEY (`equipment_id`) REFERENCES `equipment` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `equipment_logs_ibfk_2` FOREIGN KEY (`changed_by_user_id`) REFERENCES `users` (`id`);

--
-- Constraints for table `user_roles`
--
ALTER TABLE `user_roles`
  ADD CONSTRAINT `user_roles_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `user_roles_ibfk_2` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
