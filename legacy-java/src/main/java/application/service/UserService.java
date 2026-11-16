package application.service;

import javax.security.auth.login.LoginException;

import application.dao.UserDao;
import application.dao.impl.UserDaoJDBC;
import application.db.DB;
import application.domaim.User;

public class UserService {
	private UserDao dao = new UserDaoJDBC(DB.getConnection());

	public void saveOrInsert(User user) throws LoginException {
		if (user.getId() != null) {
			dao.update(user);
		} else {
			if (findByName(user.getName()) != null) {
				throw new LoginException("Usuario ya existe");
			}
			dao.insert(user);
		}
	}

	public User findById(Integer id) {
		return dao.findById(id);
	}

	public void deleteById(Integer id) throws IllegalAccessError {
		if (findByName("admin").getId() == id) {
			throw new IllegalAccessError("El usuario 'admin' no puede ser apagado");
		}
		dao.deleteById(id);
	}

	public User findByNameAndPassword(User user) {
		return dao.findByNameAndPassword(user);
	}
	
	public User findByName(String name) {
		return dao.findByName(name);
	}
}
