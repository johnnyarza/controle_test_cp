package application.dao;

import java.sql.SQLException;

import application.dao.impl.ClientDaoJDBC;
import application.dao.impl.CompresionTestDaoJDBC;
import application.dao.impl.CompresionTestListJDBC;
import application.dao.impl.ConcreteDesignDaoJDBC;
import application.dao.impl.CorpoDeProvaJDBC;
import application.dao.impl.MaterialDaoJDBC;
import application.dao.impl.MigrationDaoJDBC;
import application.dao.impl.ProviderDaoJDBC;
import application.db.DB;

public class DaoFactory {

	public static ClientDao createClientDao() throws SQLException {
		ClientDao obj = new ClientDaoJDBC(DB.getConnection());
		return obj;
	}

	public static CompresionTestDao createCompresionTestDao() throws SQLException {
		return new CompresionTestDaoJDBC(DB.getConnection());
	}

	public static CorpoDeProvaDao createCorpoDeProvaDao() throws SQLException {
		return new CorpoDeProvaJDBC(DB.getConnection());
	}

	public static CompresionTestListDao createCompresionTestListDao() throws SQLException {
		return new CompresionTestListJDBC(DB.getConnection());
	}

	public static ProviderDao createProviderDao() throws SQLException {
		return new ProviderDaoJDBC(DB.getConnection());
	}

	public static MaterialDao createMaterialDao() throws SQLException {
		return new MaterialDaoJDBC(DB.getConnection());
	}

	public static ConcreteDesignDao ConcreteDesignlDao() throws SQLException {
		return new ConcreteDesignDaoJDBC(DB.getConnection());
	}
	
	public static MigrationDao createMigrationDao() throws SQLException {
		return new MigrationDaoJDBC(DB.getConnection());
	}

}
