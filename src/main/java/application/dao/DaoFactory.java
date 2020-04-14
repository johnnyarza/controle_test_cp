package application.dao;

import application.dao.impl.ClientDaoJDBC;
import application.db.DB;

public class DaoFactory {
	
	public static ClientDao createClientDao() {
		return new ClientDaoJDBC(DB.getConnection());
	}

}
