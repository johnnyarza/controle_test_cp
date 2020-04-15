package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.ArrayList;
import java.util.List;

import application.dao.CompresionTestDao;
import application.db.DB;
import application.db.DbException;
import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.service.ClientService;

public class CompresionTestDaoJDBC implements CompresionTestDao{
	
	private Connection conn;
	
	
	public CompresionTestDaoJDBC(Connection conn) {
		this.conn = conn;
	}

	@Override
	public void insert(CompresionTest obj) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("ININSERT INTO compresion_test " 
					+ "(client_id,ConcreteDesign_id,obra,address,creacionDate) " 
					+ "VALUES "
					+ "(?,?,?,?,?)",
					Statement.RETURN_GENERATED_KEYS);
			
			st.setInt(1, obj.getClient().getId());
			st.setInt(2, 1);//TODO IMPLEMENTA CONCRETEDESIGN
			st.setString(3, obj.getObra());
			st.setString(4, obj.getAddress());
			st.setDate(5, new java.sql.Date(obj.getDate().getTime()));
			
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
	public void update(CompresionTest obj) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("UPDATE compresion_test SET "
					+ "compresion_test.client_id = ?, " 
					+ "compresion_test.ConcreteDesign_id = ?, " 
					+"compresion_test.obra = ?, " 
					+"compresion_test.address = ?, " 
					+"compresion_test.creacionDate = ? " 
					+"WHERE compresion_test.id = ?");
			
			st.setInt(1, obj.getClient().getId());
			st.setInt(2, 1);//TODO implements design id
			st.setString(3, obj.getObra());
			st.setString(4, obj.getAddress());
			st.setDate(5, new java.sql.Date(obj.getDate().getTime()));
			st.setInt(6, obj.getId());
			
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
	public CompresionTest findById(Integer id) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("SELECT * FROM compresion_test where compresion_test.id = ?");
			
			st.setInt(1, id);
			rs = st.executeQuery();
			
			Cliente client = new Cliente();
			ClientService clientService = new ClientService();
			if (rs.next()) {
				client = clientService.findById(rs.getInt(0));
				return new CompresionTest(rs.getInt(0), client, rs.getString(3), rs.getString(4), rs.getDate(5));
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
	public List<CompresionTest> findAll() {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("SELECT * FROM clients");
			
			rs = st.executeQuery();
			
			Cliente client = new Cliente();
			ClientService clientService = new ClientService();
			List<CompresionTest> list = new ArrayList<>();
			while (rs.next()) {
				client = clientService.findById(rs.getInt(0));
				list.add(new CompresionTest(rs.getInt(0), client, rs.getString(3), rs.getString(4), rs.getDate(5)));
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
