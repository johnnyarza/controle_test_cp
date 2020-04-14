package application.service;

import java.util.List;

import application.dao.ClientDao;
import application.dao.DaoFactory;
import application.domaim.Cliente;

public class ClientService {
	
	private ClientDao dao = DaoFactory.createClientDao();
	
	public List<Cliente> findAll() {
		return dao.findAll();
	}
	
	public void saveOrUpdate(Cliente obj) {
		if(obj.getId() == null) {
			dao.insert(obj);
		}
		else {
			dao.update(obj);
		}
	}
}
