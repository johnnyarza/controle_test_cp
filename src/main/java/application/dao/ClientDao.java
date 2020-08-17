package application.dao;

import java.util.List;

import application.domaim.Cliente;

public interface ClientDao {
	
	void insert(Cliente obj);
	void update(Cliente obj);
	void deleteById(Integer id);
	Cliente findById(Integer id);
	List<Cliente> findAll();
	List<Cliente> findByLikeName(String str);
	Cliente findByName(String name);

}
