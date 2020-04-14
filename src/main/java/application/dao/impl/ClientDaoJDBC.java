package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.ArrayList;
import java.util.List;

import application.dao.ClientDao;
import application.db.DB;
import application.db.DbException;
import application.domaim.Cliente;

public class ClientDaoJDBC implements ClientDao{
	
	private Connection conn;
	
	
	public ClientDaoJDBC(Connection conn) {
		this.conn = conn;
	}

	@Override
	public void insert(Cliente obj) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("INSERT INTO clients "
					+"(name,phone,address,email) "
					+"VALUES "
					+"(?,?,?,?)",
					Statement.RETURN_GENERATED_KEYS);
			
			st.setString(1, obj.getName());
			st.setString(2, obj.getPhone());
			st.setString(3, obj.getAddress());
			st.setString(4, obj.getEmail());
			
			int rowsAffected = st.executeUpdate();
			
			if (rowsAffected > 0) {
				ResultSet rs = st.getGeneratedKeys();
				if (rs.next()) {
					int id = rs.getInt(1);
					obj.setId(id);
				}
				DB.closeResultSet(rs);
			} else {
				throw new DbException("Unexpected Error. No rows affected");
			}
		} catch (SQLException e) {
			throw new DbException(e.getMessage());
		} finally {
			DB.closeStatement(st);
		}		
	}

	@Override
	public void update(Cliente obj) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("update clients set "
					+ "clients.name = ?, "
					+ "clients.phone =?, "
					+ "clients.address = ?, "
					+ "clients.email = ? "
					+ "WHERE clients.id = ?");
			
			st.setString(1, obj.getName());
			st.setString(2, obj.getPhone());
			st.setString(3, obj.getAddress());
			st.setString(4, obj.getEmail());
			st.setInt(4, obj.getId());
			
			st.executeQuery();

		} 
		catch (SQLException e) {
			throw new DbException(e.getMessage());
		} 
		finally {
			DB.closeStatement(st);
		}	
	}

	@Override
	public void deleteById(Integer id) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("delete from clients where clients.id = ?");
			
			st.setInt(1, id);
			
			st.executeUpdate();

		} 
		catch (SQLException e) {
			throw new DbException(e.getMessage());
		} 
		finally {
			DB.closeStatement(st);
		}	
	}

	@Override
	public Cliente findById(Integer id) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("SELECT * FROM clients where clients.id = ?");
			
			st.setInt(1, id);
			rs = st.executeQuery();
			
			if (rs.next()) {
				return new Cliente(rs.getInt(0), rs.getString(1), rs.getString(2), rs.getString(3), rs.getString(4));
			}
			return null;
		}
		catch (SQLException e) {
			throw new DbException(e.getMessage());
		}
		finally {
			DB.closeStatement(st);
			DB.closeResultSet(rs);
		}	
	}
	@Override
	public List<Cliente> findAll() {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("SELECT * FROM clients");
			
			rs = st.executeQuery();
			
			List<Cliente> list = new ArrayList<>();
			while (rs.next()) {
				list.add(new Cliente(rs.getInt(0), rs.getString(1), rs.getString(2), rs.getString(3), rs.getString(4)));
			}
			return list;
		}
		catch (SQLException e) {
			throw new DbException(e.getMessage());
		}
		finally {
			DB.closeStatement(st);
			DB.closeResultSet(rs);
		}	
	}
}
