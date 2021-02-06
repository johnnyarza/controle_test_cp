package application.service;

import java.sql.SQLException;

import application.dao.DaoFactory;
import application.dao.MigrationDao;

public class MigrationService {
	private MigrationDao dao;

	public MigrationService() throws SQLException {
		this.dao = DaoFactory.createMigrationDao();
	};

	public void schemaMigration() {
		dao.schemaMigration();
	}

	public void clientsTableMigration() {
		dao.clientsTableMigration();
	}

	public void materialsMigration() {
		dao.materialsMigration();
	}

	public void providersMigration() {
		dao.providersMigration();
	}

	public void compresionTestMigration() {
		dao.compresionTestMigration();
	}

	public void concreteDesignMigration() {
		dao.concreteDesignMigration();
	}

	public void corpoDeProvaMigration() {
		dao.corpoDeProvaMigration();
	}

	public void userMigration() {
		dao.userMigration();
		dao.defaultUserMigration();
	}

	public void initiateDB() {
		schemaMigration();
		clientsTableMigration();
		providersMigration();
		materialsMigration();
		concreteDesignMigration();
		compresionTestMigration();
		corpoDeProvaMigration();
		userMigration();
	}

}
