package application.dao;

import java.util.List;
import java.util.TimeZone;

import application.domaim.CompresionTest;

public interface CompresionTestDao {
	
	void insert(CompresionTest obj);
	void update(CompresionTest obj);
	void deleteById(Integer id);
	CompresionTest findById(Integer id);
	Boolean compresionTestContainsConcreteDesingId(Integer concreteDesignId); 
	CompresionTest findByIdWithTimeZone(Integer id,TimeZone tZ);
	List<CompresionTest> findAll();
	CompresionTest findByConcreteDesignId(Integer id);

}
