package application.dao;

import application.dao.impl.ClientDaoJDBC;
import application.dao.impl.CompresionTestDaoJDBC;
import application.dao.impl.CompresionTestListJDBC;
import application.dao.impl.ConcreteDesignDaoJDBC;
import application.dao.impl.CorpoDeProvaJDBC;
import application.dao.impl.MaterialDaoJDBC;
import application.dao.impl.ProviderDaoJDBC;
import application.db.DB;

public class DaoFactory {

	public static ClientDao createClientDao() {
		ClientDao obj = new ClientDaoJDBC(DB.getConnection());
		return obj;
	}

	public static CompresionTestDao createCompresionTestDao() {
		return new CompresionTestDaoJDBC(DB.getConnection());
	}

	public static CorpoDeProvaDao createCorpoDeProvaDao() {
		return new CorpoDeProvaJDBC(DB.getConnection());
	}

	public static CompresionTestListDao createCompresionTestListDao() {
		return new CompresionTestListJDBC(DB.getConnection());
	}

	public static ProviderDao createProviderDao() {
		return new ProviderDaoJDBC(DB.getConnection());
	}

	public static MaterialDao createMaterialDao() {
		return new MaterialDaoJDBC(DB.getConnection());
	}

	public static ConcreteDesignDao ConcreteDesignlDao() {
		return new ConcreteDesignDaoJDBC(DB.getConnection());
	}

}
