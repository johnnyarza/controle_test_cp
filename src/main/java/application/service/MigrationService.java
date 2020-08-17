package application.service;

import application.dao.DaoFactory;
import application.dao.MigrationDao;

public class MigrationService {
	private MigrationDao dao = DaoFactory.createMigrationDao();
	
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

}
