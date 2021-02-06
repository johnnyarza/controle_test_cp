package application.service;

import java.sql.SQLException;
import java.util.List;
import java.util.TimeZone;

import application.dao.ClientDao;
import application.dao.DaoFactory;
import application.db.DbException;
import application.domaim.Cliente;

public class ClientService {
	private ClientDao dao;
	
	
	public ClientService () throws SQLException {
		this.dao = DaoFactory.createClientDao();
	};
	
	CompresionTestService compresionTestService = new CompresionTestService();
	
	public List<Cliente> findAll() {
		return dao.findAll();
	}
	
	public void saveOrUpdate(Cliente obj) {
		if(obj.getId() == null) {
			if (findByName(obj.getName()) != null) {
				throw new DbException("El cliente: " + obj.getName() + " ya existe");
			}
			dao.insert(obj);
		}
		else {
			dao.update(obj);
		}
	}
	
	public void deleteById(Integer id) {
		if (compresionTestService.findByClientId(id, TimeZone.getDefault()).size() > 0) {
			throw new DbException("Existen documentos que utilizan este cliente");
		};
		if (compresionTestService.findByConcreteProviderId(id, TimeZone.getDefault()).size() > 0) {
			throw new DbException("Existen documentos que utilizan este cliente como proveedor de hormigón");
		}
		dao.deleteById(id);
	}
	
	public Cliente findById (Integer id) {	
		return dao.findById(id);
	}
	
	public List<Cliente> findByNameLike(String str) {
		return dao.findByLikeName(str);
	}
	
	public Cliente findByName(String str) {
		return dao.findByName(str);
	}
}
