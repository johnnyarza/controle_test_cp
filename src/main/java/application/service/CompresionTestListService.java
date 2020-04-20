package application.service;

import java.util.List;

import application.dao.CompresionTestListDao;
import application.dao.DaoFactory;
import application.domaim.CompresionTestList;

public class CompresionTestListService {
	
	CompresionTestListDao dao = DaoFactory.createCompresionTestListDao();
	
	public List<CompresionTestList> findAll() {
		return dao.findAll();
	}

}
