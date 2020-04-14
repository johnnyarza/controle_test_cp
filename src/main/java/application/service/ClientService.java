package application.service;

import java.util.ArrayList;
import java.util.List;

import application.domaim.Cliente;

public class ClientService {
	
	public List<Cliente> findAll() {
		List<Cliente> list = new ArrayList<>();
		list.add(new Cliente(1, "Johnny", "999999", "asdasdasd", "gmai.com"));
		list.add(new Cliente(2, "Erika", "9992229", "assdsdasd", "erika@gmail.com"));
		list.add(new Cliente(3, "Lucia", "111999", "asdasdasd", "lucia@gmai.com"));
		return list;
	}

}
