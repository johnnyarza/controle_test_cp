package application.service;

import java.util.List;

import application.dao.CorpoDeProvaDao;
import application.dao.DaoFactory;
import application.domaim.CorpoDeProva;

public class CorpoDeProvaService {
	
	private CorpoDeProvaDao dao = DaoFactory.createCorpoDeProvaDao();
	
	public List<CorpoDeProva> findAll() {
		return dao.findAll();
	}
	
	public void saveOrUpdate(CorpoDeProva obj) {
		if(obj.getId() == null) {
			dao.insert(obj);
		}
		else {
			dao.update(obj);
		}
	}
	
	public void deleteById(Integer id) {
		dao.deleteById(id);
	}
	
	public CorpoDeProva findById (Integer id) {	
		return dao.findById(id);
	}
	
	public List<CorpoDeProva> findByCompresionTestId(Integer id) {
		return dao.findByCompresionTestId(id);
	}
}
