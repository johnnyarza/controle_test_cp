package application.test;

import java.util.logging.Level;

import application.db.DbException;
import application.log.LogUtils;

public class Test {
	public static void main(String[] args) {
		try {
//			MigrationService service = new MigrationService();
//			service.schemaMigration();
//			service.clientsTableMigration();
//			service.providersMigration();
//			service.materialsMigration();		
//			service.concreteDesignMigration();
//			service.compresionTestMigration();			
//			service.corpoDeProvaMigration();
//		
//			System.out.println("SUcess");
			LogUtils log = new LogUtils();
			log.doLog(Level.WARNING, "Loggin", new DbException("DB erro"));

		} catch (Exception e) {
			e.printStackTrace();
		}

	}

}
