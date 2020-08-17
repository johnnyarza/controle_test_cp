package application.dao;

public interface MigrationDao {
	void schemaMigration();
	void clientsTableMigration();
	void materialsMigration();
	void providersMigration();
	void compresionTestMigration();
	void concreteDesignMigration();
	void corpoDeProvaMigration();

}
