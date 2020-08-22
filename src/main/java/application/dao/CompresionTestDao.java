package application.dao;

import java.util.List;
import java.util.TimeZone;

import application.domaim.CompresionTest;

public interface CompresionTestDao {
	
	void insert(CompresionTest obj);
	void update(CompresionTest obj);
	void deleteById(Integer id);
	CompresionTest findById(Integer id);	
	CompresionTest findByIdWithTimeZone(Integer id,TimeZone tZ);
	List<CompresionTest> findyByClientId(Integer id,TimeZone tZ);
	List<CompresionTest> findyByConcreteProviderId(Integer id,TimeZone tZ);
	List<CompresionTest> findAll();
	List<CompresionTest> findByConcreteDesignId(TimeZone tZ,Integer id);
	Boolean compresionTestContainsConcreteDesingId(Integer concreteDesignId);
	List<CompresionTest> findByConcreteDesingId(Integer concreteDesignId); 

}
