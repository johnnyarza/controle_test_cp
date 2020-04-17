package application.dao;

import application.dao.impl.ClientDaoJDBC;
import application.dao.impl.CompresionTestDaoJDBC;
import application.dao.impl.CorpoDeProvaJDBC;
import application.db.DB;

public class DaoFactory {
	
	public static ClientDao createClientDao() {
		return new ClientDaoJDBC(DB.getConnection());
	}
	
	public static CompresionTestDao createCompresionTestDao() {
		return new CompresionTestDaoJDBC(DB.getConnection());
	}
	
	public static CorpoDeProvaDao createCorpoDeProvaDao() {
		return new CorpoDeProvaJDBC(DB.getConnection());
	}

}
