package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.SQLException;

import application.dao.MigrationDao;
import application.db.DB;
import application.db.DbException;

public class MigrationDaoJDBC implements MigrationDao {

	private Connection conn;

	public MigrationDaoJDBC(Connection connection) {
		super();
		this.conn = connection;
	}

	@Override
	public void schemaMigration() {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("CREATE DATABASE  IF NOT EXISTS `cp_db`");
			st.executeUpdate();
			st.close();
			st = conn.prepareStatement("USE `cp_db`");
			st.executeUpdate();
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}
	}

	@Override
	public void clientsTableMigration() {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("CREATE TABLE IF NOT EXISTS `clients` (" + 
					"  `id` int NOT NULL AUTO_INCREMENT," + 
					"  `name` varchar(45) DEFAULT NULL," + 
					"  `phone` varchar(45) DEFAULT NULL," + 
					"  `address` varchar(45) DEFAULT NULL," + 
					"  `email` varchar(45) DEFAULT NULL," + 
					"  PRIMARY KEY (`id`)," + 
					"  UNIQUE KEY `name_UNIQUE` (`name`)" + 
					") ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci");

			st.executeUpdate();
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}

	}

	@Override
	public void materialsMigration() {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("CREATE TABLE IF NOT EXISTS `materials` (" + 
					"  `id` int NOT NULL AUTO_INCREMENT," + 
					"  `name` varchar(45) DEFAULT NULL," + 
					"  `providerId` int DEFAULT NULL," + 
					"  PRIMARY KEY (`id`)," + 
					"  KEY `PK_providersId_idx` (`providerId`)," + 
					"  CONSTRAINT `PK_providersId` FOREIGN KEY (`providerId`) REFERENCES `providers` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE" + 
					") ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;");

			st.executeUpdate();
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}
		
	}

	@Override
	public void providersMigration() {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("CREATE TABLE IF NOT EXISTS `providers` (" + 
					"  `id` int NOT NULL AUTO_INCREMENT," + 
					"  `name` varchar(45) DEFAULT NULL," + 
					"  `phone` varchar(45) DEFAULT NULL," + 
					"  `address` varchar(45) DEFAULT NULL," + 
					"  `email` varchar(45) DEFAULT NULL," + 
					"  PRIMARY KEY (`id`)," + 
					"  UNIQUE KEY `name_UNIQUE` (`name`)" + 
					") ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;");

			st.executeUpdate();
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}
		
	}

	@Override
	public void compresionTestMigration() {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("CREATE TABLE IF NOT EXISTS `compresion_test` (" + 
					"  `id` int NOT NULL AUTO_INCREMENT," + 
					"  `client_id` int DEFAULT NULL," + 
					"  `concreteProviderId` int DEFAULT NULL," + 
					"  `ConcreteDesign_id` int DEFAULT NULL," + 
					"  `obra` varchar(45) DEFAULT NULL," + 
					"  `address` varchar(45) DEFAULT NULL," + 
					"  `creacionDate` date DEFAULT NULL," + 
					"  PRIMARY KEY (`id`)," + 
					"  KEY `FK_clientID_compresionTest_idx` (`client_id`)," + 
					"  KEY `FK_concreteDesign_idx` (`ConcreteDesign_id`)," + 
					"  KEY `FK_concreteProviderId_idx` (`concreteProviderId`)," + 
					"  CONSTRAINT `FK_clientID_compresionTest` FOREIGN KEY (`client_id`) REFERENCES `clients` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE," + 
					"  CONSTRAINT `FK_concreteDesign` FOREIGN KEY (`ConcreteDesign_id`) REFERENCES `concretedesign` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE," + 
					"  CONSTRAINT `FK_concreteProviderId` FOREIGN KEY (`concreteProviderId`) REFERENCES `clients` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE" + 
					") ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci");

			st.executeUpdate();
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}
		
	}

	@Override
	public void concreteDesignMigration() {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("CREATE TABLE IF NOT EXISTS `concretedesign` (" + 
					"  `id` int NOT NULL AUTO_INCREMENT," + 
					"  `description` varchar(45) DEFAULT NULL," + 
					"  `fck` double DEFAULT NULL," + 
					"  `mat1_id` int DEFAULT NULL," + 
					"  `mat2_id` int DEFAULT NULL," + 
					"  `mat3_id` int DEFAULT NULL," + 
					"  `mat4_id` int DEFAULT NULL," + 
					"  `mat5_id` int DEFAULT NULL," + 
					"  `mat6_id` int DEFAULT NULL," + 
					"  `mat7_id` int DEFAULT NULL," + 
					"  `mat8_id` int DEFAULT NULL," + 
					"  `mat1_qtt` double DEFAULT NULL," + 
					"  `mat2_qtt` double DEFAULT NULL," + 
					"  `mat3_qtt` double DEFAULT NULL," + 
					"  `mat4_qtt` double DEFAULT NULL," + 
					"  `mat5_qtt` double DEFAULT NULL," + 
					"  `mat6_qtt` double DEFAULT NULL," + 
					"  `mat7_qtt` double DEFAULT NULL," + 
					"  `mat8_qtt` double DEFAULT NULL," + 
					"  `slump` double DEFAULT NULL," + 
					"  PRIMARY KEY (`id`)," + 
					"  UNIQUE KEY `description_UNIQUE` (`description`)," + 
					"  KEY `FK_MAT_idx` (`mat1_id`)," + 
					"  KEY `FK_MAT2_idx` (`mat2_id`)," + 
					"  KEY `FK_MAT3_idx` (`mat3_id`)," + 
					"  KEY `FK_MAT4_idx` (`mat4_id`)," + 
					"  KEY `FK_MAT5_idx` (`mat5_id`)," + 
					"  KEY `FK_MAT6_idx` (`mat6_id`)," + 
					"  KEY `FK_MAT7_idx` (`mat7_id`)," + 
					"  KEY `FK_MAT8_idx` (`mat8_id`)," + 
					"  CONSTRAINT `FK_MAT` FOREIGN KEY (`mat1_id`) REFERENCES `materials` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE," + 
					"  CONSTRAINT `FK_MAT2` FOREIGN KEY (`mat2_id`) REFERENCES `materials` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE," + 
					"  CONSTRAINT `FK_MAT3` FOREIGN KEY (`mat3_id`) REFERENCES `materials` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE," + 
					"  CONSTRAINT `FK_MAT4` FOREIGN KEY (`mat4_id`) REFERENCES `materials` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE," + 
					"  CONSTRAINT `FK_MAT5` FOREIGN KEY (`mat5_id`) REFERENCES `materials` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE," + 
					"  CONSTRAINT `FK_MAT6` FOREIGN KEY (`mat6_id`) REFERENCES `materials` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE," + 
					"  CONSTRAINT `FK_MAT7` FOREIGN KEY (`mat7_id`) REFERENCES `materials` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE," + 
					"  CONSTRAINT `FK_MAT8` FOREIGN KEY (`mat8_id`) REFERENCES `materials` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE" + 
					") ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;");

			st.executeUpdate();
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}
		
	}

	@Override
	public void corpoDeProvaMigration() {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("CREATE TABLE IF NOT EXISTS `corpo_de_provas` (" + 
					"  `id` int NOT NULL AUTO_INCREMENT," + 
					"  `code` varchar(45) DEFAULT NULL," + 
					"  `client_id` int DEFAULT NULL," + 
					"  `compresionTest_Id` int DEFAULT NULL," + 
					"  `slump` decimal(10,2) DEFAULT NULL," + 
					"  `dateMolde` date DEFAULT NULL," + 
					"  `ruptureDate` date DEFAULT NULL," + 
					"  `age` int DEFAULT NULL," + 
					"  `diameter` decimal(10,2) DEFAULT NULL," + 
					"  `height` decimal(10,2) DEFAULT NULL," + 
					"  `weight` decimal(10,2) DEFAULT NULL," + 
					"  `tonRupture` decimal(10,2) DEFAULT NULL," + 
					"  `is_locked` TINYINT NOT NULL DEFAULT 0, " +
					"  PRIMARY KEY (`id`)," + 
					"  KEY `FK_clientID_CpID_idx` (`client_id`)," + 
					"  KEY `FK_compresionTestId_CP_idx` (`compresionTest_Id`)," + 
					"  CONSTRAINT `FK_clientID_CpID` FOREIGN KEY (`client_id`) REFERENCES `compresion_test` (`client_id`) ON DELETE CASCADE ON UPDATE CASCADE," + 
					"  CONSTRAINT `FK_compresionTestId_CP` FOREIGN KEY (`compresionTest_Id`) REFERENCES `compresion_test` (`id`) ON DELETE CASCADE ON UPDATE CASCADE" + 
					") ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;");

			st.executeUpdate();
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}
		
	}

	@Override
	public void userMigration() {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("CREATE TABLE IF NOT EXISTS `users` ( " + 
					"`id` INT NOT NULL AUTO_INCREMENT," + 
					"`name` VARCHAR(45) NOT NULL, " + 
					"`password` VARCHAR(45) NOT NULL," + 
					"`role` VARCHAR(3) NOT NULL DEFAULT 'clt', " +
					"PRIMARY KEY (`id`)," + 
					"UNIQUE KEY `name_UNIQUE` (`name`) ) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci");
			st.executeUpdate();
			
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}
		
	}
	
	@Override
	public void defaultUserMigration() {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("INSERT IGNORE INTO users (name,password,role) VALUES ('admin','admin','adm')");
			st.executeUpdate();
			
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}
		
	}

}
