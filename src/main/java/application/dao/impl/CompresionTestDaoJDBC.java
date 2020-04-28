package application.dao.impl;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.List;
import java.util.TimeZone;

import application.dao.CompresionTestDao;
import application.db.DB;
import application.db.DbException;
import application.domaim.Cliente;
import application.domaim.CompresionTest;
import application.service.ClientService;
import application.service.ConcreteDesignService;

public class CompresionTestDaoJDBC implements CompresionTestDao{
	
	private Connection conn;
	
	
	public CompresionTestDaoJDBC(Connection conn) {
		this.conn = conn;
	}

	@Override
	public void insert(CompresionTest obj) {
		PreparedStatement st = null;
		try {
			st = conn.prepareStatement("INSERT INTO compresion_test " 
					+ "(client_id,ConcreteDesign_id,obra,address,creacionDate) " 
					+ "VALUES "
					+ "(?,?,?,?,?)",
					Statement.RETURN_GENERATED_KEYS);
			
			st.setInt(1, obj.getClient().getId());
			st.setInt(2, obj.getConcreteDesign().getId());
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
			st.setInt(2, obj.getConcreteDesign().getId());
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
			st = conn.prepareStatement("delete from compresion_test where id = ?");
			
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
			ConcreteDesignService concreteDesignService = new ConcreteDesignService();
			if (rs.next()) {
				client = clientService.findById(rs.getInt(2));
				CompresionTest compresionTest = new CompresionTest();
				
				compresionTest.setId(rs.getInt(1));
				compresionTest.setClient(client);
				compresionTest.setConcreteDesign(concreteDesignService.findConcreteDesignById(rs.getInt(3)));
				compresionTest.setObra(rs.getString(4));
				compresionTest.setAddress(rs.getString(5));
				compresionTest.setDate(new java.util.Date(rs.getTimestamp(6).getTime()));
				
				return compresionTest;
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
			st = conn.prepareStatement("SELECT * FROM compresion_test");
			
			rs = st.executeQuery();
			
			Calendar cal = Calendar.getInstance(TimeZone.getDefault());
			Cliente client = new Cliente();
			ClientService clientService = new ClientService();
			ConcreteDesignService concreteDesignService = new ConcreteDesignService();
			
			List<CompresionTest> list = new ArrayList<>();
			while (rs.next()) {
				client = clientService.findById(rs.getInt(2));
				list.add(
						new CompresionTest(
								rs.getInt(1), client, concreteDesignService.findConcreteDesignById(rs.getInt(3)),
								rs.getString(4), rs.getString(5), new java.util.Date(rs.getTimestamp(6,cal).getTime())));
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

	@Override
	public CompresionTest findByIdWithTimeZone(Integer id, TimeZone tZ) {
		PreparedStatement st = null;
		ResultSet rs = null;
		try {
			st = conn.prepareStatement("SELECT * FROM compresion_test where compresion_test.id = ?");
			
			st.setInt(1, id);
			rs = st.executeQuery();
			
			Calendar cal = Calendar.getInstance(TimeZone.getDefault());
			Cliente client = new Cliente();
			ClientService clientService = new ClientService();
			ConcreteDesignService concreteDesignService = new ConcreteDesignService();
			if (rs.next()) {
				client = clientService.findById(rs.getInt(2));
				
				CompresionTest compresionTest = new CompresionTest();
				
				compresionTest.setId(rs.getInt(1));
				compresionTest.setClient(client);
				compresionTest.setConcreteDesign(concreteDesignService.findConcreteDesignById(rs.getInt(3)));
				compresionTest.setObra(rs.getString(4));
				compresionTest.setAddress(rs.getString(5));
				compresionTest.setDate(new java.util.Date(rs.getTimestamp(6,cal).getTime()));
				
				return compresionTest;
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
	public CompresionTest findByConcreteDesignId(Integer id) {
		// TODO Implementar
		return null;
	}

	@Override
	public Boolean compresionTestContainsConcreteDesingId(Integer concreteDesignId) {
		PreparedStatement st = null;
		ResultSet rs = null;
		Boolean result = null;
		try {
			st = conn.prepareStatement(
					"SELECT COUNT(id) FROM compresion_test WHERE ConcreteDesign_id = ?");
			st.setInt(1, concreteDesignId);
			
			rs = st.executeQuery();
			if (rs.next()) {
				int count = rs.getInt(1);
				if (count > 0) {
					result = true;
				} else {
					result = false;
				}
			} else {
				throw new DbException("Result Set is empty on compresionTestContainsConcreteDesingIdcompresionTestContainsConcreteDesingId");
			}
			return result;
		} catch (SQLException e) {
			throw new DbException("Error on compresionTestContainsConcreteDesingId"  + e.getMessage());
		}finally {
			DB.closeResultSet(rs);
			DB.closeStatement(st);
		}
	}
}
