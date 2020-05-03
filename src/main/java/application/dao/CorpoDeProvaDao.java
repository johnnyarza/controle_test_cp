package application.dao;

import java.util.Date;
import java.util.List;
import java.util.TimeZone;

import application.domaim.CorpoDeProva;

public interface CorpoDeProvaDao {
	
	void insert(CorpoDeProva obj);
	void update(CorpoDeProva obj);
	void deleteById(Integer id);
	CorpoDeProva findById(Integer id);
	List<CorpoDeProva> findAll();
	List<CorpoDeProva> findByCompresionTestId(Integer id);
	List<CorpoDeProva> findByCompresionTestIdWithTimeZone(Integer id, TimeZone tZ);
	List<CorpoDeProva> findByDatesAndCompresionTestId(TimeZone tZ,Date initialDate, Date finalDate,Integer compresionTestId);
	Integer countCorpoDeProvasToTestbyCompresionTestId(Integer id);

}
