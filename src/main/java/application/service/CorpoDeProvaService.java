package application.service;

import java.util.Date;
import java.util.List;
import java.util.TimeZone;

import application.dao.CorpoDeProvaDao;
import application.dao.DaoFactory;
import application.domaim.CorpoDeProva;

public class CorpoDeProvaService {

	private CorpoDeProvaDao dao = DaoFactory.createCorpoDeProvaDao();

	public List<CorpoDeProva> findAll() {
		return dao.findAll();
	}

	public void saveOrUpdate(CorpoDeProva obj) {
		if (obj.getId() == null) {
			dao.insert(obj);
		} else {
			dao.update(obj);
		}
	}

	public void deleteById(Integer id) {
		dao.deleteById(id);
	}

	public CorpoDeProva findById(Integer id) {
		return dao.findById(id);
	}

	public List<CorpoDeProva> findByCompresionTestId(Integer id) {
		return dao.findByCompresionTestId(id);
	}
	
	public List<CorpoDeProva> findByClientId(Integer id) {
		return dao.findByClientId(id);
	}

	public List<CorpoDeProva> findByCompresionTestIdWithTimeZone(Integer id, TimeZone tZ) {
		return dao.findByCompresionTestIdWithTimeZone(id, tZ);
	}

	public Integer countCorpoDeProvasToTestbyCompresionTestId(Integer id) {
		return dao.countCorpoDeProvasToTestbyCompresionTestId(id);
	}

	public List<CorpoDeProva> findByDatesAndCompresionTestId(TimeZone tZ, Date initialDate, Date finalDate,
			Integer compresionTestId) {
		return dao.findByDatesAndCompresionTestId(tZ, initialDate, finalDate, compresionTestId);
	}
	public List<CorpoDeProva> findLateCorpoDeProva(TimeZone tZ) {
		return dao.findLateCorpoDeProva(tZ);
	}
}
